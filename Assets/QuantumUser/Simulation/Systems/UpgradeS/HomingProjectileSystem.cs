using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
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
            FPVector3 dir;

            // Acquire target if needed
            if (!m.HasTarget || m.CurrentTarget.IsValid)
            {
                m.HasTarget = TryAcquireClosestTarget(f, filter, pos, out m.CurrentTarget);
            }

            // Compute steering
            if (m.HasTarget) //&& f.Unsafe.Exists(m.CurrentTarget))
            {
                var targetPos = f.Unsafe.GetPointer<Transform3D>(m.CurrentTarget)->Position;
                var toTarget = (targetPos - pos).Normalized;
                var distance = (targetPos - pos).Magnitude;
                dir = (filter.Transform->Forward * (FP._1 - m.HomingStrength)
                       + toTarget * m.HomingStrength).Normalized;
                
                filter.Transform->LookAt(targetPos);

                // Apply velocity
                if (filter.Missile->MinFollowDistance <= FP._0 || distance <= filter.Missile->MinFollowDistance)
                    filter.Transform->Position += dir * m.Speed;
            }
            else
            {
                // Lost or no target
                m.HasTarget = false;
                m.CurrentTarget = EntityRef.None;
                dir = filter.Transform->Forward;
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
            // Only handle collisions where the *projectile* entity has a BouncyMissileComponent
            if (!f.Unsafe.TryGetPointer(info.Entity, out HomingProjectileComponent* bm))
                return;

            // And only if it hit an EnemyAI
            if (!f.Unsafe.TryGetPointer(info.Other, out EnemyAI* enemy))
                return;

            // Decrement bounces
            bm->RemainingBounces--;

            if (bm->RemainingBounces > 0)
            {
                // Reset so next tick it will retarget
                bm->PreviousTarget = bm->CurrentTarget;
                bm->HasTarget = false;
                bm->CurrentTarget = EntityRef.None;
            }
            else
            {
                //// No bounces left ? destroy
                //f.Destroy(info.Entity);
            }
        }

        public void OnTrigger3D(Frame f, TriggerInfo3D info)
        {
            if (f.Unsafe.TryGetPointer<HomingProjectileComponent>(info.Entity, out HomingProjectileComponent* projectile))
            {
                if (f.Unsafe.TryGetPointer<EnemyAI>(info.Other, out EnemyAI* enemy))
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
