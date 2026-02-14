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

        private Transform _skeletonRoot;
        private GameObject _helmetObj, _vestObj, _uniformObj, _glovesObj, _shoesObj;

        private void Awake()
        {
            _skeletonRoot = animatorNode != null ? animatorNode.Find("Bip001") : null;
        }

        private bool CanUsePrefabLoading(PlayerModel playerModel)
        {
            return playerModel != null
                   && playerModel.HasAllClothPrefabs
                   && playerModel.ClothPrefabMap != null
                   && partsRoot != null
                   && _skeletonRoot != null;
        }

        private void SetMeshFallback(PlayerModel playerModel)
        {
            bool hasAllClothMeshes = playerModel.HasAllClothMeshes;
            if (!hasAllClothMeshes)
            {
                _full.sharedMesh = playerModel.FullSkinnedMesh;
                _head.sharedMesh = null;
                return;
            }

            _full.sharedMesh = null;

            if (playerModel.ClothMeshMap.TryGetValue(ClothElementType.Helmet, out var helmetMesh))
                _helmet.sharedMesh = helmetMesh;
            if (playerModel.ClothMeshMap.TryGetValue(ClothElementType.Vest, out var vestMesh))
                _vest.sharedMesh = vestMesh;
            if (playerModel.ClothMeshMap.TryGetValue(ClothElementType.Uniform, out var uniformMesh))
                _uniform.sharedMesh = uniformMesh;
            if (playerModel.ClothMeshMap.TryGetValue(ClothElementType.Gloves, out var glovesMesh))
                _gloves.sharedMesh = glovesMesh;
            if (playerModel.ClothMeshMap.TryGetValue(ClothElementType.Shoes, out var shoesMesh))
                _shoes.sharedMesh = shoesMesh;
        }

        private void ReplacePart(ref GameObject slotObj, string slotName, GameObject prefab)
        {
            if (slotObj != null)
                Destroy(slotObj);
            if (prefab == null)
                return;

            GameObject go = Instantiate(prefab, partsRoot);
            go.name = slotName;

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            PartBinder.BindSkinnedMeshes(go, _skeletonRoot, "Bip001 Pelvis");
            slotObj = go;
        }

        private void SetPrefabParts(PlayerModel playerModel)
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

        protected override void OnModelChanged(UnitModel model)
        {
            var playerModel = model as PlayerModel;
            if (playerModel == null)
                return;

            var health = playerModel.GetAttribute(UnitAttributeType.Health);
            _health.fillAmount = (float)health / playerModel.HealthNominal;
            _healthText.text = health.ToString();

            if (CanUsePrefabLoading(playerModel))
            {
                SetPrefabParts(playerModel);
            }
            else
            {
                SetMeshFallback(playerModel);
            }
        }

        public void FireFootOnGround()
        {
            ON_FOOT_ON_GROUND?.Invoke();
        }
    }
}
