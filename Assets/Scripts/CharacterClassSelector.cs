using DG.Tweening;
using ExitGames.Client.Photon;
using Photon.Client;
using Quantum;
using Quantum.Menu;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class CharacterClassSelector : QuantumMenuUIScreen
{
    private const string PREF_KEY = "SelectedCharacterClass";

    [Header("UI Buttons")]
    [SerializeField] private Button tankButton;
    [SerializeField] private Button assassinButton;
    [SerializeField] private Button mageButton;
    [SerializeField] private Button healerButton;

    [Header("Selected Overlays")]
    [SerializeField] private CanvasGroup tankSelectedOverlay;
    [SerializeField] private CanvasGroup assassinSelectedOverlay;
    [SerializeField] private CanvasGroup mageSelectedOverlay;
    [SerializeField] private CanvasGroup healerSelectedOverlay;
    [SerializeField] private float selectionFadeDuration;
    
    [Header("Menu Config")]
    [SerializeField] private QuantumMenuConfig menuConfig;

    private CharacterClass _currentClass;

    public override void Start()
    {
        base.Start();
        // Wire up button clicks
        tankButton.onClick.AddListener(() => OnClassButtonClicked(CharacterClass.Tank));
        assassinButton.onClick.AddListener(() => OnClassButtonClicked(CharacterClass.Assassin));
        mageButton.onClick.AddListener(() => OnClassButtonClicked(CharacterClass.Mage));
        healerButton.onClick.AddListener(() => OnClassButtonClicked(CharacterClass.Healer));

        // Load saved selection 
        int saved = PlayerPrefs.GetInt(PREF_KEY, (int)CharacterClass.Tank);
        _currentClass = (CharacterClass)Mathf.Clamp(saved, 0, 3);

        // Update UI to reflect loaded value
        RefreshUI();
    }

    private void OnClassButtonClicked(CharacterClass chosen)
    {
        if (_currentClass == chosen) return;
        _currentClass = chosen;
        // Persist
        PlayerPrefs.SetInt(PREF_KEY, (int)_currentClass);
        PlayerPrefs.Save();
        // Update visuals
        RefreshUI();
    }

    private void RefreshUI()
    {
        // show/hide overlay GameObjects
        tankSelectedOverlay.DOFade(_currentClass == CharacterClass.Tank ? 1 : 0, selectionFadeDuration);
        assassinSelectedOverlay.DOFade(_currentClass == CharacterClass.Assassin ? 1 : 0, selectionFadeDuration);
        mageSelectedOverlay.DOFade(_currentClass == CharacterClass.Mage ? 1 : 0, selectionFadeDuration);
        healerSelectedOverlay.DOFade(_currentClass == CharacterClass.Healer ? 1 : 0, selectionFadeDuration);
    }
}
