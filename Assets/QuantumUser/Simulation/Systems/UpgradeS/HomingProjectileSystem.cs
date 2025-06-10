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
      ISignalOnTriggerEnter3D, ISignalOnTrigger3D
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public HomingProjectileComponent* Missile;
        }

        // 1) Movement & homing logic
        public override void Update(Frame f, ref Filter filter)
        {
            ref var m = ref *filter.Missile;
            var pos = filter.Transform->Position;

            // Acquire or validate target...
            bool gotTarget = TryAcquireClosestTarget(f, filter, pos, out m.CurrentTarget);

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
                    filter.Transform->Position += dir * m.Speed;
            }
            else
            {
                // fallback move forward
                filter.Transform->Position += filter.Transform->Forward * m.Speed;
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
                    if(f.Unsafe.GetPointer<HealthComponent>(e)->IsDead) continue; // skip if is dead
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

        // 2) Collision signal for bounces
        public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
        {
            //Debug.LogError("[Homing] trigger enter");

            if (!f.IsVerified) return;

            // Only handle collisions where the *projectile* entity has a HomingProjectileComponent
            if (!f.Unsafe.TryGetPointer(info.Entity, out HomingProjectileComponent* bm))
                return;

            //Debug.LogError("[Homing] hp valid");

            // And only if it hit an valid target
            bool isValidTarget = (!bm->HomeToPlayers && f.Unsafe.TryGetPointer<EnemyAI>(info.Other, out EnemyAI* enemy)) || bm->HomeToPlayers && f.Unsafe.TryGetPointer<Character>(info.Other, out Character* character);
            if (!isValidTarget)
                return;

            //Debug.LogError("[Homing] target valid");

            // Decrement bounces
            bm->RemainingBounces--;

            if (bm->RemainingBounces > 0 || bm->CanRepeatTarget)
            {
                // Reset so next tick it will retarget
                bm->PreviousTarget = bm->CurrentTarget;
                bm->CurrentTarget = EntityRef.None;

                //Debug.LogError("[Homing] reset target");
            }
            else
            {
                // No bounces left ? destroy
                f.Destroy(info.Entity);

                //Debug.LogError("[Homing] destroy entity");
            }
        }

        public void OnTrigger3D(Frame f, TriggerInfo3D info)
        {
            if (!f.IsVerified) return;

            if (f.Unsafe.TryGetPointer<HomingProjectileComponent>(info.Entity, out HomingProjectileComponent* projectile))
            {
                bool isValidTarget = (!projectile->HomeToPlayers && f.Unsafe.TryGetPointer<EnemyAI>(info.Other, out EnemyAI* enemy)) || projectile->HomeToPlayers && f.Unsafe.TryGetPointer<Character>(info.Other, out Character* character);
                if (isValidTarget)
                {
                    if (projectile->CanDragTarget)
                    {
                        FPVector3 move = (f.Unsafe.GetPointer<Transform3D>(info.Entity)->Forward) + (f.Unsafe.GetPointer<Transform3D>(info.Entity)->Up * FP._0_04);
                        FPVector3 pos = f.Unsafe.GetPointer<Transform3D>(info.Other)->Position;
                        f.Unsafe.GetPointer<Transform3D>(info.Other)->Position = new FPVector3(pos.X + move.X, FPMath.Max( pos.Y + move.Y, -FP._6), pos.Z + move.Z);
                    }
                }
            }
        }
    }
}
