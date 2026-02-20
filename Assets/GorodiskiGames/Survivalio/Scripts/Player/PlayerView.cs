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
        public event Action ON_ATTACK_HIT;
        public event Action ON_ATTACK_DASH;

        // ✅ 디버그/툴에서 접근할 모델 캐시
        private PlayerModel _debugModel;
        public PlayerModel DebugModel => _debugModel;

        // (선택) 디버그툴에서 편하게 부르라고 별칭 제공
        public PlayerModel GetPlayerModel() => _debugModel;

        [SerializeField] private Image _health;
        [SerializeField] private TMP_Text _healthText;

        [SerializeField] private SkinnedMeshRenderer _full;
        [SerializeField] private SkinnedMeshRenderer _head;
        [SerializeField] private SkinnedMeshRenderer _helmet;
        [SerializeField] private SkinnedMeshRenderer _vest;
        [SerializeField] private SkinnedMeshRenderer _uniform;
        [SerializeField] private SkinnedMeshRenderer _gloves;
        [SerializeField] private SkinnedMeshRenderer _shoes;

        [SerializeField] private Transform animatorNode;   // Player/RotateNode/AnimatorNode
        [SerializeField] private Transform partsRoot;      // AnimatorNode/PartsRoot

        private Transform skeletonRoot;
        private GameObject _helmetObj, _vestObj, _uniformObj, _glovesObj, _shoesObj;
        private GameObject _helmetPrefab, _vestPrefab, _uniformPrefab, _glovesPrefab, _shoesPrefab;

        protected override void Awake()
        {
            base.Awake();
            skeletonRoot = animatorNode != null ? animatorNode.Find("Bip001") : null;
        }

        private void ReplacePartIfNeeded(ref GameObject slotObj, ref GameObject cachedPrefab, string slotName, GameObject prefab)
        {
            if (cachedPrefab == prefab)
                return;

            cachedPrefab = prefab;
            ReplacePart(ref slotObj, slotName, prefab);
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

            // skeletonRoot가 없으면 바인딩 불가
            if (skeletonRoot != null)
                PartBinder.BindSkinnedMeshes(go, skeletonRoot, "Bip001 Pelvis");

            slotObj = go;
        }

        protected override void OnModelChanged(UnitModel model)
        {
            // ✅ 캐시부터
            _debugModel = model as PlayerModel;

            var playerModel = _debugModel;
            if (playerModel == null)
                return;

            var health = playerModel.GetAttribute(UnitAttributeType.Health);
            if (_health != null)
                _health.fillAmount = (float)health / playerModel.HealthNominal;
            if (_healthText != null)
                _healthText.text = health.ToString();

            if (playerModel.ClothPrefabMap != null)
            {
                var helmetPrefab = playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Helmet, out var helmetPrefabValue) ? helmetPrefabValue : null;
                ReplacePartIfNeeded(ref _helmetObj, ref _helmetPrefab, "Helmet", helmetPrefab);

                var vestPrefab = playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Vest, out var vestPrefabValue) ? vestPrefabValue : null;
                ReplacePartIfNeeded(ref _vestObj, ref _vestPrefab, "Vest", vestPrefab);

                var uniformPrefab = playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Uniform, out var uniformPrefabValue) ? uniformPrefabValue : null;
                ReplacePartIfNeeded(ref _uniformObj, ref _uniformPrefab, "Uniform", uniformPrefab);

                var glovesPrefab = playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Gloves, out var glovesPrefabValue) ? glovesPrefabValue : null;
                ReplacePartIfNeeded(ref _glovesObj, ref _glovesPrefab, "Gloves", glovesPrefab);

                var shoesPrefab = playerModel.ClothPrefabMap.TryGetValue(ClothElementType.Shoes, out var shoesPrefabValue) ? shoesPrefabValue : null;
                ReplacePartIfNeeded(ref _shoesObj, ref _shoesPrefab, "Shoes", shoesPrefab);
            }
        }

        public void FireFootOnGround()
        {
            ON_FOOT_ON_GROUND?.Invoke();
        }

        public void FireAttackHit()
        {
            ON_ATTACK_HIT?.Invoke();
        }

        public void FireAttackDash()
        {
            ON_ATTACK_DASH?.Invoke();
        }
    }
}
