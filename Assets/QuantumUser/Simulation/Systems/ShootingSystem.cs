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
            public Transform3D* Transform;
            public Character* Character;
            public PlayerLink* Link;          // so we only run for real players
       }

        public override void Update(Frame f, ref Filter filter)
        {
            if (filter.Character->IsDead) return;

            // Only for “real” local players (optional—remove if you want all players shooting automatically)
            if (filter.Link->Player == PlayerRef.None) return;

            ref var ply = ref *filter.Character;

            // 1) Decrement cooldown if needed
            if (ply.FireCooldown > 0)
            {
                ply.FireCooldown--;
                return;
            }

            // 2) Cooldown expired ? fire!
            // 2.a Compute spawn point & forward dir
            FPVector3 forward = filter.Transform->Forward;
            FPVector3 spawnPos = filter.Transform->Position + forward * filter.Character->MuzzleOffset;

            // 2.b Instantiate projectile (must match your ProjectilePrefab’s Archetype)
            var proj = f.Create(filter.Character->ProjectilePrefab);
            var projComp = f.Unsafe.GetPointer<Projectile>(proj);
            projComp->Owner = filter.Entity;

            // 2.c Initialize its Transform
            var projTransform = f.Unsafe.GetPointer<Transform3D>(proj);
            projTransform->Position = spawnPos;
            projTransform->Rotation = filter.Transform->Rotation;

            // 2.d Initialize its PhysicsBody3D velocity
            var body = f.Unsafe.GetPointer<PhysicsBody3D>(proj);
            body->Velocity = forward * projComp->Velocity;

            // 3) Reset your player’s cooldown
            ply.FireCooldown = ply.FireCdTicks;
        }
    }
}
