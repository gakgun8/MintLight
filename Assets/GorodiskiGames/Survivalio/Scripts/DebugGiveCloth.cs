using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Config;
using Game.Player;
using Game.UI.Hud;
using UnityEngine;

public class RuntimeDebugClothTool : MonoBehaviour
{
    public string itemInput = "0";
    public bool dontDestroyOnLoad = true;
    public bool showGui = true;
    public KeyCode toggleKey = KeyCode.F1;

    void NotifyPlayerModelChanged()
    {
        if (_playerModel == null) return;

        // Observable.SetChanged()가 public이면 이 한 줄로 끝남
        // (PlayerController에서 쓰는 걸 보면 public일 확률 높음)
        try
        {
            _playerModel.SetChanged();
            return;
        }
        catch
        {
            // 혹시 접근 제한/다른 구조면 리플렉션으로 시도
        }

        try
        {
            var m = _playerModel.GetType().GetMethod("SetChanged",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            m?.Invoke(_playerModel, null);
        }
        catch { }
    }

    void ForceReopenEquipmentHud()
    {
        // 비활성 포함해서 찾기
        var hud = Resources.FindObjectsOfTypeAll<Game.UI.Hud.EquipmentHudView>()
            .FirstOrDefault(v => v != null && v.gameObject.scene.IsValid());

        if (hud == null) return;

        // 장비창 오브젝트를 껐다 켜서 OnEnable/Show 흐름 재실행 유도
        var go = hud.gameObject;
        bool wasActive = go.activeSelf;

        if (wasActive)
        {
            go.SetActive(false);
            go.SetActive(true);
        }

        // 레이아웃 강제 갱신(그리드/컨텐츠 즉시 정렬)
        if (hud.Content != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(hud.Content);
    }



    [Header("Remove Options")]
    public bool allowRemoveEquipped = false; // 장착중 삭제 허용(비추천)

    GameConfig _gameConfig;
    PlayerData _pdCache;
    PlayerModel _playerModel; // ✅ 실제 런타임 모델
    void ForceRefreshEquipmentHud()
    {
        if (_playerModel == null) return;

        var hud = Resources.FindObjectsOfTypeAll<EquipmentHudView>()
            .FirstOrDefault(v => v != null && v.gameObject.scene.IsValid());

        if (hud == null) return;

        // Model setter 강제 호출
        var prop = typeof(EquipmentHudView)
            .GetProperty("Model", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (prop != null)
        {
            prop.SetValue(hud, _playerModel);
        }

        // 레이아웃 즉시 갱신
        if (hud.Content != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(hud.Content);
    }
    void Awake()
    {
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _gameConfig = GameConfig.Load();
        if (_gameConfig == null)
            Debug.LogError("[DebugClothTool] GameConfig.Load 실패");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showGui = !showGui;

        // ✅ 플레이어가 늦게 스폰되는 경우가 많아서 계속 잡아줌(가벼움)
        if (_playerModel == null)
        {
            var pv = FindObjectOfType<PlayerView>();
            if (pv != null)
            {
                // 🔧 pv.Model 접근 금지 → 리플렉션으로 protected Model 읽기
                _playerModel = GetProtectedModel<PlayerModel>(pv);
            }
        }
    }

    // ======= 핵심: 실제 데이터 참조 얻기 =======
    bool TryGetLiveData(out List<int> equipped, out Dictionary<int, int> stored, out Dictionary<int, int> levels)
    {
        equipped = null; stored = null; levels = null;

        if (_playerModel != null)
        {
            equipped = _playerModel.EquippedCloth;
            stored = _playerModel.StoredCloth;
            levels = _playerModel.ClothLevels;
            return (equipped != null && stored != null && levels != null);
        }

        // fallback: 아직 플레이어가 없으면 PlayerData(저장데이터)라도 수정(즉시 반영은 안될 수 있음)
        if (_gameConfig == null) return false;

        _pdCache ??= PlayerData.Load(_gameConfig);

        // null 방지
        _pdCache.StoredCloth ??= new Dictionary<int, int>();
        _pdCache.ClothLevels ??= new Dictionary<int, int>();
        _pdCache.EquippedCloth ??= new List<int>();

        equipped = _pdCache.EquippedCloth;
        stored = _pdCache.StoredCloth;
        levels = _pdCache.ClothLevels;
        return true;
    }

    void SaveNow()
    {
        // ✅ 프로젝트마다 Save 함수 형태가 달라서: "있으면 호출" 방식으로 컴파일 안전하게 처리
        if (_playerModel != null)
        {
            TryInvokeSave(_playerModel);
        }
        else if (_pdCache != null)
        {
            // PlayerData 쪽 Save도 있으면 호출
            TryInvokeSave(_pdCache);

            // 혹시 Save(GameConfig) 형태가 있으면 그걸 우선 시도
            TryInvokeSaveWithArg(_pdCache, _gameConfig);
        }
    }
    void RefreshEquipmentUI()
    {
        // 1) PlayerModel 쪽 "변경 알림" 계열 메서드가 있으면 호출
        if (_playerModel != null)
        {
            // 프로젝트마다 이름이 다르니 가능한 후보를 넓게 호출
            TryInvokeAny(_playerModel,
                "NotifyChanged", "RaiseChanged", "OnChanged",
                "Refresh", "Rebuild", "Invalidate",
                "RefreshCloth", "RefreshClothes",
                "RebuildClothPrefabMap", "UpdateClothPrefabMap", "UpdateClothMap",
                "ApplyCloth", "ApplyClothes"
            );
        }

        // 2) 씬에 떠있는 장비/인벤토리 HUD가 있으면 갱신 호출
        // (주의) 비활성 오브젝트도 잡히게 Resources.FindObjectsOfTypeAll 사용
        var allBehaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        foreach (var b in allBehaviours)
        {
            if (b == null) continue;

            // 에디터/에셋 프리팹 제외(런타임 씬 오브젝트만)
            if (b.gameObject.scene.name == null) continue;

            var tn = b.GetType().Name;

            // 장비/인벤토리/프리뷰/허드 관련만 좁혀서 호출 (과호출 방지)
            if (!(tn.Contains("Equipment", StringComparison.OrdinalIgnoreCase) ||
                  tn.Contains("Inventory", StringComparison.OrdinalIgnoreCase) ||
                  tn.Contains("Hud", StringComparison.OrdinalIgnoreCase) ||
                  tn.Contains("Preview", StringComparison.OrdinalIgnoreCase)))
                continue;

            TryInvokeAny(b,
                "Refresh", "Rebuild", "Invalidate", "UpdateView", "Redraw", "Reload"
            );
        }
    }

    static void TryInvokeAny(object target, params string[] methodNames)
    {
        if (target == null) return;

        var type = target.GetType();

        foreach (var name in methodNames)
        {
            // 1) 파라미터 없는 메서드
            var m0 = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (m0 != null)
            {
                try { m0.Invoke(target, null); } catch { }
                continue;
            }

            // 2) (PlayerModel 같은) "OnModelChanged(Model)" 형태가 있을 수도 있어 후보 처리
            //    여기서는 안전을 위해 호출은 안 하고 스킵(원하면 확장 가능)
        }
    }

    public void AddCloths(IEnumerable<int> clothConfigIndexes, int defaultLevel = 0)
    {
        if (!TryGetLiveData(out var equipped, out var stored, out var levels))
            return;

        int added = 0;

        foreach (var idx in clothConfigIndexes.Distinct())
        {
            if (idx < 0) continue;

            // 이미 있으면 스킵
            if (stored.Values.Contains(idx)) continue;

            int serial = NextFreeSerial(stored);
            stored[serial] = idx;
            levels[serial] = defaultLevel;
            added++;
        }

        Debug.Log($"[DebugClothTool] Add +{added} (Total={stored.Count})");
        SaveNow();
        NotifyPlayerModelChanged();     // ✅ 추가
        ForceReopenEquipmentHud();      // ✅ 추가(확실하게)



    }

    public void RemoveCloths(IEnumerable<int> clothConfigIndexes)
    {
        if (!TryGetLiveData(out var equipped, out var stored, out var levels))
            return;

        var targetIdx = new HashSet<int>(clothConfigIndexes.Distinct());

        // configIndex -> serial 찾기
        var serials = stored.Where(kv => targetIdx.Contains(kv.Value)).Select(kv => kv.Key).ToList();

        int removed = 0;
        foreach (var serial in serials)
        {
            bool isEquipped = equipped.Contains(serial);
            if (isEquipped && !allowRemoveEquipped)
            {
                Debug.LogWarning($"[DebugClothTool] 장착중(serial={serial})이라 삭제 스킵. allowRemoveEquipped=true로 강제 가능");
                continue;
            }

            equipped.Remove(serial);     // 장착중이면 제거(허용 시)
            stored.Remove(serial);
            levels.Remove(serial);
            removed++;
        }

        Debug.Log($"[DebugClothTool] Remove -{removed} (Total={stored.Count})");
        SaveNow();
        NotifyPlayerModelChanged();
        ForceReopenEquipmentHud();

    }

    public static List<int> ParseItemList(string text)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        var tokens = text.Replace(",", " ").Replace(";", " ")
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var t in tokens)
        {
            if (t.Contains('-'))
            {
                var p = t.Split('-');
                if (p.Length == 2 && int.TryParse(p[0], out int a) && int.TryParse(p[1], out int b))
                {
                    int min = Mathf.Min(a, b);
                    int max = Mathf.Max(a, b);
                    for (int i = min; i <= max; i++) result.Add(i);
                }
            }
            else if (int.TryParse(t, out int v))
                result.Add(v);
        }
        return result;
    }

    static int NextFreeSerial(Dictionary<int, int> dict)
    {
        int serial = 1;
        var used = new HashSet<int>(dict.Keys);
        while (used.Contains(serial)) serial++;
        return serial;
    }

    void OnGUI()
    {
        if (!showGui) return;

        GUI.Box(new Rect(10, 10, 520, 220), "Runtime Debug Cloth Tool (F1 토글)");
        GUILayout.BeginArea(new Rect(20, 40, 500, 190));

        GUILayout.Label("아이템 번호 입력: 1,2,3 / 10-20 / 5 6 7 혼용 가능");
        itemInput = GUILayout.TextField(itemInput);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("추가(Add)", GUILayout.Height(30)))
            AddCloths(ParseItemList(itemInput), 0);

        if (GUILayout.Button("삭제(Remove)", GUILayout.Height(30)))
            RemoveCloths(ParseItemList(itemInput));
        GUILayout.EndHorizontal();

        GUILayout.Label($"allowRemoveEquipped: {allowRemoveEquipped}");

        if (TryGetLiveData(out var equipped, out var stored, out var levels))
        {
            GUILayout.Label($"StoredCloth: {stored.Count} / EquippedCloth: {equipped.Count}");
        }
        else
        {
            GUILayout.Label("데이터 로드 실패(GameConfig/PlayerView 확인 필요)");
        }

        GUILayout.EndArea();
    }

    // =========================
    // 🔧 Reflection helpers
    // =========================

    static T GetProtectedModel<T>(MonoBehaviour view) where T : class
    {
        if (!view) return null;

        var type = view.GetType();
        while (type != null)
        {
            var prop = type.GetProperty("Model", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (prop != null)
                return prop.GetValue(view) as T;

            type = type.BaseType;
        }
        return null;
    }

    static void TryInvokeSave(object obj)
    {
        if (obj == null) return;

        var type = obj.GetType();
        var m = type.GetMethod("Save", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (m != null)
        {
            try { m.Invoke(obj, null); }
            catch (Exception e) { Debug.LogWarning($"[DebugClothTool] Save() invoke failed: {e.Message}"); }
        }
    }

    static void TryInvokeSaveWithArg(object obj, object arg0)
    {
        if (obj == null || arg0 == null) return;

        var type = obj.GetType();
        var m = type.GetMethod("Save", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { arg0.GetType() }, null);
        if (m != null)
        {
            try { m.Invoke(obj, new[] { arg0 }); }
            catch (Exception e) { Debug.LogWarning($"[DebugClothTool] Save(arg) invoke failed: {e.Message}"); }
        }
    }
}
