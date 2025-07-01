using DG.Tweening;
using Quantum;
using System;
using UnityEngine;
using Quantum.Collections;
using UnityEngine.UI;
using System.Collections;

public class UpgradeUIController : MonoBehaviour
{
    [Header("Upgrade UI")]
    [SerializeField] private ActiveUpgradeItem upgradeSlotPrefab;
    [SerializeField] private RectTransform activeUpgradesContent;
    [Header("Upgrade Rewards UI")]
    [SerializeField] private CanvasGroup rewardsFrame;
    [SerializeField] private RectTransform rewardsContent;
    [SerializeField] private UpgradeCardItem rewardCardPrefab;
    [SerializeField] private UpgradeCatalog catalog;   // assign your ScriptableObject here

    private void Awake()
    {
        QuantumEvent.Subscribe(this, (EventOnChooseUpgrades e) => StartCoroutine(OnRewardsDisplay(e)));
        QuantumEvent.Subscribe(this, (EventOnHasChoosenUpgrades e) => OnHasChoosenUpgrades(e));
        QuantumEvent.Subscribe(this, (EventOnDefeated e) => OnPlayerDefeated(e));
    }

    private void OnPlayerDefeated(EventOnDefeated e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        HideChooseUpgradeUI();
    }

    void Update()
    {
        if(QuantumRunner.Default == null || QuantumRunner.Default.Game == null)
            return;

        // 1. Get the local player's upgrade component
        var game = QuantumRunner.Default.Game;
        var frame = game.Frames.Verified;

        if(frame == null) return;

        // Find the local player entity
        EntityRef playerEntity = EntityRef.None;
        var registryComp = frame.GetSingleton<PlayerRegistryComponent>();
        var players = frame.ResolveList(registryComp.ActivePlayers);

        foreach (var playerLink in players)
            {
                if (game.PlayerIsLocal(frame.Get<PlayerLink>(playerLink.Entity).Player))
                {
                    playerEntity = playerLink.Entity;
                    break;
                }
            }
        if (!playerEntity.IsValid) return;

        // Get the PlayerUpgradeComponent
        if (!frame.TryGet(playerEntity, out PlayerUpgradeComponent playerUpgrade)) return;

        // 2. Clear existing UI
        foreach (Transform child in activeUpgradesContent) // to fix: Instead of clearing all children, we should only clear those that are upgrade slots
        {
            if (child.TryGetComponent<ActiveUpgradeItem>(out _))
                Destroy(child.gameObject);
        }

        // 3. For each acquired upgrade, create a slot
        foreach (var kvp in frame.ResolveDictionary(playerUpgrade.AcquiredUpgrades))
        {
            UpgradeId upgradeId = kvp.Key;
            var upgradeInfo = kvp.Value;

            // Get upgrade color from catalog
            var meta = catalog.Get(upgradeId);
            Color upgradeColor = meta != null ? meta.IconColor : Color.white;
            Sprite upgradeIcon = meta != null ? meta.Icon : null;

            // Instantiate UI slot
            var slot = Instantiate(upgradeSlotPrefab, activeUpgradesContent);
            slot.InitItem(upgradeIcon, upgradeColor, upgradeInfo.TotalCount);

            // Try to get the weapon entity for this upgrade
            var weaponEntity = upgradeInfo.UpgradeEntity;
            if (frame.TryGet(weaponEntity, out ShootingWeaponComponent weapon))
            {
                // Show cooldown as fill amount
                float fill = 1f;
                if (weapon.FireCooldown > 0)
                    fill = 1f - (weapon.FireCdTicks / (float)weapon.FireCooldown);
                slot.SetFill(Mathf.Clamp01(fill));
            }
            else
            {
                slot.SetFill(1f);
            }
        }
    }

    private void OnHasChoosenUpgrades(EventOnHasChoosenUpgrades e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

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

    private IEnumerator OnRewardsDisplay(EventOnChooseUpgrades e)
    {
        if(rewardsFrame.gameObject.activeSelf)
            yield return new WaitForSeconds(1); // Wait a bit if already showing to avoid flicker

        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) yield break;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) yield break;

        // Show the panel
        rewardsFrame.gameObject.SetActive(true);
        rewardsFrame.alpha = 0;
        rewardsFrame.DOFade(1, 0.35f);

        // Get the upgrade component
        if (!f.TryGet(e.Target, out PlayerUpgradeComponent playerUpgrade)) yield break;

        // Resolve the pending choices list
        var pending = f.ResolveList(e.PendingChoices);


        // Clear out any old cards
        foreach (Transform child in rewardsContent)
        {
            Destroy(child.gameObject);
        }

        // For each pending upgrade, spawn a card
        for (int i = 0; i < pending.Count; i++)
        {
            var entryForThisCard = pending[i];
            var acquiredUpgrades = f.ResolveDictionary(playerUpgrade.AcquiredUpgrades);
            AcquiredUpgradeInfo upgradeInfo = acquiredUpgrades.ContainsKey(entryForThisCard) ? acquiredUpgrades[entryForThisCard] :
                                        new AcquiredUpgradeInfo() { CountIndex = 0, TotalCount = 0 };
            var metaForThisCard = catalog.Get(entryForThisCard);

            var card = Instantiate(rewardCardPrefab, rewardsContent);
            if (metaForThisCard != null)
            {                
                card.Setup(entryForThisCard,
                       metaForThisCard,
                       upgradeInfo,
                       id => OnCardSelected(), i + 1);
            }

            // Animate in�
            card.transform.localScale = Vector3.zero;
            card.transform.DOKill();
            card.transform
                .DOScale(1, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.1f * i);
        }
    }    

    private void OnCardSelected()
    {
        HideChooseUpgradeUI();
    }
    private void HideChooseUpgradeUI()
    {
        rewardsFrame.DOFade(0, 0.25f).OnComplete(() =>
        {
            rewardsFrame.gameObject.SetActive(false);
        });
    }
    
}
