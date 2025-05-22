using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class HomingProjectileSystem :
      SystemMainThreadFilter<HomingProjectileSystem.Filter>,
      ISignalOnCollisionEnter3D
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public PhysicsBody3D* Body;
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
                m.HasTarget = TryAcquireClosestEnemy(f, pos, out m.CurrentTarget);
            }

            // Compute steering
            if (m.HasTarget) //&& f.Unsafe.Exists(m.CurrentTarget))
            {
                Debug.Log("Missile has target");
                var targetPos = f.Unsafe.GetPointer<Transform3D>(m.CurrentTarget)->Position;
                var toTarget = (targetPos - pos).Normalized;
                dir = (filter.Transform->Forward * (FP._1 - m.HomingStrength)
                       + toTarget * m.HomingStrength).Normalized;
            }
            else
            {
                // Lost or no target
                m.HasTarget = false;
                m.CurrentTarget = EntityRef.None;
                dir = filter.Transform->Forward;
            }

            // Apply velocity
            filter.Body->Velocity = dir * m.Speed;
            filter.Body->ClearForce();
        }

        // Helper to pick the nearest EnemyAI
        bool TryAcquireClosestEnemy(Frame f, FPVector3 from, out EntityRef best)
        {
            best = EntityRef.None;
            FP bestDsqr = FP.MaxValue;

            foreach (var block in f.Unsafe.GetComponentBlockIterator<EnemyAI>())
            {
                var e = block.Entity;
                var p = f.Unsafe.GetPointer<Transform3D>(e)->Position;
                FP dsq = (p - from).SqrMagnitude;
                if (dsq < bestDsqr)
                {
                    bestDsqr = dsq;
                    best = e;
                }
                
            }
            return best != EntityRef.None;
        }

        // 2) Collision signal for bounces
        public void OnCollisionEnter3D(Frame f, CollisionInfo3D info)
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
                bm->HasTarget = false;
                bm->CurrentTarget = EntityRef.None;
            }
            else
            {
                //// No bounces left ? destroy
                //f.Destroy(info.Entity);
            }

            // f.Signals.OnEnemyHit(info.Other, /*owner*/..., /*damage*/...);
            // f.Events.OnEnemyHit(info.Other, /*owner*/..., /*damage*/...);
        }
    }
}
