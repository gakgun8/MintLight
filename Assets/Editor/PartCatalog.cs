using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project/Parts/Part Catalog", fileName = "PartCatalog")]
public class PartCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;            // 고유키(기본: prefab name)
        public string partType;       // Head/Body/Legs/Weapon/Back/Misc ...
        public GameObject prefab;     // 생성된 프리팹
        public Sprite icon;           // 캡처된 아이콘
        public UnityEngine.Object fbx; // 원본 FBX(Model) 참조(옵션)
    }

    public List<Entry> entries = new List<Entry>();

    public Entry GetOrCreate(string key)
    {
        var e = entries.Find(x => x.key == key);
        if (e == null)
        {
            e = new Entry { key = key };
            entries.Add(e);
        }
        return e;
    }
}
