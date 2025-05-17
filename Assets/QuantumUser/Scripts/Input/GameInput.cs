using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    private InputActionMap _map;

    private void Awake()
    {
        _map = _inputActions.FindActionMap("Player", true);
    }

    void OnEnable()
    {
        QuantumCallback.Subscribe(this, (CallbackPollInput cb) => PollInput(cb));
    }

    void PollInput(CallbackPollInput callback)
    {
        var qInput = new Quantum.Input();

        // Read mouse look delta
        Vector2 look = _map.FindAction("Look").ReadValue<Vector2>();
        qInput.LookDelta = look.ToFPVector2();             // to fixed-point :contentReference[oaicite:4]{index=4}

        Vector2 move = _map.FindAction("Move").ReadValue<Vector2>();
        qInput.MoveAxis = move.ToFPVector2();

        // Always move forward
        qInput.Forward = true;

        // Submit with repeatable flag
        callback.SetInput(qInput, DeterministicInputFlags.Repeatable);
    }

}
