using Quantum;
using UnityEngine.Scripting;

[Preserve]
public unsafe class StatusEffectSystem : SystemMainThreadFilter<StatusEffectSystem.Filter>
{
    public struct Filter
    {
        public EntityRef Entity;
        public StatusEffectComponent* Status;
    }

    public override void Update(Frame f, ref Filter filter)
    {
        var status = filter.Status;

        if (status->SlowTimer > 0)
        {
            status->SlowTimer -= f.DeltaTime;
            if (status->SlowTimer <= 0)
            {
                // Reset slow when timer expires
                status->SlowMultiplier = 1;
                status->SlowTimer = 0;
            }
        }
    }
}
