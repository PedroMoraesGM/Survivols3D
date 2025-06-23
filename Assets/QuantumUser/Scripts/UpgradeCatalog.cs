using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "Scriptable Objects/UpgradeCatalog")]
public class UpgradeCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public int Key;          // must match UpgradeEntry.MetaKey
        public string DisplayName;
        
        public string Description;
        [TextArea]
        public string[] DescriptionPerUpgrade; // list of effects, e.g. "Damage +10%", "Speed +5%" 
        public string Label;
        public Sprite Icon;
        public Color IconColor;
    }

    public List<Entry> Entries;

    Dictionary<int, Entry> _map;

    public void InitializeMap()
    {
        _map = new Dictionary<int, Entry>();
        foreach (var e in Entries) _map[e.Key] = e;
    }

    public Entry Get(int key)
    {
        if (_map == null)
            InitializeMap();

        return _map.TryGetValue(key, out var e) ? e : null;
    }
}
