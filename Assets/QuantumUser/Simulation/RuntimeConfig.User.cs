using System;
using System.Collections.Generic;
using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    public partial class RuntimeConfig
    {
        public AssetRef<EntityPrototype> PlayerPrototype;
        public FPVector2 GameSize;
        // indexed by CharacterClass
        public Color[] ClassColors;       
        public int PlayersCount;
        public int CountdownTime;
        public int GameTime;
        public int FinishedTime;
    }
}
