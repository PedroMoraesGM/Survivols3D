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
        QuantumEvent.Subscribe(this, (EventOnPlayerDefeated e) => OnPlayerDefeated(e));

    }

    private void OnPlayerDefeated(EventOnPlayerDefeated e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        HideUI();
    }

    private void OnHasChoosenUpgrades(EventOnHasChoosenUpgrades e)
    {
        // Find the instantiated card whose ID matches the player's choice
        foreach (var card in rewardsContent.GetComponentsInChildren<UpgradeCardItem>(true))
        {
            if (card.UpgradeId == e.ChoosenId)
            {
                // Play its selection effect, then fade out the panel
                card.PlaySelectionEffect(() =>
                {
                    rewardsFrame
                      .DOFade(0, 0.35f)
                      .OnComplete(() => rewardsFrame.gameObject.SetActive(false));
                });
                return;
            }
        }

        // Fallback: if not found, just hide immediately
        rewardsFrame
          .DOFade(0, 0.25f)
          .OnComplete(() => rewardsFrame.gameObject.SetActive(false));
    }

    private void OnRewardsDisplay(EventOnChooseUpgrades e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;


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

            var metaForThisCard = catalog.Get(entryForThisCard);
            Debug.Log("On upgrades display! entry ID:" + entryForThisCard);

            var card = Instantiate(rewardCardPrefab, rewardsContent);
            if (metaForThisCard != null)
            {
                Debug.Log("Meta is valid");
                card.Setup(entryForThisCard,
                       metaForThisCard,
                       // here `id` is fixed per iteration
                       id => OnCardSelected(), i + 1);
            }

            // Animate in�
            card.transform.localScale = Vector3.zero;
            card.transform
                .DOScale(1, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.1f * i);
        }
    }    

    private void OnCardSelected()
    {
        HideUI();
    }
    private void HideUI()
    {
        rewardsFrame.DOFade(0, 0.25f).OnComplete(() =>
        {
            rewardsFrame.gameObject.SetActive(false);
        });
    }
}
