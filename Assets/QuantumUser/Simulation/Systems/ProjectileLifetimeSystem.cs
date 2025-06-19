using Quantum;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public unsafe class ProjectileLifetimeSystem : SystemMainThreadFilter<ProjectileLifetimeSystem.Filter>
{
    public struct Filter
    {
        public EntityRef Entity;
        public Transform3D* Transform;
        public Projectile* Projectile;
        public MoveComponent* MoveComponent;
    }

    public override void Update(Frame f, ref Filter filter)
    {
        ref var p = ref *filter.Projectile;
        p.Elapsed += f.DeltaTime;
        if (p.Elapsed >= p.TimeToLive)
        {
            f.Destroy(filter.Entity);
            return;
        }

        if(filter.Projectile->CanMove)
            filter.Transform->Position += filter.Transform->Forward * filter.MoveComponent->BaseSpeed * filter.MoveComponent->SpeedMultiplier;
    }
}
