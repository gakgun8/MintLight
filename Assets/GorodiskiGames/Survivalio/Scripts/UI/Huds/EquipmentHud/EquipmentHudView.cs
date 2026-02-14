using System.Collections.Generic;
using Game.Config;
using Game.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Hud
{
    public sealed class EquipmentHudView : BaseHudWithModel<PlayerModel>
    {
        private const float _defaultDragSensitivity = 0.35f;

        [SerializeField] private RawImage _rawImage;
        [SerializeField] private RectTransform _content;
        [SerializeField] private GridLayoutGroup _layoutGroup;
        [SerializeField] private RectTransform _weaponCell;
        [SerializeField] private RectTransform _clothHelmetCell;
        [SerializeField] private RectTransform _clothVestCell;
        [SerializeField] private RectTransform _clothGlovesCell;
        [SerializeField] private RectTransform _clothUniformCell;
        [SerializeField] private RectTransform _clothShoesCell;
        [SerializeField] private AttributeSlotView _attackAttribute;
        [SerializeField] private AttributeSlotView _healthAttribute;
        [SerializeField] private float _dragSensitivity = _defaultDragSensitivity;

        public event System.Action<float> ON_CHARACTER_DRAG;

        private CharacterDragInputView _characterDragInput;

        public RawImage RawImage => _rawImage;
        public RectTransform Content => _content;
        public GridLayoutGroup LayoutGroup => _layoutGroup;
        public RectTransform WeaponCell => _weaponCell;
        public Dictionary<ClothElementType, RectTransform> ClothCellsMap;

        public EquipmentHudView()
        {
            ClothCellsMap = new Dictionary<ClothElementType, RectTransform>();
        }

        protected override void OnEnable()
        {
            ClothCellsMap[ClothElementType.Helmet] = _clothHelmetCell;
            ClothCellsMap[ClothElementType.Vest] = _clothVestCell;
            ClothCellsMap[ClothElementType.Gloves] = _clothGlovesCell;
            ClothCellsMap[ClothElementType.Uniform] = _clothUniformCell;
            ClothCellsMap[ClothElementType.Shoes] = _clothShoesCell;

            _rawImage.raycastTarget = true;

            _characterDragInput = _rawImage.GetComponent<CharacterDragInputView>();
            if (_characterDragInput == null)
                _characterDragInput = _rawImage.gameObject.AddComponent<CharacterDragInputView>();

            _characterDragInput.ON_DRAG = OnCharacterDrag;
        }

        protected override void OnDisable()
        {
            if (_characterDragInput != null)
                _characterDragInput.ON_DRAG = null;

            ClothCellsMap.Clear();
        }

        protected override void OnModelChanged(PlayerModel model)
        {
            _attackAttribute.SetValue(model.GetAttribute(UnitAttributeType.Attack));
            _healthAttribute.SetValue(model.GetAttribute(UnitAttributeType.Health));
        }

        private void OnCharacterDrag(float deltaX)
        {
            ON_CHARACTER_DRAG?.Invoke(-deltaX * _dragSensitivity);
        }
    }

    public sealed class CharacterDragInputView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public System.Action<float> ON_DRAG;

        private bool _isDragging;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || eventData == null)
                return;

            ON_DRAG?.Invoke(eventData.delta.x);
        }
    }
}
