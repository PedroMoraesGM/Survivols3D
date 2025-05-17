using System;
using System.Collections.Generic;
using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    [Serializable]
    public class GameSettings : AssetObject
    {
        public AssetRef<EntityPrototype> characterPrototype;

        [Space]

        public FP WeaponStackLimit = 1;

    }
}