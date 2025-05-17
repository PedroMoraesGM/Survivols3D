using Quantum;
using UnityEngine.Scripting;

namespace Tomorrow.Quantum
{
    [Preserve]
    public class EnemyResetSystem : SystemMainThread
    {
        public override void Update(Frame f)
        {
            //// Grab the singleton component...
            //var registry = f.GetSingleton<EnemyRegistryComponent>();
            //// ...resolve its QList<PlayerInfo> and clear it
            //var list = f.ResolveList(registry.ActiveEnemies);
            //list.Clear();
        }
    }
}
