using System;
using System.Collections.Generic;
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

        [Header("Skeleton")]
        public Transform animatorNode;
        private Transform _skeletonRoot;

        [Header("Part slots")]
        [SerializeField] private Transform _helmetSlot;
        [SerializeField] private Transform _vestSlot;
        [SerializeField] private Transform _uniformSlot;
        [SerializeField] private Transform _glovesSlot;
        [SerializeField] private Transform _shoesSlot;

        private readonly Dictionary<ClothElementType, GameObject> _equippedPartRoots = new Dictionary<ClothElementType, GameObject>();

        private void Awake()
        {
            if (animatorNode == null)
            {
                animatorNode = transform.Find("RotateNode/AnimatorNode");
            }

            if (animatorNode != null)
            {
                _skeletonRoot = animatorNode.Find("Bip001");
            }

            if (_skeletonRoot == null)
            {
                Debug.LogWarning("[PlayerView] skeletonRoot not found at AnimatorNode/Bip001.");
            }
        }

        protected override void OnModelChanged(UnitModel model)
        {
            var playerModel = model as PlayerModel;

            var health = playerModel.GetAttribute(UnitAttributeType.Health);
            _health.fillAmount = (float)health / playerModel.HealthNominal;
            _healthText.text = health.ToString();

            bool hasAllClothMeshes = playerModel.HasAllClothMeshes;
            if (!hasAllClothMeshes)
            {
                DestroyAllClothParts();
                _full.sharedMesh = playerModel.FullSkinnedMesh;
                _head.sharedMesh = null;
                return;
            }

            _full.sharedMesh = null;

            ReplacePart(ClothElementType.Helmet, playerModel);
            ReplacePart(ClothElementType.Vest, playerModel);
            ReplacePart(ClothElementType.Uniform, playerModel);
            ReplacePart(ClothElementType.Gloves, playerModel);
            ReplacePart(ClothElementType.Shoes, playerModel);
        }

        private void ReplacePart(ClothElementType clothType, PlayerModel playerModel)
        {
            if (_equippedPartRoots.TryGetValue(clothType, out var currentPartRoot) && currentPartRoot != null)
            {
                Destroy(currentPartRoot);
                _equippedPartRoots.Remove(clothType);
            }

            if (!playerModel.ClothPrefabMap.TryGetValue(clothType, out var partPrefab) || partPrefab == null)
            {
                return;
            }

            var slot = GetSlot(clothType);
            if (slot == null)
            {
                Debug.LogWarning($"[PlayerView] Missing slot for {clothType}. Using Player root as fallback.");
                slot = transform;
            }

            var instance = Instantiate(partPrefab, slot);
            var instanceTransform = instance.transform;
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.identity;
            instanceTransform.localScale = Vector3.one;

            PartBinder.BindSkinnedMeshes(instance, _skeletonRoot);
            _equippedPartRoots[clothType] = instance;
        }

        private void DestroyAllClothParts()
        {
            foreach (var pair in _equippedPartRoots)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            _equippedPartRoots.Clear();
        }

        private Transform GetSlot(ClothElementType clothType)
        {
            switch (clothType)
            {
                case ClothElementType.Helmet:
                    return _helmetSlot;
                case ClothElementType.Vest:
                    return _vestSlot;
                case ClothElementType.Uniform:
                    return _uniformSlot;
                case ClothElementType.Gloves:
                    return _glovesSlot;
                case ClothElementType.Shoes:
                    return _shoesSlot;
                default:
                    return null;
            }
        }

        public void FireFootOnGround()
        {
            ON_FOOT_ON_GROUND?.Invoke();
        }
    }
}
