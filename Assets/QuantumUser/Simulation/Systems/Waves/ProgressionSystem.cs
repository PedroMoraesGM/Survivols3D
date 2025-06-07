using Photon.Deterministic;
using Quantum;
using UnityEngine.Scripting;

[Preserve]
public unsafe class ProgressionSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        var prog = f.Unsafe.GetPointerSingleton<ProgressionComponent>();
        var spawner = f.Unsafe.GetPointerSingleton<SpawnerComponent>();

        // Advance total time
        prog->TimeElapsed += f.DeltaTime;

        // When we hit a cycle boundary
        if (prog->TimeElapsed >= prog->CycleDuration)
        {
            prog->TimeElapsed -= prog->CycleDuration;
            prog->Cycle++;

            // Apply scaling per Vampire Survivors:
            // Interval *= 0.67 (i.e. -33%), BatchSize *= 1.5, HP×=2, Damage×=1.25
            spawner->CurrentInterval *= FP._0_75;
            spawner->CurrentBatchSize = (int)(spawner->CurrentBatchSize * FP._1_25);
            spawner->HPMultiplier *= FP._1_20;
            spawner->DamageMultiplier *= FP._1_25;
        }
    }
}
