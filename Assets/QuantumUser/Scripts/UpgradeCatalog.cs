using System.Collections.Generic;
using System;
using UnityEngine;
using Quantum;

[CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "Scriptable Objects/UpgradeCatalog")]
public class UpgradeCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public UpgradeId Key;          // must match UpgradeEntry.MetaKey
        public string DisplayName;
        
        public string Description;
        [TextArea]
        public string[] DescriptionPerUpgrade; // list of effects, e.g. "Damage +10%", "Speed +5%" 
        public string Label;
        public Sprite Icon;
        public Color IconColor;
    }

    public List<Entry> Entries;

    Dictionary<UpgradeId, Entry> _map;

    public void InitializeMap()
    {
        _map = new Dictionary<UpgradeId, Entry>();
        foreach (var e in Entries) _map[e.Key] = e;
    }

    public Entry Get(UpgradeId key)
    {
        if (_map == null)
            InitializeMap();

        return _map.TryGetValue(key, out var e) ? e : null;
    }
}
