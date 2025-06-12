using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

enum UIState
{
    Waiting,
    Countdown,
    Playing,
    GameOver,
}

[System.Serializable]
struct StateObjectPair
{
    public UIState State;
    public GameObject Object;
}

public unsafe class GameUIController : QuantumCallbacks
{    
    [SerializeField] private List<StateObjectPair> _stateObjectPairs = new();
    private Dictionary<UIState, GameObject> _stateObjectDictionary = new();

    [Header("Waiting")]
    [SerializeField] private TextMeshProUGUI _readyCtaText;    
    [Header("Countdown")]
    [SerializeField] private TextMeshProUGUI _countdownTimer;
    [Header("Gameover")]
    [SerializeField] private CanvasGroup gameoverScreen;
    [SerializeField] private GameObject resetGameTooltip;
    [Header("Playing")]
    [SerializeField] private GameObject crosshairImage;
    [Header("Health UI")]
    [SerializeField] private float healthBlinkDuration = 1;
    [SerializeField] private Image healthVignette;
    [SerializeField] private CanvasGroup healthBarCanvasGroup;
    [SerializeField] private float disableBarDelay = 3;
    [SerializeField] private Image healthBarImage;
    [Header("XP")]
    [SerializeField] private Image xpBarFill;
    [SerializeField] private TextMeshProUGUI xpLevelText;

    private UIState _currentUIState = UIState.Waiting;

    private QuantumGame _game;

    public override void OnUnitySceneLoadDone(QuantumGame game)
    {
        _game = game;
    }

    public override void OnGameStart(QuantumGame game, bool first)
    {
        _game = game;
    }

    private void Awake()
    {        
        QuantumEvent.Subscribe(this, (EventOnGameStateChanged e) => OnGameStateChanged(e));
        QuantumEvent.Subscribe(this, (EventOnScoreChanged e) => OnScoreChanged(e));
        QuantumEvent.Subscribe(this, (EventOnGameTerminated e) => OnGameTerminated(e));
        QuantumEvent.Subscribe(this, (EventOnHit e) => OnPlayerHit(e));
        QuantumEvent.Subscribe(this, (EventOnDefeated e) => OnPlayerDefeated(e));
        QuantumEvent.Subscribe(this, (EventOnGameOver e) => OnGameOver(e));
        QuantumEvent.Subscribe(this, (EventOnXpAdquired e) => OnXpAdqured(e));
        QuantumEvent.Subscribe(this, (EventOnLevelUp e) => OnLevelUp(e));

        foreach (var pair in _stateObjectPairs)
        {
            _stateObjectDictionary.Add(pair.State, pair.Object);
        }
        SetUIState(UIState.Waiting);
    }

    private void OnLevelUp(EventOnLevelUp e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        UpdateXp(f, e.Target);
    }

    private void OnXpAdqured(EventOnXpAdquired e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        UpdateXp(f, e.Target);
    }

    private void UpdateXp(Frame f, EntityRef entity)
    {
        f.TryGet(entity, out XPComponent xPComponent);
        FP nextLevelXp = XPPickupSystem.XPForNextLevel(xPComponent.Level + 1);
        xpBarFill.transform.localScale = new Vector3((xPComponent.CurrentXP / nextLevelXp).AsFloat, 1, 1);
        xpLevelText.text = "Level " + (xPComponent.Level + 1);
    }

    private void OnGameOver(EventOnGameOver e)
    {
        resetGameTooltip.SetActive(true);
    }

    private void OnPlayerDefeated(EventOnDefeated e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        crosshairImage.SetActive(false);
        gameoverScreen.gameObject.SetActive(true);
        gameoverScreen.DOFade(1, 1);
    }

    private void OnPlayerHit(EventOnHit e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        healthVignette.color = Color.white;
        healthVignette.DOKill();
        healthVignette.DOFade(0, healthBlinkDuration);

        f.Unsafe.TryGetPointer(e.Target, out HealthComponent* health);

        healthBarCanvasGroup.DOKill();
        healthBarCanvasGroup.DOFade(1, 0.35f);
        healthBarImage.transform.localScale = new Vector3(Math.Clamp((health->CurrentHealth / health->MaxHealth).AsFloat, 0, 1), 1, 1);

        CancelInvoke(nameof(DisableHealthBar));
        Invoke(nameof(DisableHealthBar), disableBarDelay);
    }

    private void DisableHealthBar()
    {
        healthBarCanvasGroup.DOKill();
        healthBarCanvasGroup.DOFade(0, 0.35f);
    }

    private void Update()
    {
        switch (_currentUIState)
        {
            default:
            case UIState.Waiting:
                break;
            case UIState.Countdown:
                UpdateCountdownText();
                UpdatePlayingText();
                break;
            case UIState.Playing:
                UpdatePlayingText();
                break;
        }
    }

    private void OnGameStateChanged(EventOnGameStateChanged e)
    {
        switch (e.state)
        {
            case GameState.Waiting:
                SetUIState(UIState.Waiting);             
                break;
            case GameState.Countdown:
                SetUIState(UIState.Countdown);
                break;
            case GameState.Playing:
                SetUIState(UIState.Playing);
                break;
            case GameState.GameOver:
                SetUIState(UIState.GameOver);
                break;
            default:
                Debug.LogWarning("Unhandled UI state");
                break;
        }
    }

    private void OnScoreChanged(EventOnScoreChanged e)
    {
        
    }

    private void OnGameTerminated(EventOnGameTerminated e)
    {
        QuantumRunner.ShutdownAll();
        SceneManager.LoadSceneAsync(0);
    }
    
    private void SetUIState(UIState state)
    {
        foreach (KeyValuePair<UIState, GameObject> pair in _stateObjectDictionary)
        {
            pair.Value.SetActive(pair.Key == state);
        }

        _currentUIState = state;
    }

    private void UpdatePlayingText()
    {
        if (_game.Frames.Predicted == null) { return; }
    }

    private void UpdateCountdownText()
    {
        if (_game.Frames.Predicted == null) { return; }
        FP timeleft = _game.Frames.Predicted.Unsafe.GetPointerSingleton<Game>()->CountdownTimer.TimeLeft;
        UpdateCountdownText(timeleft);
    }
    
    private void UpdateCountdownText(FP time)
    {
        _countdownTimer.text = FPMath.CeilToInt(time).ToString();
    }

}
