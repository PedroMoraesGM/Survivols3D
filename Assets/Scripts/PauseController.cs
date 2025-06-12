using UnityEngine;
using Quantum;
using System;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameInput gameInput;
    private bool _isPaused = false;

    public Action<bool> GamePauseChanged;

    public bool IsPaused { get { return _isPaused; } }

    private void Awake()
    {
        QuantumEvent.Subscribe(this, (EventOnGameStateChanged e) => {
            _isPaused = (e.state != GameState.Playing);
            UpdateCursor();
        });

        QuantumEvent.Subscribe(this, (EventOnRequestDisconnect e) => OnRequestDisconnect(e));

        // PauseMenu.OnPaused   += () => { _isPaused = true;  UpdateCursor(); };
        // PauseMenu.OnResumed  += () => { _isPaused = false; UpdateCursor(); };
    }

    public void Resume()
    {
        _isPaused = false;
        UpdateCursor();
    }

    public void Pause()
    {
        _isPaused = !_isPaused;
        UpdateCursor();

        GamePauseChanged?.Invoke(_isPaused);
    }

    private void Update()
    {
        if(gameInput.GetPauseAction().WasPressedThisFrame())
        {
            Pause();
        }
    }

    private void OnDestroy()
    {
        // PauseMenu.OnPaused   -= ...
        // PauseMenu.OnResumed  -= ...
    }

    private void OnRequestDisconnect(EventOnRequestDisconnect e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Entity, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        _isPaused = true;
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        if (_isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // When un-paused: lock & hide, automatically centers on most platforms
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Start()
    {
        // Initialize at startup
        UpdateCursor();
    }
}
