using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActiveUpgradeItem : MonoBehaviour
{

    [Header("Active Upgrade Item")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI upgradeAmountText;
    [SerializeField] private Image[] upgradeIcons;

    [SerializeField] private Image[] upgradePaintableFrames;

    public void InitItem(Sprite icon, Color frameColor, int amount)
    {
        if (upgradeIcons.Length > 0)
        {
            foreach (var upgradeIcon in upgradeIcons)
                upgradeIcon.sprite = icon;
        }

        if (upgradePaintableFrames.Length > 0)
        {
            foreach (var frame in upgradePaintableFrames)
                frame.color = frameColor;
        }

        if (upgradeAmountText != null)
        {
            upgradeAmountText.text = "lv"+amount.ToString();
            
        }
    }    

    public void SetFill(float fillAmount)
    {
        if (fillImage != null)        
            fillImage.fillAmount = fillAmount;        
    }
}
