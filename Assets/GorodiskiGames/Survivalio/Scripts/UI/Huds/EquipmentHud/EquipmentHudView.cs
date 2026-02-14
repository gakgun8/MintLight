using System.Collections.Generic;
using Game.Config;
using Game.Player;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

        private EventTrigger _dragEventTrigger;
        private bool _isCharacterDragging;

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

            _dragEventTrigger = _rawImage.GetComponent<EventTrigger>();
            if (_dragEventTrigger == null)
                _dragEventTrigger = _rawImage.gameObject.AddComponent<EventTrigger>();

            RegisterDragEvents();
        }

        protected override void OnDisable()
        {
            if (_dragEventTrigger != null)
                _dragEventTrigger.triggers.Clear();

            ClothCellsMap.Clear();
            _isCharacterDragging = false;
        }

        protected override void OnModelChanged(PlayerModel model)
        {
            _attackAttribute.SetValue(model.GetAttribute(UnitAttributeType.Attack));
            _healthAttribute.SetValue(model.GetAttribute(UnitAttributeType.Health));
        }

        private void RegisterDragEvents()
        {
            _dragEventTrigger.triggers.Clear();

            AddDragEvent(EventTriggerType.PointerDown, data =>
            {
                var pointerEventData = data as PointerEventData;
                _isCharacterDragging = IsPointerOnCharacter(pointerEventData);
            });

            AddDragEvent(EventTriggerType.PointerUp, _ => _isCharacterDragging = false);

            AddDragEvent(EventTriggerType.Drag, data =>
            {
                if (!_isCharacterDragging)
                    return;

                var pointerEventData = data as PointerEventData;
                if (pointerEventData == null)
                    return;

                ON_CHARACTER_DRAG?.Invoke(-pointerEventData.delta.x * _dragSensitivity);
            });
        }

        private void AddDragEvent(EventTriggerType type, System.Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => callback(data));
            _dragEventTrigger.triggers.Add(entry);
        }

        private bool IsPointerOnCharacter(PointerEventData eventData)
        {
            if (eventData == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(_rawImage.rectTransform, eventData.position, eventData.pressEventCamera);
        }
    }
}
