using Photon.Deterministic;
using Quantum;
using UnityEngine.Scripting;

[Preserve]
public unsafe class StatusEffectSystem : SystemMainThreadFilter<StatusEffectSystem.Filter>
{
    public struct Filter
    {
        public EntityRef Entity;
        public HealthComponent* Health;
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

        if (status->HealthRegenTimer > 0)
        {
            status->HealthRegenTimer -= f.DeltaTime;
            if (status->HealthRegenTimer <= 0)
            {
                // Reset health regen when timer expires
                status->HealthRegenAmount = 0;
                status->HealthRegenTimer = 0;
            }
            else
            {
                // Apply health regeneration effect
                RegenHealthEffect(f, filter.Entity, status->HealthRegenAmount * f.DeltaTime);
            }
        }
    }

    private void RegenHealthEffect(Frame f, EntityRef entity, FP regenAmount)
    {
        if (f.Unsafe.TryGetPointer(entity, out HealthComponent* health))
        {
            health->CurrentHealth += regenAmount;
            if (health->CurrentHealth > health->MaxHealth)
            {
                health->CurrentHealth = health->MaxHealth;
            }
        }
    }
}
