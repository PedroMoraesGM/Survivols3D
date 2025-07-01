using UnityEngine;
using UnityEngine.Scripting;
using Photon;
using Photon.Deterministic;
using Quantum;
using Quantum.Collections;

using Input = Quantum.Input;

namespace Tomorrow.Quantum
{
    [Preserve]
    public unsafe class GameSystem : SystemMainThread, ISignalOnDefeated
    {
        public void OnDefeated(Frame f, EntityRef target, EntityRef dealer)
        {
            // Check if all players are dead
            // Then trigger OnGameOver() signal and event -> Which should trigger reset game UI
            var registryComp = f.GetSingleton<PlayerRegistryComponent>();
            var players = f.ResolveList(registryComp.ActivePlayers);

            foreach (var player in players)
            {
                if (f.Unsafe.TryGetPointer<Character>(player.Entity, out Character* p))
                {
                    f.Unsafe.TryGetPointer<HealthComponent>(player.Entity, out HealthComponent* h);
                    if (!h->IsDead) return; // If anyone isn't dead we return without callbacks
                }
            }

            f.Signals.OnGameOver();
            f.Events.OnGameOver();

            var game = f.Unsafe.GetPointerSingleton<Game>();
            game->CurrentGameState = GameState.GameOver;
        }
        private void InitializeUpgradeData(Frame f)
        {
            var upgradeData = f.GetSingleton<UpgradeDataComponent>();
            var classCatalog = f.RuntimeConfig.CharacterClassCatalog;
            var dict = f.ResolveDictionary(upgradeData.EntriesPerClass);

            dict.Clear();
            var classesCatalog = f.FindAsset(classCatalog);

            foreach (var classInfo in classesCatalog.Classes)
            {
                // Allocate a Quantum list for UpgradeEntry
                var qList = f.AllocateList<UpgradeEntry>();

                foreach (var pool in classInfo.UpgradesIdsPool)
                {
                    // Find the master entry for this UpgradeId
                    var entryData = classesCatalog.AllUpgradeEntries.Find(e => e.Id == pool.UpgradeId);
                    if (entryData == null)
                        continue; // Skip if not found

                    // Allocate a Quantum list for WeaponUpgradeEffects
                    var effectsList = f.AllocateList<WeaponUpgradeEffects>();

                    if (entryData.EffectsPerExtraUpgrade != null)
                    {
                        foreach (var effectsData in entryData.EffectsPerExtraUpgrade)
                        {
                            // Allocate a Quantum list for WeaponUpgradeEffect
                            var effectList = f.AllocateList<WeaponUpgradeEffect>();

                            if (effectsData.Effects != null)
                            {
                                foreach (var effectData in effectsData.Effects)
                                {
                                    var effect = new WeaponUpgradeEffect
                                    {
                                        Type = effectData.Type,
                                        Value = effectData.Value
                                    };
                                    effectList.Add(effect);
                                }
                            }

                            var weaponUpgradeEffects = new WeaponUpgradeEffects
                            {
                                Effects = effectList
                            };
                            effectsList.Add(weaponUpgradeEffects);
                        }
                    }

                    // Convert UnityEngine.Object prefab to AssetRef<EntityPrototype>
                    AssetRef<EntityPrototype> prefabRef = (AssetRef<EntityPrototype>)entryData.Prefab;

                    // Use class-specific MinLevel/Weight if set, otherwise use entryData's
                    int minLevel = pool.MinLevel != -1 ? pool.MinLevel : entryData.MinLevel;
                    var weight = pool.Weight != -1 ? pool.Weight : entryData.Weight;

                    var upgradeEntry = new UpgradeEntry
                    {
                        Prefab = prefabRef,
                        Id = entryData.Id,
                        MinLevel = minLevel,
                        Weight = weight,
                        CanBeRepeated = entryData.CanBeRepeated,
                        EffectsPerExtraUpgrade = effectsList
                    };

                    qList.Add(upgradeEntry);
                }

                var classEntries = new ClassEntries { Entries = qList };
                dict.Add(classInfo.Class, classEntries);
            }
        }

        public override void OnInit(Frame f)
        {
            base.OnInit(f);

            // Initialize upgrade data
            InitializeUpgradeData(f);
        }        

        public override void Update(Frame f)
        {
            var game = f.Unsafe.GetPointerSingleton<Game>();
            game->Update(f);
        }
    }
}

