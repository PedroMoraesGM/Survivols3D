using UnityEngine;
using UnityEngine.Scripting;
using Photon;
using Photon.Deterministic;
using Quantum;
using Quantum.Collections;

using Input = Quantum.Input;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class GameSystem : SystemMainThread, ISignalOnDefeated
    {
        public void OnDefeated(Frame f, EntityRef target, EntityRef dealer)
        {
            // Check if all players are dead
            // Then trigger OnGameOver() signal and event -> Which should trigger reset game UI
            var registryComp = f.GetSingleton<PlayerRegistryComponent>();
            var players = f.ResolveList(registryComp.ActivePlayers);

            foreach (var player in players)
            {
                if (f.Unsafe.TryGetPointer<Character>(player.Entity, out Character* p))
                {
                    f.Unsafe.TryGetPointer<HealthComponent>(player.Entity, out HealthComponent* h);
                    if (!h->IsDead) return; // If anyone isn't dead we return without callbacks
                }
            }

            f.Signals.OnGameOver();
            f.Events.OnGameOver();

            var game = f.Unsafe.GetPointerSingleton<Game>();
            game->CurrentGameState = GameState.GameOver;
        }

        public override void Update(Frame f)
        {
            var game = f.Unsafe.GetPointerSingleton<Game>();
            game->Update(f);
        }
    }
}

