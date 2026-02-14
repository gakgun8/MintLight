using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    public static class PartBinder
    {
        public static void BindSkinnedMeshes(GameObject partRoot, Transform skeletonRoot, string preferRootBoneName = "Bip001 Pelvis")
        {
            if (partRoot == null || skeletonRoot == null)
            {
                Debug.LogWarning("[PartBinder] BindSkinnedMeshes skipped: missing partRoot or skeletonRoot.");
                return;
            }

            var skeletonBonesByName = new Dictionary<string, Transform>();
            var skeletonBones = skeletonRoot.GetComponentsInChildren<Transform>(true);
            foreach (var bone in skeletonBones)
            {
                skeletonBonesByName[bone.name] = bone;
            }

            var meshRenderers = partRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in meshRenderers)
            {
                var reboundBones = new Transform[smr.bones.Length];
                for (var i = 0; i < smr.bones.Length; i++)
                {
                    var sourceBone = smr.bones[i];
                    if (sourceBone == null)
                        continue;

                    if (skeletonBonesByName.TryGetValue(sourceBone.name, out var targetBone))
                    {
                        reboundBones[i] = targetBone;
                    }
                }

                smr.bones = reboundBones;

                Transform rootBone = null;
                if (!string.IsNullOrEmpty(preferRootBoneName))
                {
                    skeletonBonesByName.TryGetValue(preferRootBoneName, out rootBone);
                }

                if (rootBone == null && smr.rootBone != null)
                {
                    skeletonBonesByName.TryGetValue(smr.rootBone.name, out rootBone);
                }

                if (rootBone == null)
                {
                    for (var i = 0; i < reboundBones.Length; i++)
                    {
                        if (reboundBones[i] == null)
                            continue;

                        rootBone = reboundBones[i];
                        break;
                    }
                }

                if (rootBone != null)
                {
                    smr.rootBone = rootBone;
                }

                if (smr.bones.Length > 0 && smr.bones[0] != null)
                {
                    Debug.Log($"[PartBinder] Rebound '{smr.name}' first bone: {GetPath(smr.bones[0])}");
                }
            }
        }

        private static string GetPath(Transform target)
        {
            var path = target.name;
            var cursor = target.parent;
            while (cursor != null)
            {
                path = $"{cursor.name}/{path}";
                cursor = cursor.parent;
            }

            return path;
        }
    }
}
