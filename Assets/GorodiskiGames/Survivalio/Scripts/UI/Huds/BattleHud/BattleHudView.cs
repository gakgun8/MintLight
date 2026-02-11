using System;
using Core;
using Game.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Hud
{
    public sealed class BattleHudModel : Observable
    {
        public int Level;
        public string LevelLabel;
        public float DurationMax;

        public BattleHudModel(int level, string levelLabel, float durationMax)
        {
            Level = level;
            LevelLabel = levelLabel;
            DurationMax = durationMax;
        }
    }

    public sealed class BattleHudView : BaseHudWithModel<BattleHudModel>
    {
        private const string _levelLabelFormat = "{0}. {1}";
        private const string _durationMaxFormat = "BEST TIME: {0}";

        [SerializeField] private RawImage _rawImage;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private TMP_Text _levelLabelText;
        [SerializeField] private TMP_Text _durationMaxText;
        [SerializeField] private Button _startButton;
        [SerializeField] private TMP_Text _startButtonText;
        [SerializeField] private TMP_Text _startPriceText;

        public RawImage RawImage => _rawImage;
        public Button SettingsButton => _settingsButton;
        public Button StartButton => _startButton;
        public TMP_Text StartPriceText => _startPriceText;

        protected override void OnEnable()
        {

        }

        protected override void OnDisable()
        {

        }

        protected override void OnModelChanged(BattleHudModel model)
        {
            var levelNice = model.Level + 1; 
            _levelLabelText.text = string.Format(_levelLabelFormat, levelNice, model.LevelLabel);

            var durationMax = TimeSpan.FromSeconds(model.DurationMax).DateToMMSS();
            _durationMaxText.text = string.Format(_durationMaxFormat, durationMax);
        }
    }
}

