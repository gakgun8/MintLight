using UnityEngine;

[ExecuteAlways]
public class SMRDebug : MonoBehaviour
{
    private void OnEnable()
    {
        var smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr == null)
        {
            Debug.LogError($"[{name}] SkinnedMeshRenderer NOT FOUND in children. Attach this script to the mesh object (the one that has SkinnedMeshRenderer).", this);
            return;
        }

        var bones = smr.bones;
        string rootName = smr.rootBone != null ? smr.rootBone.name : "null";
        string meshName = smr.sharedMesh != null ? smr.sharedMesh.name : "null";
        int boneCount = bones != null ? bones.Length : -1;

        Debug.Log($"[{name}] SMR FOUND: rootBone={rootName} bones={boneCount} mesh={meshName}", this);

        // first bone path (optional)
        if (bones != null && bones.Length > 0 && bones[0] != null)
        {
            Debug.Log($"[{name}] firstBone={bones[0].name} path={GetPath(bones[0])}", this);
        }

        // Log once
        enabled = false;
    }

    private static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            s = t.name + "/" + s;
        }
        return s;
    }
}
