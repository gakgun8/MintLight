using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Advertisements;

namespace Game.Managers
{
    public sealed class UnityAdsProxy : BaseAdsProxy, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        private const int _reloadDuration = 1;

        private string _bannerID;
        private string _interstitialID;
        private string _rewardedID;

        private bool _isDebugBuild;
        private string _gameID;

        private int _bannerRetryAttempt;
        private int _interstitialRetryAttempt;
        private int _rewardedRetryAttempt;

        private readonly Dictionary<string, float> _bannerReloadTimeMap;
        private readonly Dictionary<string, float> _interstitialReloadTimeMap;
        private readonly Dictionary<string, float> _rewardedReloadTimeMap;

        public UnityAdsProxy(Timer timer) : base(timer)
        {
            _bannerReloadTimeMap = new Dictionary<string, float>();
            _interstitialReloadTimeMap = new Dictionary<string, float>();
            _rewardedReloadTimeMap = new Dictionary<string, float>();
        }

        public override void Initialize()
        {
            base.Initialize();

            InitializeAds();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public void InitializeAds()
        {
            _isDebugBuild = GameConstants.IsDebugBuild();

            SetGameID();
            SetAdIDs();

            Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);

            if (!Advertisement.isInitialized && Advertisement.isSupported)
                Advertisement.Initialize(_gameID, _isDebugBuild, this);
        }

        private void SetGameID()
        {

#if UNITY_IOS
            _gameID = AdsConstants.UnityAdsGameIDiOS;
#else
            _gameID = AdsConstants.UnityAdsGameIDAndroid;
#endif

        }

        private void SetAdIDs()
        {

#if UNITY_IOS
            _bannerID = AdsConstants.UnityAdsBannerIDiOS;
            _interstitialID = AdsConstants.UnityAdsInterstitialIDiOS;
            _rewardedID = AdsConstants.UnityAdsRewardedIDiOS;
#else
            _bannerID = AdsConstants.UnityAdsBannerIDAndroid;
            _interstitialID = AdsConstants.UnityAdsInterstitialIDAndroid;
            _rewardedID = AdsConstants.UnityAdsRewardedIDAndroid;
#endif

        }

        public void OnInitializationComplete()
        {
            INITIALIZED?.Invoke();
            Debug.Log("Unity Ads initialization complete.");
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
        }

        public override void OnPostTick()
        {
            if (_bannerReloadTimeMap.Count + _interstitialReloadTimeMap.Count + _rewardedReloadTimeMap.Count == 0)
                return;

            try
            {
                var keys = new List<string>(_bannerReloadTimeMap.Keys);
                foreach (var key in keys)
                {
                    if (Time.time >= _bannerReloadTimeMap[key])
                    {
                        _bannerReloadTimeMap.Remove(key);

                        BannerLoadOptions options = new BannerLoadOptions
                        {
                            loadCallback = OnBannerLoaded,
                            errorCallback = OnBannerError
                        };
                        Advertisement.Banner.Load(key, options);
                    }
                }

                keys = new List<string>(_interstitialReloadTimeMap.Keys);
                foreach (var key in keys)
                {
                    if (Time.time >= _interstitialReloadTimeMap[key])
                    {
                        _interstitialReloadTimeMap.Remove(key);
                        Advertisement.Load(key, this);
                    }
                }

                keys = new List<string>(_rewardedReloadTimeMap.Keys);
                foreach (var key in keys)
                {
                    if (Time.time >= _rewardedReloadTimeMap[key])
                    {
                        _rewardedReloadTimeMap.Remove(key);
                        Advertisement.Load(key, this);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }



        public override void LoadBanner()
        {
            _bannerReloadTimeMap[_bannerID] = Time.time;
        }

        public override void ShowBanner()
        {
            Debug.Log("Show banner");
            Advertisement.Banner.Show(_bannerID);
        }

        public override void HideBanner()
        {
            Advertisement.Banner.Hide();
        }

        public override void UnloadBanner()
        {
            HideBanner();
        }

        private void OnBannerLoaded()
        {
            Debug.Log("Banner loaded");
            _bannerRetryAttempt = 0;

            ON_BANNER_LOADED?.Invoke();
        }

        private void OnBannerError(string message)
        {
            Debug.Log($"Banner Error: {message}");

            _bannerRetryAttempt++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _bannerRetryAttempt));
            _bannerReloadTimeMap[_bannerID] = Time.time + _reloadDuration + retryDelay;
        }



        public override void LoadInterstitial()
        {
            _interstitialReloadTimeMap[_interstitialID] = Time.time;
        }

        public override void ShowInterstitial()
        {
            Advertisement.Show(_interstitialID, this);
        }



        public override void LoadRewarded()
        {
            _rewardedReloadTimeMap[_rewardedID] = Time.time;
        }

        public override void ShowRewarded()
        {
            Advertisement.Show(_rewardedID, this);
        }



        public void OnUnityAdsAdLoaded(string id)
        {
            if (id.Equals(_rewardedID))
                _rewardedRetryAttempt = 0;

            else if(id.Equals(_interstitialID))
                _interstitialRetryAttempt = 0;
        }

        public void OnUnityAdsFailedToLoad(string id, UnityAdsLoadError error, string message)
        {
            if(id.Equals(_rewardedID))
            {
                _rewardedRetryAttempt++;
                var retryDelay = (float)Math.Pow(2, Math.Min(6, _rewardedRetryAttempt));
                _rewardedReloadTimeMap[_rewardedID] = Time.time + _reloadDuration + retryDelay;
            }
            else if (id.Equals(_interstitialID))
            {
                _interstitialRetryAttempt++;
                var retryDelay = (float)Math.Pow(2, Math.Min(6, _interstitialRetryAttempt));
                _interstitialReloadTimeMap[_interstitialID] = Time.time + _reloadDuration + retryDelay;
            }
        }

        public void OnUnityAdsShowFailure(string id, UnityAdsShowError error, string message)
        {

        }

        public void OnUnityAdsShowStart(string id)
        {

        }

        public void OnUnityAdsShowClick(string id)
        {

        }

        public void OnUnityAdsShowComplete(string id, UnityAdsShowCompletionState showCompletionState)
        {
            if(!showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
                return;

            if (id.Equals(_rewardedID))
            {
                ON_REWARDED_WATCHED?.Invoke();
                LoadRewarded();
            }
            else if(id.Equals(_interstitialID))
            {
                ON_INTERSTITIAL_WATCHED?.Invoke();
                LoadInterstitial();
            }
        }
    }
}

