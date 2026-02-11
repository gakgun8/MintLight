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

        protected override void OnModelChanged(UnitModel model)
        {
            var playerModel = model as PlayerModel;

            var health = playerModel.GetAttribute(UnitAttributeType.Health);
            _health.fillAmount = (float)health / playerModel.HealthNominal;
            _healthText.text = health.ToString();

            bool hasAllClothMeshes = playerModel.HasAllClothMeshes;
            if(!hasAllClothMeshes)
            {
                _full.sharedMesh = playerModel.FullSkinnedMesh;
                _head.sharedMesh = null;
                return;
            }

            _helmet.sharedMesh = playerModel.ClothMeshMap[ClothElementType.Helmet];
            _vest.sharedMesh = playerModel.ClothMeshMap[ClothElementType.Vest];
            _uniform.sharedMesh = playerModel.ClothMeshMap[ClothElementType.Uniform];
            _gloves.sharedMesh = playerModel.ClothMeshMap[ClothElementType.Gloves];
            _shoes.sharedMesh = playerModel.ClothMeshMap[ClothElementType.Shoes];
        }

        public void FireFootOnGround()
        {
            ON_FOOT_ON_GROUND?.Invoke();
        }
    }
}

