using UnityEngine;
using UnityEngine.Scripting;
using Quantum;
using Photon.Deterministic;
using System.Runtime.Serialization.Formatters;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class SpawnSystem : SystemSignalsOnly, 
        ISignalOnPlayerAdded, ISignalOnGameStateChanged, ISignalOnScoreChanged, ISignalOnGameStarted, ISignalOnGameOver
    {
        public void OnPlayerAdded(Frame f, PlayerRef player, bool firstTime)
        {
            int playerCount = f.ComponentCount<PlayerLink>();
            SpawnPlayer(f, playerCount, player);
        }

        public void OnGameStarted(Frame f)
        {
            if(!f.IsVerified) return;
            //int playerCount = f.ComponentCount<PlayerLink>();
            //int missingPlayers = f.RuntimeConfig.PlayersCount - playerCount;
            //if (missingPlayers <= 0) return;
            //for (int i = 0; i < missingPlayers; i++)
            //{
            //    SpawnAI(f, playerCount+i);
            //}
        }
        public void OnGameStateChanged(Frame f, GameState state)
        {
            if(!f.IsVerified) return;
            if (state == GameState.Countdown)
            {

            }
        }

        public void OnScoreChanged(Frame f, EntityRef ballEntity, EntityRef goalEntity)
        {
            if(!f.IsVerified) return;
        }

        public void OnGameOver(Frame f)
        {
            if(!f.IsVerified) return;
        }

        void SpawnPlayer(Frame f, int index, PlayerRef player)
        {
            var playerEntity = Spawn(f, index);

            // Get the selected class for this player
            var selectedClass = f.GetPlayerData(player).SelectedClass;

            // Get the CharacterClassInfo from the catalog
            var classCatalog = f.FindAsset(f.RuntimeConfig.CharacterClassCatalog);                
            var classInfo = classCatalog.Classes.Find(c => c.Class == selectedClass);

            // Set base stats
            if (f.Unsafe.TryGetPointer<HealthComponent>(playerEntity, out var health))
            {
                health->MaxHealth = classInfo.BaseMaxHealth;
                health->CurrentHealth = classInfo.BaseMaxHealth;
            }
            if (f.Unsafe.TryGetPointer<MoveComponent>(playerEntity, out var move))
            {
                move->BaseSpeed = classInfo.BaseSpeed;
            }
            if (f.Unsafe.TryGetPointer<DamageComponent>(playerEntity, out var damage))
            {
                damage->DamageMultiplier = classInfo.BaseDamageMultiplier;
            }

            // Add PlayerLink
            var playerLink = new PlayerLink()
            {
                Player = player,
                Class = selectedClass
            };
            f.Add(playerEntity, playerLink);

            // Spawn initial weapon if set
            if (classInfo.InitialWeapon != UpgradeId.None)
            {
                // create a weapon entity based on the initial weapon ID
                var upgradeEntry = classCatalog.AllUpgradeEntries.Find(e => e.Id == classInfo.InitialWeapon);
                if (upgradeEntry != null && upgradeEntry.Prefab)
                {
                    // Create the weapon entity from the prefab
                    var weaponEntity = f.Create((AssetRef<EntityPrototype>) upgradeEntry.Prefab);
                    f.Set(weaponEntity, new OwnerData() { OwnerEntity = playerEntity });

                    // Add to acquired upgrades
                    if (f.Unsafe.TryGetPointer<PlayerUpgradeComponent>(playerEntity, out var upgrades))
                    {
                        var acquired = f.ResolveDictionary(upgrades->AcquiredUpgrades);
                        acquired.Add(classInfo.InitialWeapon, new AcquiredUpgradeInfo() {
                            UpgradeEntity = weaponEntity,
                            CountIndex = 1,
                            TotalCount = 1
                        });
                    }
                }
            }
        }

        EntityRef Spawn(Frame f, int index)
        {
            EntityRef paddleEntity = f.Create(f.RuntimeConfig.PlayerPrototype);

            if (f.Unsafe.TryGetPointer<Transform3D>(paddleEntity, out var transform))
            {
                transform->Position = new FPVector3(
                    f.RuntimeConfig.GameSize.X/2, 
                    0,
                    index * f.RuntimeConfig.GameSize.Y
                );
            }
            return paddleEntity;
        }
    }
}
