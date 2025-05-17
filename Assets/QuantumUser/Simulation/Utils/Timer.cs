using Photon.Deterministic;

namespace Quantum
{
    public unsafe partial struct Timer
    {
        public FP NormalizedProgress => TimeLeft / TotalTime;
        public bool IsDone => TimeLeft <= FP._0;

        public void Tick(Frame f)
        {
            TimeLeft -= f.DeltaTime;
        }

        public void Start(FP Duration)
        {
            TotalTime = Duration;
            TimeLeft = TotalTime;
        }
    }
}
