using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public unsafe class XPPickupSystem : SystemSignalsOnly, ISignalOnDefeated, ISignalOnXpAdquired
{
    public void OnXpAdquired(Frame f, EntityRef target, FP xpAmount)
    {
        // Grab the player’s XPComponent
        if (!f.Unsafe.TryGetPointer<XPComponent>(target, out XPComponent* xpComponent))
        {
            return;
        }

        // Add the gained XP
        xpComponent->TotalXP += xpAmount;
        xpComponent->CurrentXP += xpAmount;

        // Check for level?ups (possibly multiple)
        var nextLevelReq = XPForNextLevel(xpComponent->Level + 1);
        while (xpComponent->CurrentXP >= nextLevelReq)
        {
            // Deduct the XP and bump the level
            xpComponent->CurrentXP -= nextLevelReq;
            xpComponent->Level++;

            // Raise a signal so other systems know the player leveled up
            f.Signals.OnLevelUp(target, xpComponent->Level);
            f.Events.OnLevelUp(target, xpComponent->Level);

            Debug.Log("Character levelup! level:" + xpComponent->Level + " currentxp:" + xpComponent->CurrentXP);

            // Prepare for the next loop iteration
            nextLevelReq = XPForNextLevel(xpComponent->Level + 1);
        }
    }

    public static FP XPForNextLevel(int nextLevel)
    {
        if (nextLevel < 20)
        {
            return (nextLevel * 10) - 5;                                   
        }
        else if (nextLevel < 40)
        {
            return (nextLevel * 13) - 6 + (nextLevel == 20 ? 600 : 0);    
        }
        else
        {
            return (nextLevel * 16) - 8 + (nextLevel == 40 ? 2400 : 0);   
        }
    }

    public void OnDefeated(Frame f, EntityRef target, EntityRef dealer)
    {
        if (f.Unsafe.TryGetPointer<XPComponent>(dealer, out XPComponent* dealerXpComponent))
        {
            if (f.Unsafe.TryGetPointer<EnemyAI>(target, out EnemyAI* enemyAi))
            {
                if (enemyAi->XpDrop == 0) return;
                
                EntityRef xpEntity = f.Create(dealerXpComponent->XpPrefab);

                if (f.Unsafe.TryGetPointer<XPPickup>(xpEntity, out var xpPickup))
                {
                    // Assign target xp value to new created xp pickup
                    xpPickup->Value = enemyAi->XpDrop;
                }

                // Spawn at same position as the target;
                f.Unsafe.GetPointer<Transform3D>(xpEntity)->Position = f.Unsafe.GetPointer<Transform3D>(target)->Position;
            }
        }
    }
}
