using Photon.Deterministic;
using Quantum;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public unsafe class UpgradeSystem : SystemSignalsOnly, ISignalOnLevelUp, ISignalOnRequestUpgradeChoices
{
    Quantum.Collections.QList<UpgradeId> GenerateChoices(Frame f, EntityRef entity, ref PlayerUpgradeComponent* pu)
    {
        var data = f.GetSingleton<UpgradeDataComponent>();
        var entries = f.ResolveDictionary(data.EntriesPerClass)[f.Get<PlayerLink>(entity).Class].Entries;
        Quantum.Collections.QList<UpgradeEntry> all = f.ResolveList(entries);
        var taken = f.ResolveDictionary(pu->AcquiredUpgrades);
        Quantum.Collections.QList<UpgradeId> pending = f.ResolveList(pu->PendingChoices);

        if (!f.TryGet(entity, out XPComponent xp)) return pending;

        Debug.Log($"All upgrades for class: {all.Count}");
        // Debug.Log($"EntriesPerClass keys: {entries.}");
        Debug.Log($"Player class: {f.Get<PlayerLink>(entity).Class}");
        Debug.Log($"Taken upgrades: {taken.Count}");

        // Build pool of eligible entries
        var pool = new List<UpgradeEntry>();
        foreach (var e in all)
        {
            if (e.MinLevel <= xp.Level && (!taken.ContainsKey(e.Id) || e.CanBeRepeated))
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
        return pending;
    }

    public void OnLevelUp(Frame f, EntityRef target, int newLevel)
    {
        if (!f.Unsafe.TryGetPointer(target, out PlayerUpgradeComponent* playerUpgrades)) return;

        playerUpgrades->PendingLevelUpsChoices++;

        // Only generate choices if not already waiting for a choice
        if (!playerUpgrades->WaitingForChoice)
        {
            f.Signals.OnRequestUpgradeChoices(target);
        }
    }
    
    public void OnRequestUpgradeChoices(Frame f, EntityRef target)
    {
        if (!f.Unsafe.TryGetPointer(target, out PlayerUpgradeComponent* playerUpgrades)) return;

        var pending = GenerateChoices(f, target, ref playerUpgrades);
        playerUpgrades->WaitingForChoice = true;
        f.Events.OnChooseUpgrades(target, pending);
    }
}
