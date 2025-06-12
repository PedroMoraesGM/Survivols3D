using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    private PauseController pauseController;
    private InputActionMap _map;

    private void Awake()
    {
        pauseController = FindFirstObjectByType<PauseController>();
        _map = _inputActions.FindActionMap("Player", true);
    }

    void OnEnable()
    {
        QuantumCallback.Subscribe(this, (CallbackPollInput cb) => PollInput(cb));
    }

    public InputAction GetPauseAction()
    {
        return _map.FindAction("Pause");
    }

    void PollInput(CallbackPollInput callback)
    {
        if (pauseController.IsPaused) return;

        var qInput = new Quantum.Input();

        // Read mouse look delta
        Vector2 look = _map.FindAction("Look").ReadValue<Vector2>();
        qInput.LookDelta = look.ToFPVector2();             // to fixed-point :contentReference[oaicite:4]{index=4}

        Vector2 move = _map.FindAction("Move").ReadValue<Vector2>();
        qInput.MoveAxis = move.ToFPVector2();

        qInput.Reset = _map.FindAction("Reset").WasPressedThisFrame(); // if we press button "Reset"

        qInput.ChoiceFirst = _map.FindAction("ChoiceFirst").WasPressedThisFrame();
        qInput.ChoiceSecond = _map.FindAction("ChoiceSecond").WasPressedThisFrame();
        qInput.ChoiceThird = _map.FindAction("ChoiceThird").WasPressedThisFrame();

        // Submit with repeatable flag
        callback.SetInput(qInput, DeterministicInputFlags.Repeatable);

    }
}
