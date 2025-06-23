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

    public int UpgradeId;

    /// <summary>
    /// Call this to initialize the card with both the simulation data (entry)
    /// and the UI metadata (catalog entry), plus the callback to invoke on click.
    /// </summary>
    public void Setup(int entry, UpgradeCatalog.Entry meta, AcquiredUpgradeInfo upgradeInfo, Action<int> onSelected, int choiceOrder)
    {
        UpgradeId = entry;
        nameText.text = meta.DisplayName;
        if (upgradeInfo.TotalCount == 0)
            descText.text = meta.Description;
        else
            descText.text = meta.DescriptionPerUpgrade[upgradeInfo.CountIndex - 1];

        if (upgradeInfo.TotalCount == 0)
            labelText.text = "<color=#FDD143>New !</color>";
        else
            labelText.text = $"Level {upgradeInfo.TotalCount + 1}";
        
        iconImage.sprite = meta.Icon;
        choiceText.text = choiceOrder.ToString();
        if(meta.IconColor != Color.clear) iconImage.color = meta.IconColor;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() =>
        {
            // Play a little feedback
            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 3, 1);
            onSelected(UpgradeId);
        });
    }

    /// <summary>
    /// Plays feedback (e.g. a punch-scale + color flash), then invokes onComplete.
    /// </summary>
    public void PlaySelectionEffect(Action onComplete)
    {        
        transform
          .DOPunchScale(Vector3.one * 0.2f, 0.3f, vibrato: 2, elasticity: 0.5f)
          .OnComplete(() =>
          {
              // optional: flash background
              var img = GetComponent<Image>();
              if (img != null)
              {
              img
                .DOColor(Color.yellow, 0.15f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => onComplete());
              }
              else
              {
                  onComplete();
              }
          });
    }
}
