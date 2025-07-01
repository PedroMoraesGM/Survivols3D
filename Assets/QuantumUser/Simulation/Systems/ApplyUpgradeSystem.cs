using System;
using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using Quantum.Collections;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class ApplyUpgradeSystem : SystemMainThreadFilter<ApplyUpgradeSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public PlayerUpgradeComponent* Upgrades;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            Input input = default;
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* link))
            {
                input = *f.GetPlayerInput(link->Player);
            }

            ref var pu = ref *filter.Upgrades;
            if (!pu.WaitingForChoice)
                return;

            // Resolve the QTN lists
            var pending = f.ResolveList(pu.PendingChoices);
            QEnumDictionary<UpgradeId, AcquiredUpgradeInfo> taken = f.ResolveDictionary(pu.AcquiredUpgrades);

            // Player picks one
            if (pu.ChosenUpgradeId == UpgradeId.None)
            {
                int choosenIndex = -1;

                if (input.ChoiceFirst)
                    choosenIndex = 0;
                else if (input.ChoiceSecond)
                    choosenIndex = 1;
                else if (input.ChoiceThird)
                    choosenIndex = 2;

                if (choosenIndex != -1)
                {
                    pu.ChosenUpgradeId = pending[choosenIndex];
                    f.Signals.OnHasChoosenUpgrades(filter.Entity, pu.ChosenUpgradeId);
                    f.Events.OnHasChoosenUpgrades(filter.Entity, pu.ChosenUpgradeId);
                }
                return;
            }

            // Find the index of the chosen upgrade in pending
            int chosenIndex = -1;
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i] == pu.ChosenUpgradeId)
                {
                    chosenIndex = i;
                    break;
                }
            }

            // If not found, bail
            if (chosenIndex < 0)
                return;

            // Get the UpgradeEntry
            var data = f.GetSingleton<UpgradeDataComponent>();
            var entries = f.ResolveDictionary(data.EntriesPerClass)[f.Get<PlayerLink>(filter.Entity).Class].Entries;
            var all = f.ResolveList(entries);
            UpgradeEntry chosenEntry = default;

            foreach (var item in all)
            {
                if (item.Id == pending[chosenIndex])
                {
                    chosenEntry = item;
                    break;
                }
            }

            // Apply the upgrade (instantiate prefab, modify stats, etc.)
            ApplyUpgradeToPlayer(f, filter.Entity, chosenEntry, taken);

            // Clear pending and reset choice state
            pending.Clear();
            pu.ChosenUpgradeId = UpgradeId.None;
            pu.WaitingForChoice = false;
            
            pending.Clear();
            pu.ChosenUpgradeId = UpgradeId.None;
            pu.WaitingForChoice = false;
            pu.PendingLevelUpsChoices--;

            if (pu.PendingLevelUpsChoices > 0)
            {
                f.Signals.OnRequestUpgradeChoices(filter.Entity);
            }
        }

        void ApplyUpgradeToPlayer(Frame f, EntityRef player, UpgradeEntry entry, QEnumDictionary<UpgradeId, AcquiredUpgradeInfo> taken)
        {
            bool isRepeated = taken.ContainsKey(entry.Id);

            if (!isRepeated)
            {
                var newUpgrade = f.Create(entry.Prefab); // prefab here is a weapon that the player will be shooting 
                f.Set(newUpgrade, new OwnerData() { OwnerEntity = player });

                // Record it so it 
                taken.Add(entry.Id, new AcquiredUpgradeInfo() { UpgradeEntity = newUpgrade, CountIndex = 1, TotalCount = 1 });
            }
            else
            {
                // First increase the count of the upgrade
                AcquiredUpgradeInfo acquired = taken[entry.Id];                

                // Then apply the upgrade effect based on count
                QList<WeaponUpgradeEffects> effectsPerUpgrade = f.ResolveList(entry.EffectsPerExtraUpgrade);
                int i = acquired.CountIndex - 1; // count starts at 1, so we subtract to access effect from start

                QList<WeaponUpgradeEffect> effects = f.ResolveList(effectsPerUpgrade[i].Effects);

                foreach (var effect in effects)
                {
                    UnityEngine.Debug.Log($"Applying effect {effect.Type} with value {effect.Value} to upgrade {acquired.UpgradeEntity}");
                    switch (effect.Type)
                    {
                        case WeaponUpgradeType.DamageBonus:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out DamageComponent* damageComponent))
                                damageComponent->BaseDamage += effect.Value;
                            break;
                        case WeaponUpgradeType.AddDamageMultiplier:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out DamageComponent* damageComponent1))
                                damageComponent1->DamageMultiplier += effect.Value;
                            break;
                        case WeaponUpgradeType.FireRateBonus:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out ShootingWeaponComponent* shootingWeapon1))
                                shootingWeapon1->FireCooldown -= effect.Value.AsInt;
                            break;
                        case WeaponUpgradeType.FireRateMultiplier:
                            // This is a multiplier, so we multiply the cooldown by the value
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out ShootingWeaponComponent* shootingWeapon))
                                shootingWeapon->FireCooldown = (int)(shootingWeapon->FireCooldown * effect.Value);
                            break;
                        case WeaponUpgradeType.BurstCountBonus:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out ShootingWeaponComponent* shootingWeapon2))
                                shootingWeapon2->BurstCount += effect.Value.AsInt;
                            break;
                        case WeaponUpgradeType.BurstShotDelayBonus:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out ShootingWeaponComponent* shootingWeapon3))
                                shootingWeapon3->BurstShotDelay += effect.Value.AsInt;
                            break;
                        case WeaponUpgradeType.AddBounce:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out ShootingWeaponComponent* shootingWeapon4))
                                shootingWeapon4->CanBounce = true;
                            break;
                        case WeaponUpgradeType.EnableHoming:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out ShootingWeaponComponent* shootingWeapon5))
                                shootingWeapon5->CanHome = true;
                            break;
                        case WeaponUpgradeType.AddAreaRangeMultiplier:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out ShootingWeaponComponent* shootingWeapon6))
                                shootingWeapon6->AddAreaRangeMultiplier += effect.Value;
                            break;
                        case WeaponUpgradeType.AddHitsToDestroy:
                            if (f.Unsafe.TryGetPointer(acquired.UpgradeEntity, out ShootingWeaponComponent* shootingWeapon7))
                                shootingWeapon7->AddHitsToDestroy += effect.Value.AsInt;
                            break;
                    }
                }

                // Increase the count index and total count of the upgrade
                acquired.CountIndex++;
                acquired.TotalCount++;

                if (acquired.CountIndex < 1 || acquired.CountIndex > effectsPerUpgrade.Count) // If the index is out of bounds, we assume it finishes the effects
                    acquired.CountIndex = 1; // Reset count to 1                

                taken[entry.Id] = acquired;// update the count in the dictionary        
            }       
        }
    }
}
