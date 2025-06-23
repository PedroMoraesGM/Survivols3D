using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class ShootingSystem : SystemMainThreadFilter<ShootingSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public OwnerData* OwnerData;
            public DamageComponent* DamageComponent;
            public ShootingWeaponComponent* WeaponComponent;
        }
        public override void Update(Frame f, ref Filter filter)
        {
            // If there's no owner or the owner is dead, bail out
            if (!filter.OwnerData->OwnerEntity.IsValid) return;
            if (!f.TryGet(filter.OwnerData->OwnerEntity, out HealthComponent healthComponent) || healthComponent.IsDead) return;
            if (!filter.WeaponComponent->CanShoot) return;

            var weapon = filter.WeaponComponent;

            // ───────────────────────────────────────────────
            //  1) If we're in the middle of a burst, handle per-shot delays
            // ───────────────────────────────────────────────
            if (weapon->BurstShotsRemaining > 0)
            {
                // We still have shots to fire in this burst.
                if (weapon->BurstDelayTicks > 0)
                {
                    // Wait until the next shot delay has elapsed
                    weapon->BurstDelayTicks--;
                    return;
                }

                // Time to fire the next shot in the burst
                FireAllProjectiles(f, filter.Entity, filter.OwnerData->OwnerEntity, weapon, filter.DamageComponent);

                // Decrement shots remaining
                weapon->BurstShotsRemaining--;

                if (weapon->BurstShotsRemaining > 0)
                {
                    // More shots to fire: reset the inter-shot delay
                    weapon->BurstDelayTicks = weapon->BurstShotDelay;
                }
                else
                {
                    // Burst is done. Start the cooldown until the next burst can begin.
                    weapon->FireCdTicks = weapon->FireCooldown;
                }

                return;
            }

            // ───────────────────────────────────────────────
            //  2) If not bursting, count down the “between-burst” cooldown
            // ───────────────────────────────────────────────
            if (weapon->FireCdTicks > 0)
            {
                weapon->FireCdTicks--;
                return;
            }

            // ───────────────────────────────────────────────
            //  3) No cooldown and not in a burst > start a new burst
            // ───────────────────────────────────────────────
            weapon->BurstShotsRemaining = weapon->BurstCount;
            // Fire the very first shot _immediately_, so zero delay
            weapon->BurstDelayTicks = 0;
            // Then the next loop iteration will “catch” the bursting code above, fire the first shot,
            // decrement BurstShotsRemaining, etc.
        }

        // ────────────────────────────────────────────────────────────────────
        // Fires all projectile prefabs in the list, with a bit of random angular dispersion.
        // ────────────────────────────────────────────────────────────────────
        private void FireAllProjectiles(
          Frame f,
          EntityRef weaponEntity,
          EntityRef ownerEntity,
          ShootingWeaponComponent* weapon,
          DamageComponent* damage)
        {
            // 1) Figure out spawn position & “forward” direction from the owner’s Transform3D
            var ownerTf = f.Get<Transform3D>(ownerEntity);
            var forward = ownerTf.Forward;
            var up = ownerTf.Up;

            // 2) Apply a small random cone/spread, using BurstDispersion:
            FP randPitch = (f.Global->RngSession.Next() * FP._1 * 2 - FP._1) * weapon->BurstDispersion;
            FP randYaw = (f.Global->RngSession.Next() * FP._1 * 2 - FP._1) * weapon->BurstDispersion;
            var spreadRot = FPQuaternion.Euler(randPitch, randYaw, FP._0);
            var shotDir = spreadRot * forward;

            // 3) Compute final spawn position:
            var spawnPos = ownerTf.Position
                         + forward * weapon->MuzzleOffset.X
                         + up * weapon->MuzzleOffset.Y;

            // 4) Fire all projectile prefabs in the list
            if (!f.TryResolveList(weapon->ProjectilePrefabs, out var projectilePrefabs))
                return;

            for (int i = 0; i < projectilePrefabs.Count; i++)
            {
                var prefab = projectilePrefabs[i];
                var proj = f.Create(prefab);
                if (!f.Unsafe.TryGetPointer<OwnerData>(proj, out var projOwner))
                {
                    f.Set(proj, new OwnerData { });
                    projOwner = f.Unsafe.GetPointer<OwnerData>(proj);
                }
                projOwner->OwnerEntity = ownerEntity;

                // 5) Initialize projectile’s Transform3D using the “shotDir” and spawnPos
                FP verticalPitch = FP._0;
                if (f.Unsafe.TryGetPointer<Character>(ownerEntity, out var character))
                {
                    verticalPitch = character->VerticalLookPitch;
                }
                var ownerEulerY = ownerTf.Rotation.AsEuler.Y;
                var ownerEulerZ = ownerTf.Rotation.AsEuler.Z;
                var rotYawRoll = FPQuaternion.Euler(verticalPitch, ownerEulerY, ownerEulerZ);
                var finalRot = spreadRot * rotYawRoll;

                var projTf = f.Unsafe.GetPointer<Transform3D>(proj);
                projTf->Position = spawnPos;
                projTf->Rotation = finalRot;

                // 6) Initialize any projectile-specific components here…
                if (f.Unsafe.TryGetPointer<DamageComponent>(proj, out var damageComp))
                {
                    damageComp->BaseDamage += damage->BaseDamage;
                    damageComp->DamageMultiplier *= damage->DamageMultiplier;
                }

                if (f.Unsafe.TryGetPointer<Projectile>(proj, out var projectileComp))
                    projectileComp->HitsToDestroy += weapon->AddHitsToDestroy;

                if (f.Unsafe.TryGetPointer<HomingProjectileComponent>(proj, out var homingComp))
                {
                    f.Unsafe.TryGetPointer<Projectile>(proj, out var projectileComp1);
                    projectileComp1->CanMove = !weapon->CanHome;
                    homingComp->CanMove = weapon->CanHome;
                    homingComp->CanRepeatTarget = !weapon->CanBounce;
                }

                if(f.Unsafe.TryGetPointer<PhysicsCollider3D>(proj, out PhysicsCollider3D* collider))
                    collider->Shape.Capsule.Radius = collider->Shape.Capsule.Radius * (FP._1 + weapon->AddAreaRangeMultiplier);
            }
        }
    }
}
