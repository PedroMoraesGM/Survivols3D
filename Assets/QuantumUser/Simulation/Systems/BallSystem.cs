using UnityEngine;
using UnityEngine.Scripting;
using Quantum;
using Photon.Deterministic;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class BallSystem : SystemMainThreadFilter<BallSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Ball* Ball;
            public PhysicsBody3D* Body;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            var game = f.Unsafe.GetPointerSingleton<Game>();
            if(game->CurrentGameState == GameState.Playing)
            {
                filter.Body->Velocity = f.RuntimeConfig.BallSpeed * filter.Ball->Velocity;
            }
        }
    }
}
