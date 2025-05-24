using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using UnityEngine;


namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class EnemyAISystem : SystemMainThreadFilter<EnemyAISystem.Filter>, ISignalOnEnemyHit
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public EnemyAI* EnemyAI;
        }

        public override void Update(Frame f, ref Filter enemy)
        {
            if (!f.IsVerified) return;

            // 1) Grab the singleton registry
            var registryComp = f.GetSingleton<PlayerRegistryComponent>();
            var players = f.ResolveList(registryComp.ActivePlayers);

            // 2) Find the closest player
            FP bestDistSqr = FP.MaxValue;
            PlayerInfo bestTarget = default;
            FPVector3 mePos = enemy.Transform->Position;

            for (int i = 0; i < players.Count; i++)
            {
                var info = players[i];
                var delta = info.Position - mePos;
                FP ds = delta.SqrMagnitude;
                if (ds < bestDistSqr)
                {
                    bestDistSqr = ds;
                    bestTarget = info;
                }
            }

            // 3) Chase if found
            if (bestDistSqr < FP.MaxValue && enemy.EnemyAI->CanMove)
            {
                var dir = (bestTarget.Position - mePos).Normalized;
                var moveDelta = dir * enemy.EnemyAI->Speed;
                enemy.Transform->Position += moveDelta;
            }

            // Check if distance is very close to try hit player
            if (enemy.EnemyAI->CloseDamageRange > bestDistSqr)
            {
                f.Signals.OnPlayerHit(bestTarget.Entity, enemy.Entity, enemy.EnemyAI->Damage);
                f.Events.OnPlayerHit(bestTarget.Entity, enemy.Entity, enemy.EnemyAI->Damage);
            }
        }

        public void HitEnemy(Frame f, EntityRef target, EntityRef dealer, FP damage)
        {
            if (f.Unsafe.TryGetPointer(target, out EnemyAI* enemy))
            {
                enemy->Health -= damage;

                if(enemy->Health < 0)
                {
                    f.Signals.OnEnemyDefeated(target,dealer);
                    f.Events.OnEnemyDefeated(target, dealer);
                    f.Destroy(target); 
                    Debug.Log("Enemy destroyed");
                }
            }
        }

        public void OnEnemyHit(Frame f, EntityRef target, EntityRef dealer, FP damage)
        {
            HitEnemy(f, target, dealer, damage);
            
        }
    }
}
