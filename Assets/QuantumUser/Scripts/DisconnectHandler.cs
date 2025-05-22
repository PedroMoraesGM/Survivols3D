using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectHandler : MonoBehaviour
{
    private void OnEnable()
    {
        QuantumEvent.Subscribe(this, (EventOnRequestDisconnect e) => HandleDisconnect());
    }

    private void HandleDisconnect()
    {
        // tood: call this only to dead Entity
        // Stop Quantum runner
        if (QuantumRunner.Default != null && QuantumRunner.Default.Game != null)
        {
            QuantumRunner.Default.Shutdown();
        }

        // Load Menu scene (index 0 or name)
        SceneManager.LoadScene("Menu");
    }
}
