using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using Quantum.Collections;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class PlayerRegistryResetSystem : SystemMainThread
    {
        public override void Update(Frame f)
        {
            // Grab the singleton component...
            var registry = f.GetSingleton<PlayerRegistryComponent>();
            // ...resolve its QList<PlayerInfo> and clear it
            var list = f.ResolveList(registry.ActivePlayers);
            list.Clear();
        }
    }
}
