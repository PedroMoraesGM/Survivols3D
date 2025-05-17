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

    [Header("Playing")]
    [SerializeField] private TextMeshProUGUI _timeLeftText;
    [SerializeField] private TextMeshProUGUI[] _scoreTexts;
    [Header("Gameover")]
    [SerializeField] private CanvasGroup gameoverScreen;
    [Header("Health UI")]
    [SerializeField] private float healthBlinkDuration = 1;
    [SerializeField] private Image healthVignette;
    [SerializeField] private CanvasGroup healthBarCanvasGroup;
    [SerializeField] private float disableBarDelay = 3;
    [SerializeField] private Image healthBarImage;

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
        QuantumEvent.Subscribe(this, (EventOnPlayerHit e) => OnPlayerHit(e));
        QuantumEvent.Subscribe(this, (EventOnPlayerDefeated e) => OnPlayerDefeated(e));
        foreach (var pair in _stateObjectPairs)
        {
            _stateObjectDictionary.Add(pair.State, pair.Object);
        }
        SetUIState(UIState.Waiting);
    }

    private void OnPlayerDefeated(EventOnPlayerDefeated e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        gameoverScreen.gameObject.SetActive(true);
        gameoverScreen.DOFade(1, 1);
    }

    private void OnPlayerHit(EventOnPlayerHit e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Target, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        healthVignette.color = Color.white;
        healthVignette.DOKill();
        healthVignette.DOFade(0, healthBlinkDuration);

        f.Unsafe.TryGetPointer(e.Target, out Character* character);

        healthBarCanvasGroup.DOKill();
        healthBarCanvasGroup.DOFade(1, 0.35f);
        healthBarImage.transform.localScale = new Vector3(Math.Clamp((character->CurrentHealth / character->MaxHealth).AsFloat, 0, 1), 1, 1);

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
                foreach(TextMeshProUGUI t in _scoreTexts)
                {
                    t.text = "0";
                }
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
        _scoreTexts[e.playerIndex].text = e.score.ToString();
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

        FP timeleft = _game.Frames.Predicted.Unsafe.GetPointerSingleton<Game>()->StateTimer.TimeLeft;
        if(timeleft > 0)
        {
            int minutes = FPMath.FloorToInt(timeleft / 60);
            int seconds = FPMath.FloorToInt(timeleft) % 60;
            _timeLeftText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            // int milliSeconds = FPMath.FloorToInt(timeleft % 1 * 100);
            // _timeLeftText.text = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliSeconds);
        }
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
