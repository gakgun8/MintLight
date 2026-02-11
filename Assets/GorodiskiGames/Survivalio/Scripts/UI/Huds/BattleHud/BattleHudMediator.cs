using Game.Config;
using Game.Managers;
using Game.States;
using Injection;
using UnityEngine;

namespace Game.UI.Hud
{
    public sealed class BattleHudMediator : RawCameraHudMediator<BattleHudView>
    {
        private const string _startLevelPriceFormat = "{0}{1}";
        private const int _startLevelPrice = 5;
        private const float _anchorPositionY = 0.4f;

        [Inject] private HudManager _hudManager;
        [Inject] private GameStateManager _gameStateManager;
        [Inject] private MenuManager _menuManager;
        [Inject] private GameConfig _config;

        private GameObject _levelPreview;

        protected override void Show()
        {
            SetViewFromCamera(_view.RawImage, out _rawCamera);

            _view.StartPriceText.text = string.Format(_startLevelPriceFormat, GameConstants.EnergyIcon, _startLevelPrice);

            var level = _menuManager.Model.Level;
            var levelConfig = _config.LevelConfigs[level];
            var levelLabel = levelConfig.Label;
            var levelDurationMax = _menuManager.Model.LevelDurationMax;

            var prefabPreview = levelConfig.Preview;
            _levelPreview = GameObject.Instantiate(prefabPreview);
            _levelPreview.transform.position = _rawCamera.AnchorToWorldPosition(0.5f, _anchorPositionY);
            _levelPreview.gameObject.SetActive(false);

            var model = new BattleHudModel(level, levelLabel, levelDurationMax);
            _view.Model = model;

            _view.SettingsButton.onClick.AddListener(OnSettingsButtonClick);
            _view.StartButton.onClick.AddListener(OnStartButtonClick);
        }

        protected override void Hide()
        {
            _view.SettingsButton.onClick.RemoveListener(OnSettingsButtonClick);
            _view.StartButton.onClick.RemoveListener(OnStartButtonClick);

            Object.Destroy(_levelPreview);
            Object.Destroy(_rawCamera.gameObject);
        }

        private void OnSettingsButtonClick()
        {
            _hudManager.ShowSingle<SettingsHudMediator>();
        }

        private void OnStartButtonClick()
        {
            if(_menuManager.Model.Energy < _startLevelPrice)
                return;

            var energy = _menuManager.Model.Energy;
            energy -= _startLevelPrice;

            _menuManager.Model.Energy = energy;
            _menuManager.Model.Save();
            _menuManager.Model.SetChanged();

            _gameStateManager.SwitchToState(new GamePlayState());
        }
    }
}

