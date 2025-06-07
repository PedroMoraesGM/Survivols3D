using UnityEngine;
using Quantum;

public class CursorController : MonoBehaviour
{
    private bool _isPaused = false;

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
