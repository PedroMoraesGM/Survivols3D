using UnityEngine;
using UnityEngine.Scripting;
using Quantum;
using Photon.Deterministic;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class SpawnSystem : SystemSignalsOnly, 
        ISignalOnPlayerAdded, ISignalOnGameStateChanged, ISignalOnScoreChanged, ISignalOnGameStarted, ISignalOnGameOver
    {
        public void OnPlayerAdded(Frame f, PlayerRef player, bool firstTime)
        {
            int playerCount = f.ComponentCount<PlayerLink>();
            SpawnPlayer(f, playerCount, player);
        }

        public void OnGameStarted(Frame f)
        {
            if(!f.IsVerified) return;
            //int playerCount = f.ComponentCount<PlayerLink>();
            //int missingPlayers = f.RuntimeConfig.PlayersCount - playerCount;
            //if (missingPlayers <= 0) return;
            //for (int i = 0; i < missingPlayers; i++)
            //{
            //    SpawnAI(f, playerCount+i);
            //}
        }
        public void OnGameStateChanged(Frame f, GameState state)
        {
            if(!f.IsVerified) return;
            if (state == GameState.Countdown)
            {

            }
        }

        public void OnScoreChanged(Frame f, EntityRef ballEntity, EntityRef goalEntity)
        {
            if(!f.IsVerified) return;
        }

        public void OnGameOver(Frame f)
        {
            if(!f.IsVerified) return;
        }

        void SpawnPlayer(Frame f, int index, PlayerRef player)
        {
            var playerEntity = Spawn(f, index);

            var health = f.Unsafe.GetPointer<HealthComponent>(playerEntity);
            health->CurrentHealth = health->MaxHealth;

            var playerLink = new PlayerLink()
            {
                Player = player,
                Class = f.GetPlayerData(player).SelectedClass   // class value is assigned at player customproperties,
            };
            f.Add(playerEntity, playerLink);
        }

        EntityRef Spawn(Frame f, int index)
        {
            EntityRef paddleEntity = f.Create(f.RuntimeConfig.PlayerPrototype);

            if (f.Unsafe.TryGetPointer<Transform3D>(paddleEntity, out var transform))
            {
                transform->Position = new FPVector3(
                    f.RuntimeConfig.GameSize.X/2, 
                    0,
                    index * f.RuntimeConfig.GameSize.Y
                );
            }
            return paddleEntity;
        }
    }
}
