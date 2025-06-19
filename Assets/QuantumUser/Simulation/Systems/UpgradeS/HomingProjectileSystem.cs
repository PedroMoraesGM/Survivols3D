using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using Quantum.Profiling;
using UnityEngine;


namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class HomingProjectileSystem :
      SystemMainThreadFilter<HomingProjectileSystem.Filter>,
      ISignalOnCollisionEnter3D, ISignalOnCollision3D, ISignalOnTriggerEnter3D, ISignalOnTrigger3D
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public MoveComponent* MoveComponent;
            public HomingProjectileComponent* Missile;
        }

        // 1) Movement & homing logic
        public override void Update(Frame f, ref Filter filter)
        {
            ref var m = ref *filter.Missile;
            ref var move = ref *filter.MoveComponent;
            var pos = filter.Transform->Position;

            // Acquire or validate target...
            bool gotTarget = TryAcquireClosestTarget(f, filter, pos, out m.CurrentTarget);

            if (!m.CanMove) return;

            if (m.CurrentTarget.IsValid)
            {
                var targetPos = f.Unsafe.GetPointer<Transform3D>(m.CurrentTarget)->Position;
                var offset = targetPos - pos;
                var distance = offset.Magnitude;

                // Safe normalize:
                FPVector3 toTarget = (distance > FP.Epsilon)
                  ? offset / distance
                  : filter.Transform->Forward;

                // Blend forward & target
                var blended = filter.Transform->Forward * (FP._1 - m.HomingStrength)
                            + toTarget * m.HomingStrength;

                // Safe normalize blended
                FPVector3 dir = (blended.Magnitude > FP.Epsilon)
                  ? blended / blended.Magnitude
                  : filter.Transform->Forward;

                // Only look if we actually have distance
                if (distance > FP.Epsilon)
                    filter.Transform->LookAt(targetPos);

                // Move if within follow range
                if (m.MinFollowDistance <= FP._0 || distance <= m.MinFollowDistance)
                    filter.Transform->Position += dir * move.BaseSpeed * move.SpeedMultiplier;
            }
            else
            {
                // fallback move forward
                filter.Transform->Position += filter.Transform->Forward * move.BaseSpeed * move.SpeedMultiplier;
            }
        }

        // Helper to pick the nearest EnemyAI
        bool TryAcquireClosestTarget(Frame f, Filter filter, FPVector3 from, out EntityRef best)
        {
            best = EntityRef.None;
            FP bestDsqr = FP.MaxValue;

            if (filter.Missile->HomeToPlayers)
            {
                foreach (var block in f.Unsafe.GetComponentBlockIterator<Character>()) // target player
                {
                    var e = block.Entity;
                    if (f.Unsafe.GetPointer<HealthComponent>(e)->IsDead) continue; // skip if is dead
                    var p = f.Unsafe.GetPointer<Transform3D>(e)->Position;

                    FP dsq = (p - from).SqrMagnitude;
                    if (dsq < bestDsqr && (filter.Missile->PreviousTarget != e || filter.Missile->CanRepeatTarget))
                    {
                        bestDsqr = dsq;
                        best = e;
                    }
                }
            }
            else
            {
                foreach (var block in f.Unsafe.GetComponentBlockIterator<EnemyAI>()) // target enemy
                {
                    var e = block.Entity;
                    if (f.Unsafe.GetPointer<HealthComponent>(e)->IsDead) continue; // skip if is dead
                    var p = f.Unsafe.GetPointer<Transform3D>(e)->Position;
                    FP dsq = (p - from).SqrMagnitude;
                    if (dsq < bestDsqr && (filter.Missile->PreviousTarget != e || filter.Missile->CanRepeatTarget))
                    {
                        bestDsqr = dsq;
                        best = e;
                    }

                }
            }

            return best != EntityRef.None;
        }

        private void ProjetileCollisionEnter(Frame f, EntityRef infoEntity, EntityRef infoOther)
        {
            if (!f.IsVerified) return;

            // Only handle collisions where the *projectile* entity has a HomingProjectileComponent
            if (!f.Unsafe.TryGetPointer(infoEntity, out HomingProjectileComponent* bm))
                return;

            // And only if it hit an valid target
            bool isValidTarget = (!bm->HomeToPlayers && f.Unsafe.TryGetPointer<EnemyAI>(infoOther, out EnemyAI* enemy)) || bm->HomeToPlayers && f.Unsafe.TryGetPointer<Character>(infoOther, out Character* character);
            if (!isValidTarget)
                return;

            // Decrement bounces
            bm->RemainingBounces--;

            if (bm->RemainingBounces > 0 || bm->CanRepeatTarget)
            {
                // Reset so next tick it will retarget
                bm->PreviousTarget = bm->CurrentTarget;
                bm->CurrentTarget = EntityRef.None;
            }
            else
            {
                // No bounces left ? destroy
                f.Destroy(infoEntity);
            }
        }

        private void ProjectileContinuousTrigger(Frame f, EntityRef infoEntity, EntityRef infoOther)
        {
            if (!f.IsVerified) return;

            if (f.Unsafe.TryGetPointer<HomingProjectileComponent>(infoEntity, out HomingProjectileComponent* projectile))
            {
                bool isValidTarget = (!projectile->HomeToPlayers && f.Unsafe.TryGetPointer<EnemyAI>(infoOther, out EnemyAI* enemy)) || projectile->HomeToPlayers && f.Unsafe.TryGetPointer<Character>(infoOther, out Character* character);
                if (isValidTarget)
                {
                    if (projectile->CanDragTarget)
                    {
                        FPVector3 move = (f.Unsafe.GetPointer<Transform3D>(infoEntity)->Forward) + (f.Unsafe.GetPointer<Transform3D>(infoEntity)->Up * FP._0_04);
                        FPVector3 pos = f.Unsafe.GetPointer<Transform3D>(infoOther)->Position;
                        f.Unsafe.GetPointer<Transform3D>(infoOther)->Position = new FPVector3(pos.X + move.X, FPMath.Max(pos.Y + move.Y, -FP._6), pos.Z + move.Z);
                    }
                }
            }
        }

        public void OnCollisionEnter3D(Frame f, CollisionInfo3D info)
        {
            ProjetileCollisionEnter(f, info.Entity, info.Other);
        }

        public void OnCollision3D(Frame f, CollisionInfo3D info)
        {
            ProjectileContinuousTrigger(f, info.Entity, info.Other);
        }

        public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
        {
            ProjetileCollisionEnter(f, info.Entity, info.Other);
        }

        public void OnTrigger3D(Frame f, TriggerInfo3D info)
        {
            ProjectileContinuousTrigger(f, info.Entity, info.Other);
        }
    }
}
