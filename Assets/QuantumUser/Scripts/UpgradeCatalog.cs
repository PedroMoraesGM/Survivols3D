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
        [TextArea]
        public string Description;
        public string Label;
        public Sprite Icon;
        public Color IconColor;
    }

    public List<Entry> Entries;

    // Helper lookup (build at Awake or via Editor script)
    Dictionary<int, Entry> _map;
    public void OnEnable()
    {
        _map = new Dictionary<int, Entry>();
        foreach (var e in Entries) _map[e.Key] = e;
    }

    public Entry Get(int key)
    {
        return _map.TryGetValue(key, out var e) ? e : null;
    }
}
