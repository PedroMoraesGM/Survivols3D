using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class ShootingSystem : SystemMainThreadFilter<ShootingSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            //public Transform3D* Transform;
            public OwnerData* OwnerData;
            public ShootingWeaponComponent* WeaponComponent;
            //public PlayerLink* Link;          // so we only run for real players
       }

        public override void Update(Frame f, ref Filter filter)
        {
            if (!filter.OwnerData->OwnerEntity.IsValid) return;

            if (f.Get<Character>(filter.OwnerData->OwnerEntity).IsDead) return;

            if (f.Get<PlayerLink>(filter.OwnerData->OwnerEntity).Player == PlayerRef.None) return;

            ref var ply = ref *filter.WeaponComponent;

            // 1) Decrement cooldown if needed
            if (ply.FireCooldown > 0)
            {
                ply.FireCooldown--;
                return;
            }

            // 2) Cooldown expired ? fire!
            // 2.a Compute spawn point & forward dir
            var ownerTrasnform = f.Get<Transform3D>(filter.OwnerData->OwnerEntity);

            FPVector3 forward = ownerTrasnform.Forward;
            FPVector3 spawnPos = ownerTrasnform.Position + forward * filter.WeaponComponent->MuzzleOffset;

            // 2.b Instantiate projectile (must match your ProjectilePrefab�s Archetype)
            var proj = f.Create(filter.WeaponComponent->ProjectilePrefab);
            var projComp = f.Unsafe.GetPointer<Projectile>(proj);
            projComp->Owner = filter.Entity;

            // 2.c Initialize its Transform
            var projTransform = f.Unsafe.GetPointer<Transform3D>(proj);
            projTransform->Position = spawnPos;
            projTransform->Rotation = ownerTrasnform.Rotation;

            //// 2.d Initialize its PhysicsBody3D velocity
            //var body = f.Unsafe.GetPointer<PhysicsBody3D>(proj);
            //body->Velocity = forward * projComp->Velocity;

            // 3) Reset your player�s cooldown
            ply.FireCooldown = ply.FireCdTicks;
        }
    }
}
