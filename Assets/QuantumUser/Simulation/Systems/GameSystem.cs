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
    public unsafe class GameSystem : SystemMainThread
    {
        public override void Update(Frame f)
        {
            var game = f.Unsafe.GetPointerSingleton<Game>();
            game->Update(f);
        }

        // Spawn
    }
}

