using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectHandler : QuantumEntityViewComponent
{
    private void OnEnable()
    {
        QuantumEvent.Subscribe(this, (EventOnRequestDisconnect e) => HandleDisconnect(e));
    }

    private void HandleDisconnect(EventOnRequestDisconnect e)
    {
        var f = e.Game.Frames.Verified;
        if (!f.TryGet(e.Entity, out PlayerLink playerLink)) return;
        if (!e.Game.PlayerIsLocal(playerLink.Player)) return;

        // Stop Quantum runner
        if (QuantumRunner.Default != null && QuantumRunner.Default.Game != null)
        {
            QuantumRunner.Default.Shutdown();
        }

        // Load Menu scene (index 0 or name)
        SceneManager.LoadScene("Menu");
    }
}
