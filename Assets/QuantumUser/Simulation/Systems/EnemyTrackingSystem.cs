using Tomorrow.Quantum;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum
{
    [Preserve]
    public unsafe class EnemyTrackingSystem : SystemMainThreadFilter<EnemyTrackingSystem.Filter>
    {        public struct Filter
        {
            public EntityRef Entity;
            public EnemyAI* Enemy;
            public Transform3D* Transform;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            //// Grab the singleton registry
            //var registry = f.GetSingleton<EnemyRegistryComponent>();
            //// Resolve its list in the frame
            //var list = f.ResolveList(registry.ActiveEnemies);

            //// Add this player’s info
            //list.Add(new EnemyInfo
            //{
            //    Entity = filter.Entity,
            //    Position = filter.Transform->Position
            //});
        }
    }
}
