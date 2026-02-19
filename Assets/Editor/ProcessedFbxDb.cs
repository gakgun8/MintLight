using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project/Parts/Processed FBX DB", fileName = "ProcessedFbxDb")]
public class ProcessedFbxDb : ScriptableObject
{
    [Serializable]
    public class Item
    {
        public string guid;            // FBX GUID
        public string assetPath;       // FBX 경로(디버깅용)
        public string hash;            // 변경 감지용 시그니처
        public long lastWriteUtcTicks; // 파일 수정시간(백업)
    }

    public List<Item> items = new List<Item>();

    public bool TryGet(string guid, out Item item)
    {
        item = items.Find(x => x.guid == guid);
        return item != null;
    }

    public void Upsert(string guid, string assetPath, string hash, long ticks)
    {
        var it = items.Find(x => x.guid == guid);
        if (it == null)
        {
            it = new Item { guid = guid };
            items.Add(it);
        }

        it.assetPath = assetPath;
        it.hash = hash;
        it.lastWriteUtcTicks = ticks;
    }
}
