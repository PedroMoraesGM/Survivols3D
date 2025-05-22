using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Quantum;
using System;
using Button = UnityEngine.UI.Button;

public class UpgradeCardItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text choiceText;
    [SerializeField] private Button selectButton;

    private string _upgradeId;

    /// <summary>
    /// Call this to initialize the card with both the simulation data (entry)
    /// and the UI metadata (catalog entry), plus the callback to invoke on click.
    /// </summary>
    public void Setup(int entry, UpgradeCatalog.Entry meta, Action<string> onSelected, int choiceOrder)
    {
        _upgradeId = entry.ToString();
        nameText.text = meta.DisplayName;
        descText.text = meta.Description;
        labelText.text = meta.Label;
        iconImage.sprite = meta.Icon;
        choiceText.text = choiceOrder.ToString();
        if(meta.IconColor != Color.clear) iconImage.color = meta.IconColor;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() =>
        {
            // Play a little feedback
            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 3, 1);
            onSelected(_upgradeId);
        });
    }
}
