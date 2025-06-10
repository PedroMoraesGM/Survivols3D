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
            var taken = f.ResolveList(pu.AcquiredUpgrades);

            // Player picks one
            if (pu.ChosenUpgradeId == -1)
            {
                int choosenIndex = -1;

                if (input.ChoiceFirst)
                    choosenIndex = 0;
                else if(input.ChoiceSecond)
                    choosenIndex = 1;
                else if(input.ChoiceThird)
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
            var all = f.ResolveList(data.Entries);
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
            ApplyUpgradeToPlayer(f, filter.Entity, chosenEntry);

            // Record it so it won�t be offered again
            taken.Add(pending[chosenIndex]);

            // Clear pending and reset choice state
            pending.Clear();
            pu.ChosenUpgradeId = -1;
            pu.WaitingForChoice = false;
        }

        void ApplyUpgradeToPlayer(Frame f, EntityRef player, UpgradeEntry entry)
        {
            var newUpgrade = f.Create(entry.Prefab);
            f.Set(newUpgrade, new OwnerData() { OwnerEntity = player });            
        }
    }
}
