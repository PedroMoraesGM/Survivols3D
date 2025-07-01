using System.Collections.Generic;
using Photon.Deterministic;
using UnityEngine;

namespace Quantum
{
    [CreateAssetMenu(fileName = "CharacterClassInfo", menuName = "Scriptable Objects/CharacterClassInfo")]
    public class CharacterClassInfo : AssetObject
    {
        public CharacterClass Class; // (Tank, Assassin, etc.)
        public string DisplayName;
        [TextArea]
        public string Description;
        public Sprite Icon;
        public Color Color = Color.white;

        [Header("Base Stats")]
        public int BaseMaxHealth = 100;
        public FP BaseSpeed = 12;
        public FP BaseDamageMultiplier = 1;

        [Header("Initial Weapon / Abilities")]
        public UpgradeId InitialWeapon; // Weapon ID  
        public List<UpgradePool> UpgradesIdsPool; // List of Upgrade IDs that this class starts with
    }

    [System.Serializable]
    public class UpgradePool
    {
        public UpgradeId UpgradeId;

        // -1 means so it won't replace default values
        public int MinLevel = -1;
        public int Weight = -1;
    }
}
