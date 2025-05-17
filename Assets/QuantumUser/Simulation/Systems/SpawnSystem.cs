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
            // comlete missing players with AI
            int playerCount = f.ComponentCount<PlayerLink>();
            int missingPlayers = f.RuntimeConfig.PlayersCount - playerCount;
            if (missingPlayers <= 0) return;
            for (int i = 0; i < missingPlayers; i++)
            {
                SpawnAI(f, playerCount+i);
            }
        }
        public void OnGameStateChanged(Frame f, GameState state)
        {
            if(!f.IsVerified) return;
            if (state == GameState.Countdown)
            {
                // spawn the ball
                EntityRef ballEntity = f.Create(f.RuntimeConfig.BallPrototype);
                f.Add(ballEntity, new Ball());
                if (f.Unsafe.TryGetPointer<Transform3D>(ballEntity, out var ballTransform))
                {
                    RespawnBall(f, ballEntity);
                }
            }
        }

        public void OnScoreChanged(Frame f, EntityRef ballEntity, EntityRef goalEntity)
        {
            if(!f.IsVerified) return;
            if (f.Unsafe.TryGetPointer<Ball>(ballEntity, out Ball *ball))
            {
        
            }
        }

        public void OnGameOver(Frame f)
        {
            if(!f.IsVerified) return;
            foreach (var pair in f.GetComponentIterator<Ball>())
            {
                f.Destroy(pair.Entity);
            }
        }

        void RespawnBall(Frame f, EntityRef ballEntity)
        {
            // reset position
            if (f.Unsafe.TryGetPointer<Transform3D>(ballEntity, out var ballTransform))
            {
                ballTransform->Position = new FPVector3(
                    f.RuntimeConfig.GameSize.X/2, 
                    0,
                    f.RuntimeConfig.GameSize.Y/2
                );
            }
            // reset physics
            if (f.Unsafe.TryGetPointer<Ball>(ballEntity, out var ball))
            {
                ball->Velocity = f.RuntimeConfig.BallSpeed * FPVector3.Forward;
            }
        }

        void SpawnPlayer(Frame f, int index, PlayerRef player)
        {
            var paddleEntity = Spawn(f, index);
            var playerLink = new PlayerLink()
            {
                Player = player,
            };
            f.Add(paddleEntity, playerLink);
        }

        void SpawnAI(Frame f, int index)
        {
            //var paddleEntity = Spawn(f, index);
            //var playerAI = new PlayerAI();
            //f.Add(paddleEntity, playerAI);
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
