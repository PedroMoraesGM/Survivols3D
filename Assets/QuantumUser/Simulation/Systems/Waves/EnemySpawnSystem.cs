using UnityEngine.Scripting;
using Photon.Deterministic;
using Quantum;

[Preserve]
public unsafe class EnemySpawnSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        var spawner = f.Unsafe.GetPointerSingleton<SpawnerComponent>();
        var progression = f.Unsafe.GetPointerSingleton<ProgressionComponent>();
        var waveData = f.GetSingleton<EnemyWaveComponent>();
        var entries = f.ResolveList(waveData.Entries);

        if (f.Unsafe.GetPointerSingleton<Game>()->CurrentGameState == GameState.GameOver) return;

        // Only advance spawn if below threshold and timer elapsed
        int activeCount = CountActiveEnemies(f);  // block-iterator count
        spawner->TimeSinceLastSpawn += f.DeltaTime;

        if (activeCount < spawner->CurrentBatchSize &&
            spawner->TimeSinceLastSpawn >= spawner->CurrentInterval)
        {
            spawner->TimeSinceLastSpawn -= spawner->CurrentInterval;

            // 1) Build a list of eligible entries
            FP totalWeight = FP._0;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.MinWave <= progression->Cycle)
                {
                    totalWeight += e.Weight;
                }
            }

            // 2) For each spawn slot, pick a random type
            for (int s = 0; s < spawner->CurrentBatchSize; s++)
            {
                FP choice = f.RNG->Next() * totalWeight;
                FP accum = FP._0;
                AssetRef<EntityPrototype> selectedPrefab = default;

                // Weighted selection
                for (int j = 0; j < entries.Count; j++)
                {
                    var e = entries[j];
                    if (e.MinWave > progression->Cycle) continue;
                    accum += e.Weight;
                    if (choice <= accum)
                    {
                        selectedPrefab = e.Prefab;
                        break;
                    }
                }

                // 3) Spawn the selected enemy around a random player
                FPVector3 spawnPos = GetRandomPlayerPosition(f);
                var enemy = f.Create(selectedPrefab);

                // Given an angle in radians (FP angle):
                var angle = (f.RNG->Next() * 2 * FP.Pi);
                FP sin, cos;
                FPMath.SinCos(angle, out sin, out cos);

                var offset = new FPVector3(cos, FP._0, sin) * spawner->SpawnRadius;
                var pos = spawnPos + offset;

                f.Unsafe.GetPointer<Transform3D>(enemy)->Position = pos;

                // Initialize health & damage as before
                var enemyAi = f.Unsafe.GetPointer<EnemyAI>(enemy);
                enemyAi->Health *= spawner->HPMultiplier;
                enemyAi->Damage *= spawner->DamageMultiplier;
            }
        }
    }

    //public override void Update(Frame f)
    //{
    //    var spawner = f.Unsafe.GetPointerSingleton<SpawnerComponent>();

    //    // Advance timers
    //    spawner->TimeSinceLastSpawn += f.DeltaTime;

    //    int activeCount = CountActiveEnemies(f);

    //    // If below threshold, and interval elapsed
    //    if (activeCount < spawner->CurrentBatchSize &&
    //        spawner->TimeSinceLastSpawn >= spawner->CurrentInterval)
    //    {
    //        spawner->TimeSinceLastSpawn -= spawner->CurrentInterval;

    //        // Spawn 'batch' enemies
    //        for (int i = 0; i < spawner->CurrentBatchSize; i++)
    //        {
    //            // Instantiate prefab (set up in SpawnerComponent.Settings)
    //            var enemy = f.Create(spawner->Settings.EnemyPrefab);

    //            // Position randomly around player, e.g. circle radius R
    //            var playerPos = GetRandomPlayerPosition(f);
    //            var angle = (f.RNG->Next() * 2 * FP.Pi);

    //            // Given an angle in radians (FP angle):
    //            FP sin, cos;
    //            FPMath.SinCos(angle, out sin, out cos);

    //            var offset = new FPVector3(cos, FP._0, sin) * spawner->SpawnRadius;

    //            var pos = playerPos + offset;

    //            f.Unsafe.GetPointer<Transform3D>(enemy)->Position = pos;

    //            // Initialize stats
    //            var enemyAi = f.Unsafe.GetPointer<EnemyAI>(enemy);
    //            enemyAi->Health *= spawner->HPMultiplier;
    //            enemyAi->Damage *= spawner->DamageMultiplier;

    //            //// Tag it as active
    //            //f.AddComponent<EnemyTagComponent>(enemy);
    //        }
    //    }
    //}

    public static int CountActiveEnemies(Frame f)
    {
        int total = 0;
        foreach (var block in f.Unsafe.GetComponentBlockIterator<EnemyAI>())
        {
            total++;
        }
        return total;
    }


    private FPVector3 GetRandomPlayerPosition(Frame f)
    {
        // Fetch the singleton registry component
        var registry = f.GetSingleton<PlayerRegistryComponent>();
        // Resolve the frame?local list of PlayerInfo
        var players = f.ResolveList(registry.ActivePlayers);

        // If no players, bail out
        int count = players.Count;
        if (count == 0)
        {
            return FPVector3.Zero;
        }

        // Pick a random index [0, count)
        int index = f.Global->RngSession.Next(0, count);

        // Return that player’s position
        return players[index].Position;
    }
}
