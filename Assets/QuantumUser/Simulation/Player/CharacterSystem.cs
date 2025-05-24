using Photon.Deterministic;
using Tomorrow.Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Quantum
{
    [Preserve]
    public unsafe class CharacterSystem : SystemMainThreadFilter<CharacterSystem.Filter>, ISignalOnPlayerHit, ISignalOnPlayerDefeated
    {
        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public Character* Character;
            public PlayerLink* Link;  
        }

        public void OnPlayerHit(Frame f, EntityRef target, EntityRef dealer, FP damage)
        {
            if (f.Unsafe.TryGetPointer(target, out Character* character))
            {
                if (f.Unsafe.TryGetPointer(dealer, out EnemyAI* enemy))
                {
                    character->CurrentHealth -= enemy->Damage;

                    if (character->CurrentHealth < 0 && !character->IsDead)
                    {
                        f.Signals.OnPlayerDefeated(target, dealer);
                        f.Events.OnPlayerDefeated(target, dealer);
                    }
                }
            }
        }

        public void OnPlayerDefeated(Frame f, EntityRef target, EntityRef dealer)
        {
            if (f.Unsafe.TryGetPointer(target, out Character* character))
            {
                character->IsDead = true;
            }
        }

        public override void Update(Frame f, ref Filter filter)
        {
            Input input = default;
            if (f.Unsafe.TryGetPointer(filter.Entity, out PlayerLink* link))
            {
                input = *f.GetPlayerInput(link->Player);
            }

            if (input.Reset && filter.Character->IsDead)
            {
                // Tell the view layer to disconnect & go to menu
                f.Events.OnRequestDisconnect(filter.Entity);
            }
        }
    }
}
