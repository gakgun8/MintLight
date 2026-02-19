using System.Collections.Generic;
using Game.Cloth;
using Game.Config;
using Game.Equipment;
using Game.Inventory;
using Game.Managers;
using Game.Player;
using Game.Weapon;
using Injection;
using UnityEngine;
using Utilities;

namespace Game.UI.Hud
{
    public sealed class EquipmentHudMediator : RawCameraHudMediator<EquipmentHudView>
    {
        private const float _rotationY = 205f;
        private const float _anchorPositionY = 0.55f;

        [Inject] private GameConfig _config;
        [Inject] private MenuManager _menuManager;
        [Inject] private HudManager _hudManager;

        private PlayerController _player;

        private readonly Dictionary<EquipmentModel, InventorySlotView> _slotsMap;
        private bool _isApplyingEquipChange;

        public EquipmentHudMediator()
        {
            _slotsMap = new Dictionary<EquipmentModel, InventorySlotView>();
        }

        protected override void Show()
        {
            SetViewFromCamera(_view.RawImage, out _rawCamera);

            _player = _menuManager.Player;
            _player.View.Position = _rawCamera.AnchorToWorldPosition(0.5f, _anchorPositionY);
            _player.View.Rotation = Quaternion.Euler(0f, _rotationY, 0f);
            _player.View.InitializeAnimationBinding(_player.Model.GetCurrentClothAnimationController(), true);
            _player.IdleMenu();

            RebuildSlots();

            _view.Model = _player.Model;

            SetContentSize();

            _view.ON_MODEL_CHANGED += OnPlayerModelChanged;
            _menuManager.ON_EQUIP += OnEquip;
            _view.ON_CHARACTER_DRAG += OnCharacterDrag;
        }

        private void RebuildSlots()
        {
            foreach (var slot in _slotsMap.Values)
            {
                slot.ON_CLICK -= OnEquipmentSlotClick;
                GameObject.Destroy(slot.gameObject);
            }
            _slotsMap.Clear();

            var prefab = _resourcesManager.LoadInventorySlot();
            foreach (var serial in _player.Model.StoredWeapons.Keys)
            {
                var category = InventoryCategory.Weapon;
                var index = _player.Model.StoredWeapons[serial];
                var config = _config.WeaponMap[index];
                var level = _player.Model.WeaponLevels[serial];

                var model = _menuManager.CreateInventoryModel(category, config, index, serial, level);
                var weaponModel = model as WeaponModel;
                var slot = GameObject.Instantiate(prefab).GetComponent<InventorySlotView>();

                var parent = _view.Content;
                if (weaponModel.IsEquipped)
                    parent = _view.WeaponCell;
                slot.SetParent(parent);

                _slotsMap[weaponModel] = slot;

                slot.Model = model;
                slot.ON_CLICK += OnEquipmentSlotClick;
            }

            foreach (var serial in _player.Model.StoredCloth.Keys)
            {
                var category = InventoryCategory.Cloth;
                var index = _player.Model.StoredCloth[serial];
                var config = _config.ClothMap[index];
                var level = _player.Model.ClothLevels[serial];

                var model = _menuManager.CreateInventoryModel(category, config, index, serial, level);
                var clothModel = model as ClothModel;
                var slot = GameObject.Instantiate(prefab).GetComponent<InventorySlotView>();

                var parent = _view.Content;
                if(clothModel.IsEquipped)
                    parent = _view.ClothCellsMap[clothModel.ClothType];

                slot.SetParent(parent);

                _slotsMap[clothModel] = slot;

                slot.Model = model;
                slot.ON_CLICK += OnEquipmentSlotClick;
            }
        }

        protected override void Hide()
        {
            _view.ON_MODEL_CHANGED -= OnPlayerModelChanged;
            _menuManager.ON_EQUIP -= OnEquip;
            _view.ON_CHARACTER_DRAG -= OnCharacterDrag;

            foreach (var slot in _slotsMap.Values)
            {
                slot.ON_CLICK -= OnEquipmentSlotClick;
                GameObject.Destroy(slot.gameObject);
            }
            _slotsMap.Clear();

            _player.View.SetMenuPreviewMode(false);

            Object.Destroy(_rawCamera.gameObject);
        }

        private void OnPlayerModelChanged(PlayerModel model)
        {
            if (_isApplyingEquipChange)
                return;

            RebuildSlots();
            SetContentSize();
        }


        private void OnCharacterDrag(float rotationDeltaY)
        {
            var rotation = _player.View.Rotation.eulerAngles;
            rotation.y += rotationDeltaY;
            _player.View.Rotation = Quaternion.Euler(0f, rotation.y, 0f);
        }

        private void OnEquip(EquipmentModel candidateModel)
        {
            _isApplyingEquipChange = true;
            try
            {
                var category = candidateModel.Category;
                foreach (var model in _slotsMap.Keys)
                {
                    if (model.Category != category)
                        continue;

                    if (!model.IsEquipped)
                        continue;

                    if(category == InventoryCategory.Weapon)
                    {
                        Unequip(model);
                        break;
                    }
                    else
                    {
                        var clothModel = model as ClothModel;
                        var candidateClothModel = candidateModel as ClothModel;
                        if(clothModel.ClothType != candidateClothModel.ClothType)
                            continue;

                        Unequip(model);
                    }
                }

                Equip(candidateModel);
            }
            finally
            {
                _isApplyingEquipChange = false;
                RebuildSlots();
                SetContentSize();
            }
        }

        private void Equip(EquipmentModel model)
        {
            model.IsEquipped = true;

            var slot = _slotsMap[model];
            var category = model.Category;
            var serial = model.Serial;

            RectTransform parent = null;
            if (category == InventoryCategory.Weapon)
            {
                var weaponModel = model as WeaponModel;

                var type = UnitAttributeType.Attack;
                var value = _player.Model.GetAttribute(type);
                value += weaponModel.Attack;

                _player.Model.SetAttribute(type, value);
                _player.Model.EquippedWeapon = serial;
                _player.Model.Save();
                _player.Model.SetChanged();

                parent = _view.WeaponCell;
            }
            else if(category == InventoryCategory.Cloth)
            {
                var clothModel = model as ClothModel;

                _player.Model.ClothMeshMap[clothModel.ClothType] = clothModel.Mesh;
                _player.Model.ClothPrefabMap[clothModel.ClothType] = clothModel.Prefab;
                _player.Model.SetClothAnimationController(clothModel.ClothType, clothModel.AnimationOverride);

                var type = UnitAttributeType.Health;
                var value = _player.Model.GetAttribute(type);
                value += clothModel.Armor;

                _player.Model.SetAttribute(type, value);
                _player.Model.UpdateNominalHealth(value);
                _player.Model.EquippedCloth.Add(serial);
                _player.Model.Save();
                _player.Model.SetChanged();

                var clothType = clothModel.ClothType;
                parent = _view.ClothCellsMap[clothType];

                _menuManager.Player.View.ApplyAnimationOverride(_player.Model.GetCurrentClothAnimationController());
                _menuManager.Player.ChangeCloth();
            }

            slot.SetParent(parent);
            SetContentSize();
        }

        private void Unequip(EquipmentModel model)
        {
            var category = model.Category;
            if (category == InventoryCategory.Weapon)
            {
                var weaponModel = model as WeaponModel;

                var type = UnitAttributeType.Attack;
                var value = _player.Model.GetAttribute(type);
                value -= weaponModel.Attack;
                _player.Model.SetAttribute(type, value);

                _player.Model.EquippedWeapon = -1;
                _player.Model.Save();
                _player.Model.SetChanged();
            }
            else if(category == InventoryCategory.Cloth)
            {
                var clothModel = model as ClothModel;

                var type = UnitAttributeType.Health;
                var value = _player.Model.GetAttribute(type);
                value -= clothModel.Armor;

                _player.Model.SetAttribute(type, value);
                _player.Model.UpdateNominalHealth(value);

                var serial = model.Serial;
                _player.Model.EquippedCloth.Remove(serial);
                _player.Model.ClothPrefabMap[clothModel.ClothType] = null;
                _player.Model.RemoveClothAnimationController(clothModel.ClothType);
                _player.Model.Save();
                _player.Model.SetChanged();
            }

            model.IsEquipped = false;

            if (category == InventoryCategory.Cloth)
                _player.View.ApplyAnimationOverride(_player.Model.GetCurrentClothAnimationController());

            var slot = _slotsMap[model];
            slot.SetParent(_view.Content);
            SetContentSize();
        }

        private void OnEquipmentSlotClick(InventoryModel model)
        {
            var category = model.Category;
            if(category == InventoryCategory.Resource)
                return;

            var equipmentModel = model as EquipmentModel;
            _hudManager.ShowSingle<EquipmentPreviewHudMediator>(equipmentModel);
        }

        private void SetContentSize()
        {
            var sizeResult = UIUtil.GetContentSize(_view.Content, _view.LayoutGroup);
            _view.Content.sizeDelta = sizeResult;
        }
    }
}
