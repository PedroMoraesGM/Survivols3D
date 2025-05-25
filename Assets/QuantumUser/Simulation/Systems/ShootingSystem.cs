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
            if (!filter.OwnerData->OwnerEntity.IsValid) return;

            if (f.Get<HealthComponent>(filter.OwnerData->OwnerEntity).IsDead) return;

            if (f.Get<PlayerLink>(filter.OwnerData->OwnerEntity).Player == PlayerRef.None) return;

            ref var ply = ref *filter.WeaponComponent;

            // Decrement cooldown if needed
            if (ply.FireCdTicks > 0)
            {
                ply.FireCdTicks--;
                return;
            }

            // Cooldown expired ? fire!
            // Compute spawn point & forward dir
            var ownerTrasnform = f.Get<Transform3D>(filter.OwnerData->OwnerEntity);

            FPVector3 forward = ownerTrasnform.Forward;
            FPVector3 spawnPos = ownerTrasnform.Position + forward * filter.WeaponComponent->MuzzleOffset.X + ownerTrasnform.Up * filter.WeaponComponent->MuzzleOffset.Y;

            // Instantiate projectile (must match your ProjectilePrefab�s Archetype)
            var proj = f.Create(filter.WeaponComponent->ProjectilePrefab);
            if (!f.Unsafe.TryGetPointer<OwnerData>(proj, out var projComp))
            {
                f.Set(proj, new OwnerData { });
                projComp = f.Unsafe.GetPointer<OwnerData>(proj);
            }
            projComp->OwnerEntity = filter.OwnerData->OwnerEntity;

            // Initialize its Transform
            var character = f.Unsafe.GetPointer<Character>(filter.OwnerData->OwnerEntity);
            var projTransform = f.Unsafe.GetPointer<Transform3D>(proj);
            projTransform->Position = spawnPos;
            projTransform->Rotation = FPQuaternion.Euler(character->VerticalLookPitch, ownerTrasnform.Rotation.AsEuler.Y, ownerTrasnform.Rotation.AsEuler.Z);

            // Reset players cooldown
            ply.FireCdTicks = ply.FireCooldown;
        }
    }
}
