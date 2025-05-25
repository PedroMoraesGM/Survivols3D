using Photon.Deterministic;
using Tomorrow.Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Quantum
{
    [Preserve]
    public unsafe class CharacterSystem : SystemMainThreadFilter<CharacterSystem.Filter>, ISignalOnHit, ISignalOnDefeated
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public Character* Character;
            public HealthComponent* Health;
            public PlayerLink* Link;  
        }

        public override void Update(Frame f, ref Filter filter)
        {
            Input input = default;
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* link))
            {
                input = *f.GetPlayerInput(link->Player);
            }

            if (input.Reset && filter.Health->IsDead)
            {
                // Tell the view layer to disconnect & go to menu
                f.Events.OnRequestDisconnect(filter.Entity);
            }
        }

        public void OnHit(Frame f, EntityRef target, EntityRef dealer, FP damage)
        {
            if (f.Unsafe.TryGetPointer(target, out Character* character))
            {
                if (f.Unsafe.TryGetPointer(dealer, out EnemyAI* enemy))
                {
                    f.Unsafe.TryGetPointer(target, out HealthComponent* health);

                    health->CurrentHealth -= enemy->Damage;

                    if (health->CurrentHealth < 0 && !health->IsDead)
                    {
                        f.Signals.OnDefeated(target, dealer);
                        f.Events.OnDefeated(target, dealer);
                    }
                }
            }
        }

        public void OnDefeated(Frame f, EntityRef target, EntityRef dealer)
        {
            if (f.Unsafe.TryGetPointer(target, out Character* character))
            {
                f.Unsafe.TryGetPointer(target, out HealthComponent* health);
                health->IsDead = true;
            }
        }
    }
}
