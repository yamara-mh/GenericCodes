using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Yamara.FontLoader
{
    public class FallbackFontLoader : MonoBehaviour
    {
        public enum LinkType
        {
            Manual = 0,
            Instance = 1,
            Active = 2,
        }
        [SerializeField] private LinkType _linkType = LinkType.Instance;
        [SerializeField] private bool _updateAllTextOnLoad = true;
        [SerializeField] private List<AssetReferenceT<FallbackFontData>> _fontSettingsRefs;
        [SerializeField] public UnityEvent<IEnumerable<TMP_FontAsset>> OnLoadedEvent;
        [SerializeField] public UnityEvent OnUnloadedEvent;

        public bool IsLoaded { get; private set; }

        private readonly Subject<IEnumerable<TMP_FontAsset>> onLoadedSubject = new Subject<IEnumerable<TMP_FontAsset>>();
        public IObservable<IEnumerable<TMP_FontAsset>> OnLoaded => onLoadedSubject;

        private readonly Subject<Unit> onUnloadedSubject = new Subject<Unit>();
        public IObservable<Unit> OnUnloaded => onUnloadedSubject;

        private FallbackFontData[] _settings;
        private List<TMP_FontAsset> _loadedFonts;
        private CancellationTokenSource _loadCtSource;

        private void Start()
        {
            if (_linkType == LinkType.Instance)
            {
                _loadCtSource = new CancellationTokenSource();
                Activate();
            }
        }
        private void OnDestroy()
        {
            if (_linkType == LinkType.Instance)
            {
                _loadCtSource?.Cancel();
                Deactivate();
            }
        }

        private void OnEnable()
        {
            if (_linkType == LinkType.Active)
            {
                _loadCtSource = new CancellationTokenSource();
                Activate();
            }
        }
        private void OnDisable()
        {
            if (_linkType == LinkType.Active)
            {
                _loadCtSource?.Cancel();
                Deactivate();
            }
        }

        private async void Activate()
        {
            LocalizationSettings.SelectedLocaleChanged += ReloadFonts;
            await LoadFontsAsync(await LocalizationSettings.SelectedLocaleAsync);
        }
        private void Deactivate()
        {
            LocalizationSettings.SelectedLocaleChanged -= ReloadFonts;
            UnloadFonts();
        }

        public async void LoadFonts() => await LoadFontsAsync(LocalizationSettings.SelectedLocale);
        public async void LoadFonts(Locale locale) => await LoadFontsAsync(locale);
        public async UniTask LoadFontsAsync() => await LoadFontsAsync(LocalizationSettings.SelectedLocale);
        public async UniTask LoadFontsAsync(Locale locale)
        {
            _loadCtSource?.Cancel();
            _loadCtSource = new CancellationTokenSource();
            IsLoaded = false;
            _loadedFonts ??= new List<TMP_FontAsset>();
            _loadedFonts.Clear();

            _settings ??= (await _fontSettingsRefs.Select(async r => await r.LoadAssetAsync())).ToArray();
            foreach (var setting in _settings)
            {
                if (setting.LoadOnStartup)
                {
                    Debug.LogWarning($"Skip loading because {setting.name} is set to load on startup.");
                    continue;
                }
                var font = await setting.LoadFontsAsync(locale, _loadCtSource.Token);
                _loadedFonts.Add(font);
            }
            IsLoaded = true;
            onLoadedSubject.OnNext(_loadedFonts);
            OnLoadedEvent.Invoke(_loadedFonts);

            if (_updateAllTextOnLoad) FallbackFontData.UpdateAllText(_loadedFonts.ToArray());
        }

        public async void ReloadFonts(Locale locale) => await ReloadFontsAsync(locale);
        public async UniTask ReloadFontsAsync(Locale locale)
        {
            UnloadFonts();
            await LoadFontsAsync(locale);
        }
        public async void UnloadFonts()
        {
            IsLoaded = false;
            _loadedFonts ??= new List<TMP_FontAsset>();

            _settings ??= (await _fontSettingsRefs.Select(async r => await r.LoadAssetAsync())).ToArray();
            foreach (var setting in _settings) setting.UnloadFallbacks();

            if (_updateAllTextOnLoad) FallbackFontData.UpdateAllText(_loadedFonts.ToArray());

            onUnloadedSubject.OnNext(Unit.Default);
            OnUnloadedEvent.Invoke();

            _loadedFonts.Clear();
        }
    }
}
