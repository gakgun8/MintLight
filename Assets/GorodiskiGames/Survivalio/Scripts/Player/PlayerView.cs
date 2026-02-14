using System;
using Game.Config;
using Game.Unit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    public sealed class PlayerView : UnitView
    {
        public event Action ON_FOOT_ON_GROUND;

        [SerializeField] private Image _health;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private SkinnedMeshRenderer _full;
        [SerializeField] private SkinnedMeshRenderer _head;
        [SerializeField] private SkinnedMeshRenderer _helmet;
        [SerializeField] private SkinnedMeshRenderer _vest;
        [SerializeField] private SkinnedMeshRenderer _uniform;
        [SerializeField] private SkinnedMeshRenderer _gloves;
        [SerializeField] private SkinnedMeshRenderer _shoes;
        [SerializeField] private Transform animatorNode; // Player/RotateNode/AnimatorNode
        [SerializeField] private Transform partsRoot;    // AnimatorNode 아래 PartsRoot
        private Transform skeletonRoot;

        private GameObject _helmetObj, _vestObj, _uniformObj, _glovesObj, _shoesObj;
        void Awake()
        {
            skeletonRoot = animatorNode.Find("Bip001"); // ★ 이 줄은 여기!
            Debug.Log($"[PlayerView] animatorNode={animatorNode?.name} partsRoot={partsRoot?.name}");
            Debug.Log($"[PlayerView] skeletonRoot={(skeletonRoot ? GetPath(skeletonRoot) : "NULL")}");

        }

        private static string GetPath(Transform t)
        {
            string s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        private void ReplacePart(ref GameObject slotObj, string slotName, GameObject prefab)
        {
            Debug.Log($"[ReplacePart] slot={slotName} prefab={(prefab ? prefab.name : "NULL")}");

            if (slotObj != null) Destroy(slotObj);
            if (prefab == null) return;

            // ✅ go는 여기서 딱 1번만 선언
            GameObject go = Instantiate(prefab, partsRoot);
            go.name = slotName;

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Debug.Log($"[ReplacePart] smr={(smr ? "YES" : "NO")} rootBefore={(smr && smr.rootBone ? smr.rootBone.name : "null")} bonesBefore={(smr ? smr.bones?.Length : -1)}");

            PartBinder.BindSkinnedMeshes(go, skeletonRoot, "Bip001 Pelvis");

            smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Debug.Log($"[ReplacePart] rootAfter={(smr && smr.rootBone ? smr.rootBone.name : "null")} bonesAfter={(smr ? smr.bones?.Length : -1)}");

            slotObj = go;
        }


        protected override void OnModelChanged(UnitModel model)
        {
            Debug.Log("[PlayerView] OnModelChanged called");

            var playerModel = model as PlayerModel;

            var health = playerModel.GetAttribute(UnitAttributeType.Health);
            _health.fillAmount = (float)health / playerModel.HealthNominal;
            _healthText.text = health.ToString();

           
           
                Debug.Log($"[PlayerView] HasAllClothPrefabs={playerModel.HasAllClothPrefabs}");
            Debug.Log($"[PlayerView] ClothPrefabMap null? {playerModel.ClothPrefabMap == null}");

            if (playerModel.ClothPrefabMap != null)
            {
                if (playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Helmet, out var helmetPrefab))
                    ReplacePart(ref _helmetObj, "Helmet", helmetPrefab);

                if (playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Vest, out var vestPrefab))
                    ReplacePart(ref _vestObj, "Vest", vestPrefab);

                if (playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Uniform, out var uniformPrefab))
                    ReplacePart(ref _uniformObj, "Uniform", uniformPrefab);

                if (playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Gloves, out var glovesPrefab))
                    ReplacePart(ref _glovesObj, "Gloves", glovesPrefab);

                if (playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Shoes, out var shoesPrefab))
                    ReplacePart(ref _shoesObj, "Shoes", shoesPrefab);
            }


        }

        public void FireFootOnGround()
        {
            ON_FOOT_ON_GROUND?.Invoke();
        }
    }
}

