using System.Collections.Generic;
using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    public unsafe partial struct Game
    {
        public void Update(Frame frame)
        {
            StateTimer.Tick(frame);
            CountdownTimer.Tick(frame);

            switch (CurrentGameState)
            {
                //case GameState.None:
                //    InitializeGame(frame);
                //    break;
                //case GameState.Waiting:
                //    Update_Waiting(frame);
                //    break;
                //case GameState.Countdown:
                //    Update_Countdown(frame);
                //    break;
                //case GameState.Playing:
                //    Update_Playing(frame);
                //    break;
                //case GameState.GameOver:
                //    Update_GameOver(frame);
                //    break;
                //default:
                //    Debug.LogError("Unknown GameState");
                //    break;
            }
        }

        public void Respawn(Frame f)
        {
            CountdownTimer.Start(f.RuntimeConfig.CountdownTime);
            ChangeState(f, GameState.Countdown);
        }

        private void ChangeState(Frame f, GameState state)
        {
            UnityEngine.Debug.Log($"[Game] {state}");
            CurrentGameState = state;
            f.Events.OnGameStateChanged(state);
            f.Signals.OnGameStateChanged(state);
        }

        private void InitializeGame(Frame f)
        {
            ChangeState(f, GameState.Waiting);
        }

        private void StartGame(Frame f)
        {
            CountdownTimer.Start(f.RuntimeConfig.CountdownTime);
            StateTimer.Start(f.RuntimeConfig.GameTime);
            ChangeState(f, GameState.Countdown);
            f.Signals.OnGameStarted();
        }

        private void GameOver(Frame f)
        {
            ChangeState(f, GameState.GameOver);
            StateTimer.Start(f.RuntimeConfig.FinishedTime);
            f.Signals.OnGameOver();
        }

        private void Update_Waiting(Frame f)
        {
            bool allReady = true;
            int playerCount = GetPlayerCount(f);

            foreach (var pair in f.Unsafe.GetComponentBlockIterator<PlayerLink>())
            {
                if (!pair.Component->Ready)
                {
                    allReady = false;
                }
            }

            if ((allReady && playerCount > 0) || playerCount == f.RuntimeConfig.PlayersCount)
            {
                StartGame(f);
            }
        }

        private void Update_Countdown(Frame f)
        {
            if (CountdownTimer.IsDone)
            {
                ChangeState(f, GameState.Playing);
            }
        }

        private void Update_Playing(Frame f)
        {
            if (StateTimer.IsDone)
            {
                GameOver(f);
            }
        }
        private void Update_GameOver(Frame f)
        {
            if (StateTimer.IsDone)
            {
                f.Events.OnGameTerminated();
            }
        }

        private int GetPlayerCount(Frame f)
        {
            int count = 0;

            foreach (var pair in f.Unsafe.GetComponentBlockIterator<PlayerLink>())
            {
                count++;
            }

            return count;
        }
    }
}
