using System;
using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnityEngine;

/// The maps offered on the map select screen, in display order. Kept separate from the
/// HexMapDefinition assets so test/UI maps in Data/Maps don't show up in the menu.
[CreateAssetMenu(menuName = "Unholy Trinity/Map Catalog", fileName = "MapCatalog")]
public class MapCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string displayName;
        public Sprite thumbnail;
        public HexMapDefinition definition;

        public string Id => definition != null ? definition.name : displayName;
    }

    [SerializeField] List<Entry> maps = new List<Entry>();

    public IReadOnlyList<Entry> Maps => maps;
}
