using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;
using UnityEngine;


namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class EnemyAISystem : SystemMainThreadFilter<EnemyAISystem.Filter>, ISignalOnHit
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public EnemyAI* EnemyAI;
            public StatusEffectComponent* StatusEffect;
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
                var moveDelta = dir * enemy.EnemyAI->Speed * enemy.StatusEffect->SlowMultiplier;
                enemy.Transform->LookAt(bestTarget.Position);
                enemy.Transform->Position += moveDelta;
            }

            // Check if disatance is close enough to shoot
            if(f.Unsafe.TryGetPointer(enemy.Entity, out ShootingWeaponComponent* shootingWeapon))
            {
                shootingWeapon->CanShoot = bestDistSqr <= enemy.EnemyAI->ShootRangeDistance;
            }

            // Check if distance is very close to try hit player
            if (enemy.EnemyAI->CloseDamageRange > bestDistSqr)
            {
                f.Signals.OnHit(bestTarget.Entity, enemy.Entity, enemy.EnemyAI->Damage);
                f.Events.OnHit(bestTarget.Entity, enemy.Entity, enemy.EnemyAI->Damage);
            }
        }

        public void HitEnemy(Frame f, EntityRef target, EntityRef dealer, FP damage)
        {
            if (f.Unsafe.TryGetPointer(target, out EnemyAI* enemy))
            {
                f.Unsafe.TryGetPointer(target, out HealthComponent* health);
                health->CurrentHealth = FPMath.Clamp(health->CurrentHealth - damage, 0, health->MaxHealth);
            
                if(health->CurrentHealth <= 0)
                {
                    f.Signals.OnDefeated(target, dealer);
                    f.Events.OnDefeated(target, dealer);
                    f.Destroy(target); 
                    Debug.Log("Enemy destroyed");
                }
            }
        }

        public void OnHit(Frame f, EntityRef target, EntityRef dealer, FP damage)
        {
            HitEnemy(f, target, dealer, damage);
        }
    }
}
