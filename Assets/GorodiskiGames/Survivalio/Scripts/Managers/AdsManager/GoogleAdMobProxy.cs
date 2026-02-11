using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

namespace Game.Managers
{
    public sealed class GoogleAdMobProxy : BaseAdsProxy
    {
        private const int _reloadDuration = 1;

        private readonly Dictionary<string, float> _bannerReloadTimeMap;
        private readonly Dictionary<string, float> _rewardedReloadTimeMap;
        private readonly Dictionary<string, float> _interstitialReloadTimeMap;
        
        private int _interstitialRetryAttempt;
        private int _rewardedRetryAttempt;
        private int _bannerRetryAttempt;

        BannerView _bannerView;
        InterstitialAd _interstitialAd;
        RewardedAd _rewardedAd;

        private bool _isDebugBuild;
        private bool _isTestDevice;
        private bool _isInitialized;

        private string _bannerID;
        private string _interstitialID;
        private string _rewardedID;

        public GoogleAdMobProxy(Timer timer) : base(timer)
        {
            _interstitialReloadTimeMap = new Dictionary<string, float>();
            _rewardedReloadTimeMap = new Dictionary<string, float>();
            _bannerReloadTimeMap = new Dictionary<string, float>();
        }

        public override void Initialize()
        {
            base.Initialize();

            _isDebugBuild = GameConstants.IsDebugBuild();
            _isTestDevice = TestDeviceIDs.IsTestDevice();

            SetIDs();
            RequestConsentInfoUpdate();
        }

        public override void Dispose()
        {
            base.Dispose();
            _isInitialized = false;
        }

        private void SetIDs()
        {
#if UNITY_ANDROID
            _bannerID = _isDebugBuild ? AdsConstants.AdMobBannerIDAndroidTest : AdsConstants.AdMobBannerIDAndroid;
            _interstitialID = _isDebugBuild ? AdsConstants.AdMobInterstitialIDAndroidTest : AdsConstants.AdMobInterstitialIDAndroid;
            _rewardedID = _isDebugBuild ? AdsConstants.AdMobRewardedIDAndroidTest : AdsConstants.AdMobRewardedIDAndroid;
#elif UNITY_IOS
            _bannerID = _isDebugBuild ? AdsConstants.AdMobBannerIDiOSTest : AdsConstants.AdMobBannerIDiOS;
            _interstitialID = _isDebugBuild ? AdsConstants.AdMobInterstitialIDiOSTest : AdsConstants.AdMobInterstitialIDiOS;
            _rewardedID = _isDebugBuild ? AdsConstants.AdMobRewardedIDiOSTest : AdsConstants.AdMobRewardedIDiOS;
#else
            _bannerID = AdsConstants.AdMobBannerIDAndroidTest;
            _interstitialID = AdsConstants.AdMobInterstitialIDAndroidTest;
            _rewardedID = AdsConstants.AdMobRewardedIDAndroidTest;
#endif
        }

        private void RequestConsentInfoUpdate()
        {
            ConsentRequestParameters request = new ConsentRequestParameters();

            if(_isTestDevice)
            {
                var debugSettings = new ConsentDebugSettings();

                debugSettings.DebugGeography = DebugGeography.EEA;
                debugSettings.TestDeviceHashedIds = TestDeviceIDs.List;

                request.ConsentDebugSettings = debugSettings;
            }

            ConsentInformation.Update(request, OnConsentInfoUpdated);
        }

        private void OnConsentInfoUpdated(FormError consentError)
        {
            if (consentError != null)
            {
                Debug.LogError(consentError);
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError != null)
                {
                    Debug.LogError(consentError);
                    return;
                }

                if (ConsentInformation.CanRequestAds())
                    InitializeAdMob();
            });
        }

        private void InitializeAdMob()
        {
            if(_isInitialized)
                return;

            MobileAds.Initialize(initStatus => { OnInitialized(); });
        }

        private void OnInitialized()
        {
            _isInitialized = true;
            INITIALIZED?.Invoke();
        }

        public override void OnPostTick()
        {
            if (_bannerReloadTimeMap.Count + _interstitialReloadTimeMap.Count + _rewardedReloadTimeMap.Count == 0)
                return;

            try
            {
                var keys = new List<string>(_rewardedReloadTimeMap.Keys);
                foreach (var key in keys)
                {
                    if (Time.time >= _rewardedReloadTimeMap[key])
                    {
                        _rewardedReloadTimeMap.Remove(key);
                        if (_rewardedAd != null)
                        {
                            _rewardedAd.Destroy();
                            _rewardedAd = null;
                        }
                        Debug.Log("Loading rewarded ad.");
                        var adRequest = new AdRequest();
                        RewardedAd.Load(key, adRequest, (RewardedAd ad, LoadAdError error) =>
                        {
                            if (error != null || ad == null)
                            {
                                Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                                OnRewardedAdLoadFailedEvent(key);
                                return;
                            }
                            Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
                            _rewardedAd = ad;
                            OnRewardedAdLoadedEvent();

                            ad.OnAdFullScreenContentClosed += () =>
                            {
                                Debug.Log("Rewarded ad full screen content closed.");
                                OnRewardedAdHiddenEvent();
                            };

                        });
                    }
                }

                keys = new List<string>(_interstitialReloadTimeMap.Keys);
                foreach (var key in keys)
                {
                    if (Time.time >= _interstitialReloadTimeMap[key])
                    {
                        _interstitialReloadTimeMap.Remove(key);
                        if (_interstitialAd != null)
                        {
                            _interstitialAd.Destroy();
                            _interstitialAd = null;
                        }
                        Debug.Log("Loading interstitial ad.");
                        var adRequest = new AdRequest();
                        InterstitialAd.Load(key, adRequest, (InterstitialAd ad, LoadAdError error) =>
                        {
                            if (error != null || ad == null)
                            {
                                Debug.LogError("Interstitial ad failed to load an ad with error : " + error);
                                OnInterstitialAdLoadFailedEvent(key);
                                return;
                            }
                            Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
                            _interstitialAd = ad;
                            OnInterstitialAdLoadedEvent();

                            ad.OnAdFullScreenContentClosed += () =>
                            {
                                Debug.Log("Interstitial ad full screen content closed.");
                                OnInterstitialHiddenEvent();
                            };

                        });
                    }
                }

                keys = new List<string>(_bannerReloadTimeMap.Keys);
                foreach (var key in keys)
                {
                    if (Time.time >= _bannerReloadTimeMap[key])
                    {
                        _bannerReloadTimeMap.Remove(key);

                        if (_bannerView == null)
                            _bannerView = new BannerView(key, AdSize.Banner, AdPosition.Bottom);

                        _bannerView.OnBannerAdLoaded += () =>
                        {
                            Debug.Log("Banner view loaded an ad with response : " + _bannerView.GetResponseInfo());
                            OnBannerLoaded();
                        };
                        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
                        {
                            Debug.LogError("Banner view failed to load an ad with error : " + error);
                            OnBannerAdLoadFailed();
                        };

                        var adRequest = new AdRequest();
                        _bannerView.LoadAd(adRequest);

                        HideBanner();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }



        //banner
        public override void LoadBanner()
        {
            var key = _bannerID;
            if (string.IsNullOrEmpty(key))
                return;

            _bannerReloadTimeMap[key] = Time.time;
        }

        public override void ShowBanner()
        {
            if (_bannerView != null)
            {
                Debug.Log("Show banner.");
                _bannerView.Show();
            }
        }

        public override void HideBanner()
        {
            if (_bannerView != null)
            {
                Debug.Log("Hide banner.");
                _bannerView.Hide();
            }
        }

        public override void UnloadBanner()
        {
            if (_bannerView != null)
            {
                Debug.Log("Destroying banner.");
                _bannerView.Destroy();
                _bannerView = null;
            }
        }

        private void OnBannerLoaded()
        {
            Debug.Log("Banner loaded");
            _bannerRetryAttempt = 0;

            ON_BANNER_LOADED?.Invoke();
        }

        private void OnBannerAdLoadFailed()
        {
            _bannerRetryAttempt++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _bannerRetryAttempt));
            _bannerReloadTimeMap[_bannerID] = Time.time + _reloadDuration + retryDelay;
        }



        //interstitial
        public override void LoadInterstitial()
        {
            var key = _interstitialID;
            if (string.IsNullOrEmpty(key))
                return;

            _interstitialReloadTimeMap[key] = Time.time;
        }

        public override void ShowInterstitial()
        {
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                Debug.Log("Showing interstitial ad.");
                ON_INTERSTITIAL_SHOW?.Invoke();
                _interstitialAd.Show();
            }
            else
            {
                Debug.LogError("Interstitial ad is not ready yet.");
            }
        }

        private void OnInterstitialAdLoadedEvent()
        {
            _interstitialRetryAttempt = 0;
        }

        private void OnInterstitialAdLoadFailedEvent(string key)
        {
            _interstitialRetryAttempt++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _interstitialRetryAttempt));
            _interstitialReloadTimeMap[key] = Time.time + _reloadDuration + retryDelay;
        }

        private void OnInterstitialHiddenEvent()
        {
            try
            {
                ON_INTERSTITIAL_WATCHED?.Invoke();
                LoadInterstitial();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }



        //rewarded
        public override void LoadRewarded()
        {
            var key = _rewardedID;
            if (string.IsNullOrEmpty(key))
                return;

            _rewardedReloadTimeMap[key] = Time.time;
        }

        public override void ShowRewarded()
        {
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                Debug.Log("Showing rewarded ad.");
                _rewardedAd.Show((Reward reward) =>
                {
                    Debug.Log(String.Format("Rewarded ad granted a reward: {0} {1}",
                                            reward.Amount,
                                            reward.Type));
                });
            }
            else
            {
                Debug.LogError("Rewarded ad is not ready yet.");
            }
        }

        private void OnRewardedAdLoadedEvent()
        {
            _rewardedRetryAttempt = 0;
        }

        private void OnRewardedAdLoadFailedEvent(string key)
        {
            _rewardedRetryAttempt++;
            var retryDelay = (float)Math.Pow(2, Math.Min(6, _rewardedRetryAttempt));
            _rewardedReloadTimeMap[key] = Time.time + _reloadDuration + retryDelay;
        }

        private void OnRewardedAdHiddenEvent()
        {
            try
            {
                ON_REWARDED_WATCHED?.Invoke();
                LoadRewarded();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}

