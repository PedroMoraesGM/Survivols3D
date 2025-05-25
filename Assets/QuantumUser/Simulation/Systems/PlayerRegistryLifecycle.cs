using UnityEngine.Scripting;
using Quantum;
using UnityEngine;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class PlayerRegistryLifecycle :
      SystemSignalsOnly,
      ISignalOnComponentAdded<PlayerRegistryComponent>,
      ISignalOnComponentRemoved<PlayerRegistryComponent>
    {
        public void OnAdded(Frame f, EntityRef e, PlayerRegistryComponent* c)
        {
            //c->ActivePlayers = f.AllocateList<PlayerInfo>(16);
            // Allocate an empty list in the frame heap
            c->ActivePlayers = f.AllocateList<PlayerInfo>();
        }

        public void OnRemoved(Frame f, EntityRef e, PlayerRegistryComponent* c)
        {
            // Free & nullify so Quantum can serialize correctly
            f.FreeList(c->ActivePlayers);
            c->ActivePlayers = default;
        }
    }
}
