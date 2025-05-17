using Photon.Deterministic;
using Tomorrow.Quantum;
using UnityEngine;
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
                Debug.Log("[CharacterSystem] Player will be hit:"+ character->CurrentHealth);
                if (f.Unsafe.TryGetPointer(dealer, out EnemyAI* enemy))
                {
                    character->CurrentHealth -= enemy->Damage;
                    Debug.Log("[CharacterSystem] Player was hit:" + character->CurrentHealth);

                    if (character->CurrentHealth < 0)
                    {
                        Debug.Log("[CharacterSystem] Gameover");
                        f.Signals.OnPlayerDefeated(target, dealer);
                        f.Events.OnPlayerDefeated(target, dealer);
                    }
                }
            }
        }

        public void OnPlayerDefeated(Frame f, EntityRef target, EntityRef dealer)
        {
            
        }

        public override void Update(Frame f, ref Filter filter)
        {
            
        }
    }
}
