using Game.Config;
using Game.Domain;
using Game.Managers;
using Game.Modules;
using Injection;
using UnityEngine;

namespace Game.States
{
    public class GameCommonModulesState : GameModulesState
    {
        [Inject] private Context _context;
        [Inject] private GameConfig _config;
        [Inject] private GameStateManager _gameStateManager;

        private AudioManager _audioManager;

        public override void Initialize()
        {
            var model = GameModel.Load(_config);
            _audioManager = new AudioManager(model.MusicVolume, model.SFXVolume);

            _context.Install(_audioManager);
            _context.ApplyInstall();

            InitLevelModules();

            var key = GameConstants.LoginToTheGameKey;
            var loginToTheGame = PlayerPrefs.GetInt(key, 0);
            if (loginToTheGame == 0)
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();

                _gameStateManager.SwitchToState(new GamePlayState());
            }
            else
            {
                _gameStateManager.SwitchToState(new GameMenuState());
            }
        }

        public override void Dispose()
        {

        }

        private void InitLevelModules()
        {
            AddModule<AudioModule, AudioModuleView>(_gameView);
        }
    }
}

