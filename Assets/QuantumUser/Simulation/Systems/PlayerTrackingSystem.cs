using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using Quantum.Collections;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class PlayerTrackingSystem : SystemMainThreadFilter<PlayerTrackingSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public PlayerLink* Link;
            public Transform3D* Transform;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            // Only include real players
            if (filter.Link->Player == PlayerRef.None)
                return;

            // Grab the singleton registry
            var registry = f.GetSingleton<PlayerRegistryComponent>();
            // Resolve its list in the frame
            var list = f.ResolveList(registry.ActivePlayers);

            // Add this player’s info
            list.Add(new PlayerInfo
            {
                Entity = filter.Entity,
                Position = filter.Transform->Position
            });
        }
    }
}
