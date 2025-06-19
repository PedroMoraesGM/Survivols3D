using Photon.Deterministic;
using Quantum;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public unsafe class UpgradeSystem : SystemSignalsOnly, ISignalOnLevelUp
{
    void GenerateChoices(Frame f, EntityRef entity, ref PlayerUpgradeComponent* pu)
    {
        var data = f.GetSingleton<UpgradeDataComponent>();
        var all = f.ResolveList(data.Entries);
        var taken = f.ResolveDictionary(pu->AcquiredUpgrades);
        var pending = f.ResolveList(pu->PendingChoices);

        if (!f.TryGet(entity, out XPComponent xp)) return;

        // Build pool of eligible entries
        var pool = new List<UpgradeEntry>();
        foreach (var e in all)
        {
            if (e.MinLevel <= xp.Level && (!taken.ContainsKey(e.Id) || e.CanBeRepeated) )
                pool.Add(e);
        }

        // Pick without replacement
        pending.Clear();
        for (int k = 0; k < data.ChoicesPerLevel && pool.Count > 0; k++)
        {
            int total = pool.Sum(x => x.Weight);
            int pick = f.Global->RngSession.Next(0, total);
            int acc = 0, idx = 0;
            for (; idx < pool.Count; idx++)
            {
                acc += pool[idx].Weight;
                if (pick < acc) break;
            }

            pending.Add(pool[idx].Id);
            pool.RemoveAt(idx);
        }
    }

    public void OnLevelUp(Frame f, EntityRef target, int newLevel)
    {
        if (!f.Unsafe.TryGetPointer(target, out PlayerUpgradeComponent* playerUpgrades)) return;

        if (playerUpgrades->WaitingForChoice) return;

        // Generate choices
        GenerateChoices(f, target, ref playerUpgrades);

        playerUpgrades->WaitingForChoice = true;
        f.Events.OnChooseUpgrades(target);
    }
}
