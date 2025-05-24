using UnityEngine;
using UnityEngine.Scripting;
using Quantum;
using Photon.Deterministic;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class CollisionSystem : SystemSignalsOnly, ISignalOnCollisionEnter3D, ISignalOnTriggerEnter3D
    {
        public void OnCollisionEnter3D(Frame f, CollisionInfo3D info)
        {
             if(!f.IsVerified) return;

            OnPlayerTouchesXp(f, info);            
        }

        public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
        {
            OnProjectileHittingEnemy(f, info);
        }

        private void OnPlayerTouchesXp(Frame f, CollisionInfo3D info)
        {
            if (f.Unsafe.TryGetPointer<XPPickup>(info.Entity, out XPPickup* xpPickup))
            {
                if (f.Unsafe.TryGetPointer<Character>(info.Other, out Character* character))
                {
                    Debug.Log("[CollisionSystem] xp hitted player!");

                    f.Signals.OnXpAdquired(info.Other, xpPickup->Value);
                    f.Events.OnXpAdquired(info.Other, xpPickup->Value);

                    // Destroy xp after being adquired
                    f.Destroy(info.Entity); 

                }
            }
        }

        private void OnProjectileHittingEnemy(Frame f, TriggerInfo3D info)
        {
            if (f.Unsafe.TryGetPointer<Projectile>(info.Entity, out Projectile* projectile))
            {
                if (f.Unsafe.TryGetPointer<EnemyAI>(info.Other, out EnemyAI* enemy))
                {
                    Debug.Log("[CollisionSystem] projectile hitted enemy!");

                    if (!f.Unsafe.TryGetPointer<OwnerData>(info.Entity, out var owner))
                        return;

                    f.Signals.OnEnemyHit(info.Other, owner->OwnerEntity, projectile->Damage);
                    f.Events.OnEnemyHit(info.Other, owner->OwnerEntity, projectile->Damage);
                }

                projectile->HitsToDestroy--;

                if (projectile->HitsToDestroy <= 0)
                    f.Destroy(info.Entity);
            }
        }
    }
}
