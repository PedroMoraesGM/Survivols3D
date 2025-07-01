using UnityEngine;

namespace Quantum
{
    using System.Collections.Generic;
    using Photon.Deterministic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "CharacterClassCatalog", menuName = "Scriptable Objects/CharacterClassCatalog")]
    public class CharacterClassCatalog : AssetObject
    {
        public List<CharacterClassInfo> Classes;
        public List<UpgradeEntryData> AllUpgradeEntries;
    }

    [System.Serializable]
    public class WeaponUpgradeEffectData {
        public WeaponUpgradeType Type;
        public FP Value;
    }

    [System.Serializable]
    public class WeaponUpgradeEffectsData {
        public List<WeaponUpgradeEffectData> Effects;
    }

    [System.Serializable]
    public class UpgradeEntryData {
        public UnityEngine.Object Prefab;
        public UpgradeId Id;
        public int MinLevel;
        public int Weight;
        public bool CanBeRepeated;
        public List<WeaponUpgradeEffectsData> EffectsPerExtraUpgrade;
    }
}
