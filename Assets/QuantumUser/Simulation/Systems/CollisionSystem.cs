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

            OnPlayerTouchesXp(f, info.Entity, info.Other);
            OnProjectileHitting(f, info.Entity, info.Other);
        }

        public void OnTriggerEnter3D(Frame f, TriggerInfo3D info)
        {
            OnProjectileHitting(f, info.Entity, info.Other);
        }

        private void OnPlayerTouchesXp(Frame f, EntityRef infoEntity, EntityRef infoOther)
        {
            if (f.Unsafe.TryGetPointer<XPPickup>(infoEntity, out XPPickup* xpPickup))
            {
                if (f.Unsafe.TryGetPointer<Character>(infoOther, out Character* character))
                {
                    f.Signals.OnXpAdquired(infoOther, xpPickup->Value);
                    f.Events.OnXpAdquired(infoOther, xpPickup->Value);

                    // Destroy xp after being adquired
                    f.Destroy(infoEntity); 

                }
            }
        }

        private void OnProjectileHitting(Frame f, EntityRef infoEntity, EntityRef infoOther)
        {
            if (f.Unsafe.TryGetPointer<Projectile>(infoEntity, out Projectile* projectile))
            {
                if (f.Unsafe.TryGetPointer<HealthComponent>(infoOther, out HealthComponent* health))
                {
                    EntityRef dealerEntity = EntityRef.None;

                    if (f.Unsafe.TryGetPointer<OwnerData>(infoEntity, out var owner))
                        dealerEntity = owner->OwnerEntity;

                    if (f.Unsafe.TryGetPointer<DamageComponent>(infoEntity, out DamageComponent* damageComponent))
                    {
                        f.Signals.OnHit(infoOther, dealerEntity, damageComponent->BaseDamage * damageComponent->DamageMultiplier);
                        f.Events.OnHit(infoOther, dealerEntity, damageComponent->BaseDamage * damageComponent->DamageMultiplier);
                    }
                }

                projectile->HitsToDestroy--;

                if (projectile->HitsToDestroy <= 0)
                    f.Destroy(infoEntity);
            }
        }
    }
}
