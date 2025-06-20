using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class AreaWeaponSystem :
      SystemMainThreadFilter<AreaWeaponSystem.Filter>,
      ISignalOnTrigger3D
    {

        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public AreaWeaponComponent* Area;
            public OwnerData* Owner;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            var area = filter.Area;

            // Tick lifetime
            area->Elapsed += f.DeltaTime;
            if (area->Elapsed >= area->TimeToLive)
            {
                f.Destroy(filter.Entity);
                return;
            }

            // Tick cooldown
            area->DamageCdTicks += 1;

            // Enable damage tick when cooldown threshold is reached
            if (area->DamageCdTicks >= area->DamageCooldown)
            {
                area->IsDamageTick = true;
                area->DamageCdTicks = 0;
            }
            else
            {
                area->IsDamageTick = false;
            }
        }

        public void OnTrigger3D(Frame f, TriggerInfo3D info)
        {
            if (!f.Unsafe.TryGetPointer<AreaWeaponComponent>(info.Entity, out var areaWeapon))
                return;

            // Check for Slow area
            if(f.Unsafe.TryGetPointer<SlowAreaComponent>(info.Entity, out var slowArea))
            {
                if (f.Unsafe.TryGetPointer<StatusEffectComponent>(info.Other, out var status))
                {
                    if(status->SlowMultiplier > slowArea->SlowAmount) 
                        status->SlowMultiplier = slowArea->SlowAmount;
                    status->SlowTimer = slowArea->SlowDuration;
                }
            }

            //Check for Health Regen area
            if (f.Unsafe.TryGetPointer<HealthRegenAreaComponent>(info.Entity, out var healthRegenArea))
            {
                if (f.Unsafe.TryGetPointer<StatusEffectComponent>(info.Other, out var status))
                {
                    status->HealthRegenAmount = healthRegenArea->HealthRegenAmount;
                    status->HealthRegenTimer = healthRegenArea->HealthRegenDuration;
                }
            }

            // Only apply damage during valid damage tick
            if (!areaWeapon->IsDamageTick)
                return;

            if (!f.Unsafe.TryGetPointer<OwnerData>(info.Entity, out var owner)) 
                return;

            // Apply damage 
            if (f.Unsafe.TryGetPointer<DamageComponent>(info.Entity, out DamageComponent* damageComponent))
            {
                f.Signals.OnHit(info.Other, owner->OwnerEntity, damageComponent->BaseDamage * damageComponent->DamageMultiplier);
                f.Events.OnHit(info.Other, owner->OwnerEntity, damageComponent->BaseDamage * damageComponent->DamageMultiplier);
            }
        }
    }
}
