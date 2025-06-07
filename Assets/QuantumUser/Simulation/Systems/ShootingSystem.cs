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
            public ShootingWeaponComponent* WeaponComponent;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            // If there's no owner or the owner is dead, bail out
            if (!filter.OwnerData->OwnerEntity.IsValid) return;
            if (f.Get<HealthComponent>(filter.OwnerData->OwnerEntity).IsDead) return;
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
                FireSingleProjectile(f, filter.Entity, filter.OwnerData->OwnerEntity, weapon);

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
            //  3) No cooldown and not in a burst → start a new burst
            // ───────────────────────────────────────────────
            weapon->BurstShotsRemaining = weapon->BurstCount;
            // Fire the very first shot _immediately_, so zero delay
            weapon->BurstDelayTicks = 0;
            // Then the next loop iteration will “catch” the bursting code above, fire the first shot,
            // decrement BurstShotsRemaining, etc.
        }

        // ────────────────────────────────────────────────────────────────────
        // Fires exactly one projectile, with a bit of random angular dispersion.
        // ────────────────────────────────────────────────────────────────────
        private void FireSingleProjectile(
          Frame f,
          EntityRef weaponEntity,
          EntityRef ownerEntity,
          ShootingWeaponComponent* weapon)
        {
            // 1) Figure out spawn position & “forward” direction from the owner’s Transform3D
            var ownerTf = f.Get<Transform3D>(ownerEntity);
            var forward = ownerTf.Forward;
            var up = ownerTf.Up;

            // 2) Apply a small random cone/spread, using BurstDispersion:
            //    We'll generate two random FP angles (pitch & yaw) ∈ [-Dispersion, +Dispersion].
            //    Then rotate the forward vector by those small angles.
            //
            //    RngSession.NextNormalized() yields an FP in [0,1]. We center it around 0:
            //      x ∈ [-1, +1] → x * BurstDispersion = random angle in [-Dispersion, +Dispersion].
            FP randPitch = (f.Global->RngSession.Next() * FP._1 * 2 - FP._1) * weapon->BurstDispersion;
            FP randYaw = (f.Global->RngSession.Next() * FP._1 * 2 - FP._1) * weapon->BurstDispersion;

            // Build a quaternion from those small Euler angles (pitch, yaw, 0)
            var spreadRot = FPQuaternion.Euler(randPitch, randYaw, FP._0);
            var shotDir = spreadRot * forward; // rotated forward vector

            // 3) Compute final spawn position:
            //    MuzzleOffset is in local “forward/up” space, so we do:
            var spawnPos = ownerTf.Position
                         + forward * weapon->MuzzleOffset.X
                         + up * weapon->MuzzleOffset.Y;

            // 4) Create the projectile entity and set its OwnerData
            var proj = f.Create(weapon->ProjectilePrefab);
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

            // We combine:
            //   a) our randomized “spread” yaw/pitch about the forward axis, and
            //   b) the player’s actual vertical look pitch.
            //
            // First, get the owner’s yaw & roll from ownerTf.Rotation. As Euler angles:
            var ownerEulerY = ownerTf.Rotation.AsEuler.Y; // owner’s yaw
            var ownerEulerZ = ownerTf.Rotation.AsEuler.Z; // owner’s roll (if any)

            // Then build a full rotation quaternion:
            //   1) start with vertical pitch (character look up/down),
            //   2) then apply owner yaw/roll,
            //   3) then apply our small “spread” pitch/yaw.
            //
            // In practice, we can multiply quaternions in Euler order:
            var rotYawRoll = FPQuaternion.Euler(verticalPitch, ownerEulerY, ownerEulerZ);
            var finalRot = spreadRot * rotYawRoll;
            Debug.Log("spreadRot:" + spreadRot);

            var projTf = f.Unsafe.GetPointer<Transform3D>(proj);
            projTf->Position = spawnPos;
            projTf->Rotation = finalRot;

            // 6) (Optionally) initialize any projectile-specific components here…
            //    e.g. damage, homing logic, etc.
        }
    }
}
