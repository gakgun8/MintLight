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

        // ✅ (추가) 공격 히트 이벤트를 컨트롤러로 전달
        public event Action<int> ON_ATTACK_HIT;

        // ✅ (추가) Inspector에서 AutoCombatConfig 연결하기 위한 필드
        [SerializeField] private AutoCombatConfig _autoCombatConfig;
        public AutoCombatConfig AutoCombatConfig => _autoCombatConfig;

        [SerializeField] private Image _health;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private SkinnedMeshRenderer _full;
        [SerializeField] private SkinnedMeshRenderer _head;
        [SerializeField] private SkinnedMeshRenderer _helmet;
        [SerializeField] private SkinnedMeshRenderer _vest;
        [SerializeField] private SkinnedMeshRenderer _uniform;
        [SerializeField] private SkinnedMeshRenderer _gloves;
        [SerializeField] private SkinnedMeshRenderer _shoes;

        protected override void OnModelChanged(UnitModel model)
        {
            var playerModel = model as PlayerModel;
            if (playerModel == null)
                return;

            var health = playerModel.GetAttribute(UnitAttributeType.Health);
            if (_health != null)
                _health.fillAmount = (float)health / playerModel.HealthNominal;

            if (_healthText != null)
                _healthText.text = health.ToString();

            bool hasAllClothMeshes = playerModel.HasAllClothMeshes;
            if (!hasAllClothMeshes)
            {
                SetSharedMesh(_full, playerModel.FullSkinnedMesh, nameof(_full));
                SetSharedMesh(_head, null, nameof(_head));
                return;
            }

            SetSharedMesh(_helmet, playerModel.ClothMeshMap[ClothElementType.Helmet], nameof(_helmet));
            SetSharedMesh(_vest, playerModel.ClothMeshMap[ClothElementType.Vest], nameof(_vest));
            SetSharedMesh(_uniform, playerModel.ClothMeshMap[ClothElementType.Uniform], nameof(_uniform));
            SetSharedMesh(_gloves, playerModel.ClothMeshMap[ClothElementType.Gloves], nameof(_gloves));
            SetSharedMesh(_shoes, playerModel.ClothMeshMap[ClothElementType.Shoes], nameof(_shoes));
        }

        private void SetSharedMesh(SkinnedMeshRenderer renderer, Mesh mesh, string fieldName)
        {
            if (renderer == null)
            {
                Debug.LogWarning($"{nameof(PlayerView)} on '{name}' is missing reference for {fieldName}.", this);
                return;
            }

            renderer.sharedMesh = mesh;
        }

        public void FireFootOnGround()
        {
            ON_FOOT_ON_GROUND?.Invoke();
        }

        // ✅ (추가) 애니메이션 이벤트에서 호출할 함수들
        // attack_01 클립 이벤트에서 FireAttack01Hit 호출
        public void FireAttack01Hit() => ON_ATTACK_HIT?.Invoke(0);

        // attack_02 클립 이벤트에서 FireAttack02Hit 호출
        public void FireAttack02Hit() => ON_ATTACK_HIT?.Invoke(1);

        // attack_03 클립 이벤트에서 FireAttack03Hit 호출
        public void FireAttack03Hit() => ON_ATTACK_HIT?.Invoke(2);
    }
}
