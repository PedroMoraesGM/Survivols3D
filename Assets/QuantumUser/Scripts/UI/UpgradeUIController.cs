using DG.Tweening;
using Quantum;
using System;
using UnityEngine;
using Quantum.Collections;

public class UpgradeUIController : MonoBehaviour
{
    [SerializeField] private CanvasGroup rewardsFrame;
    [SerializeField] private RectTransform rewardsContent;
    [SerializeField] private UpgradeCardItem rewardCardPrefab;
    [SerializeField] private UpgradeCatalog catalog;   // assign your ScriptableObject here

    private void Awake()
    {
        QuantumEvent.Subscribe(this, (EventOnChooseUpgrades e) => OnRewardsDisplay(e));
        QuantumEvent.Subscribe(this, (EventOnHasChoosenUpgrades e) => OnHasChoosenUpgrades(e));
    }

    private void OnHasChoosenUpgrades(EventOnHasChoosenUpgrades e)
    {
        Debug.Log("Has choosen upgrades!");
        rewardsFrame.DOFade(0, 0.35f);

    }

    private void OnRewardsDisplay(EventOnChooseUpgrades e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        Debug.Log("On upgrades display!");

        // Show the panel
        rewardsFrame.gameObject.SetActive(true);
        rewardsFrame.alpha = 0;
        rewardsFrame.DOFade(1, 0.35f);

        // Get the upgrade component
        if (!f.TryGet(e.Target, out PlayerUpgradeComponent playerUpgrade)) return;

        // Resolve the pending choices list
        var pending = f.ResolveList(playerUpgrade.PendingChoices);

        // Clear out any old cards
        foreach (Transform child in rewardsContent)
        {
            Destroy(child.gameObject);
        }

        // For each pending upgrade, spawn a card
        for (int i = 0; i < pending.Count; i++)
        {
            var entryForThisCard = pending[i];
            Debug.Log("Pending chioce ID:" + pending[i]);   
            var metaForThisCard = catalog.Get(entryForThisCard);

            var card = Instantiate(rewardCardPrefab, rewardsContent);

            card.Setup(entryForThisCard,
                       metaForThisCard,
                       // here `id` is fixed per iteration
                       id => OnCardSelected(), i+1);

            // Animate in…
            card.transform.localScale = Vector3.zero;
            card.transform
                .DOScale(1, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.1f * i);
        }
    }

    private void OnCardSelected()
    {
        // hide UI
        rewardsFrame.DOFade(0, 0.25f).OnComplete(() =>
        {
            rewardsFrame.gameObject.SetActive(false);
        });
    }
}
