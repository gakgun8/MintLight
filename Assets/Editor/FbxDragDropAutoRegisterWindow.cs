using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FbxDragDropAutoRegisterWindow : EditorWindow
{
    // EditorPrefs keys
    const string PREF_PREFAB_ROOT = "Project_PrefabRootFolder";
    const string PREF_ICON_ROOT = "Project_IconRootFolder";
    const string PREF_CONFIG_PATH = "Project_PartCatalogPath";
    const string PREF_DB_PATH = "Project_ProcessedDbPath";

    DefaultAsset prefabRootFolder;
    DefaultAsset iconRootFolder;
    PartCatalog catalogAsset;
    ProcessedFbxDb processedDb;

    // Drop Queue
    readonly List<string> droppedModelPaths = new();

    // 코드 → 폴더명 매핑
    static readonly Dictionary<string, string> CodeToFolder = new(StringComparer.OrdinalIgnoreCase)
    {
        { "HD", "Head" },
        { "BD", "Body" },
        { "LG", "Leg" },
        { "BP", "BackPack" },
        { "SH", "Shoes" },
    };
    bool ConfirmOverwriteIfNeeded()
    {
        bool hasCatalog = catalogAsset != null && catalogAsset.entries != null && catalogAsset.entries.Count > 0;
        bool hasProcessed = processedDb != null && processedDb.items != null && processedDb.items.Count > 0;

        if (!hasCatalog && !hasProcessed)
            return true;

        string msg =
            $"이미 데이터가 존재합니다.\n\n" +
            $"- PartCatalog: {(hasCatalog ? catalogAsset.entries.Count : 0)}개\n" +
            $"- ProcessedDb: {(hasProcessed ? processedDb.items.Count : 0)}개\n\n" +
            $"덮어쓰기를 선택하면 위 리스트를 모두 삭제하고 새로 생성합니다.\n" +
            $"추가(유지)를 선택하면 기존 리스트는 유지되고, 같은 키는 갱신/없는 키는 추가됩니다.";

        int choice = EditorUtility.DisplayDialogComplex(
            "기존 DB 데이터가 있습니다",
            msg,
            "덮어쓰기",   // 0
            "취소",       // 1
            "추가(유지)"  // 2
        );

        if (choice == 1) // 취소
            return false;

        if (choice == 0) // 덮어쓰기
        {
            if (hasCatalog) catalogAsset.entries.Clear();
            if (hasProcessed) processedDb.items.Clear();

            EditorUtility.SetDirty(catalogAsset);
            EditorUtility.SetDirty(processedDb);
            AssetDatabase.SaveAssets();
        }

        return true;
    }


    IconCaptureUtility.CaptureSettings captureSettings = new IconCaptureUtility.CaptureSettings
    {
        resolution = 512,
        transparentBackground = true,

        cameraEuler = new Vector3(17f, 143f, 0f),
        autoFrameByBounds = true,
        boundsPadding = 1.15f,
        cameraDistance = 2.5f,

        keyIntensity = 1.0f,
        keyEuler = new Vector3(162f, -20f, 0f),

        // 기본 OFF
        useFillLight = false,
        fillIntensity = 0.8f,
        fillEuler = new Vector3(15f, 120f, 0f),

        useRimLight = false,
        rimIntensity = 1.2f,
        rimEuler = new Vector3(70f, 160f, 0f),
    };

    [MenuItem("Tools/Charactor Setting")]
    public static void Open()
    {
        var w = GetWindow<FbxDragDropAutoRegisterWindow>("FBX Drag & Register");
        w.minSize = new Vector2(560, 620);
        w.LoadPrefs();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Targets (경로 저장됨)", EditorStyles.boldLabel);

        prefabRootFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefab Root Folder", prefabRootFolder, typeof(DefaultAsset), false);
        iconRootFolder = (DefaultAsset)EditorGUILayout.ObjectField("Icon Root Folder", iconRootFolder, typeof(DefaultAsset), false);
        catalogAsset = (PartCatalog)EditorGUILayout.ObjectField("Part Catalog Asset", catalogAsset, typeof(PartCatalog), false);
        processedDb = (ProcessedFbxDb)EditorGUILayout.ObjectField("Processed DB Asset", processedDb, typeof(ProcessedFbxDb), false);

        EditorGUILayout.Space(10);
        DrawDropArea();

        EditorGUILayout.Space(10);
        DrawQueue();

        EditorGUILayout.Space(10);
        DrawCaptureSettings();

        EditorGUILayout.Space(12);
        using (new EditorGUI.DisabledScope(!CanRun()))
        {
            if (GUILayout.Button("Run: Create Prefabs + Capture Icons + Update Config", GUILayout.Height(38)))
            {
                SavePrefs();

                // ✅ DB에 이미 데이터가 있으면 확인창
                if (!ConfirmOverwriteIfNeeded())
                    return;

                ProcessDropped();

            }


        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Clear Queue"))
            droppedModelPaths.Clear();

        if (GUILayout.Button("Save Paths Now"))
            SavePrefs();
    }

    bool CanRun()
    {
        return IsFolder(prefabRootFolder) && IsFolder(iconRootFolder)
               && catalogAsset != null && processedDb != null
               && droppedModelPaths.Count > 0;
    }

    static bool IsFolder(DefaultAsset a)
    {
        if (a == null) return false;
        var p = AssetDatabase.GetAssetPath(a);
        return AssetDatabase.IsValidFolder(p);
    }

    void DrawDropArea()
    {
        Rect dropRect = GUILayoutUtility.GetRect(0, 120, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "여기에 FBX(또는 Model 에셋)를 드래그&드롭\n\n- Project 창에서 FBX 선택해서 드롭\n- 여러 개 동시 드롭 가능\n\n※ Chr_001_HD_001 규칙 기반 자동 분류", EditorStyles.helpBox);

        Event evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj == null) continue;
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path)) continue;

                    // FBX / Model만 받기
                    if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    {
                        AddUnique(path);
                    }
                    else
                    {
                        // Model 타입(GameObject) 드롭했을 때도 경로가 model이면 처리
                        var go = obj as GameObject;
                        if (go != null)
                        {
                            var p = AssetDatabase.GetAssetPath(go);
                            if (p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                                AddUnique(p);
                        }
                    }
                }
            }
            evt.Use();
        }
    }

    void AddUnique(string assetPath)
    {
        if (!droppedModelPaths.Contains(assetPath))
            droppedModelPaths.Add(assetPath);
    }

    void DrawQueue()
    {
        EditorGUILayout.LabelField($"Queue ({droppedModelPaths.Count})", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (droppedModelPaths.Count == 0)
            {
                EditorGUILayout.LabelField("드롭된 FBX가 없습니다.");
                return;
            }

            int removeIndex = -1;
            for (int i = 0; i < droppedModelPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(droppedModelPaths[i]);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                    removeIndex = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeIndex >= 0) droppedModelPaths.RemoveAt(removeIndex);
        }
    }

    void DrawCaptureSettings()
    {
        EditorGUILayout.LabelField("Icon Capture Settings", EditorStyles.boldLabel);

        captureSettings.resolution = EditorGUILayout.IntPopup("Resolution", captureSettings.resolution,
            new[] { "256", "512", "1024", "2048" }, new[] { 256, 512, 1024, 2048 });
        captureSettings.transparentBackground = EditorGUILayout.Toggle("Transparent BG", captureSettings.transparentBackground);

        captureSettings.cameraEuler = EditorGUILayout.Vector3Field("Camera Euler", captureSettings.cameraEuler);
        captureSettings.autoFrameByBounds = EditorGUILayout.Toggle("Auto Frame By Bounds", captureSettings.autoFrameByBounds);
        if (captureSettings.autoFrameByBounds)
            captureSettings.boundsPadding = EditorGUILayout.Slider("Bounds Padding", captureSettings.boundsPadding, 1.0f, 2.0f);
        else
            captureSettings.cameraDistance = EditorGUILayout.FloatField("Camera Distance", captureSettings.cameraDistance);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Lighting (Fill/Rim 기본 OFF)", EditorStyles.boldLabel);
        captureSettings.keyIntensity = EditorGUILayout.Slider("Key Intensity", captureSettings.keyIntensity, 0f, 8f);
        captureSettings.keyEuler = EditorGUILayout.Vector3Field("Key Euler", captureSettings.keyEuler);

        captureSettings.useFillLight = EditorGUILayout.Toggle("Use Fill Light", captureSettings.useFillLight);
        if (captureSettings.useFillLight)
        {
            captureSettings.fillIntensity = EditorGUILayout.Slider("Fill Intensity", captureSettings.fillIntensity, 0f, 8f);
            captureSettings.fillEuler = EditorGUILayout.Vector3Field("Fill Euler", captureSettings.fillEuler);
        }

        captureSettings.useRimLight = EditorGUILayout.Toggle("Use Rim Light", captureSettings.useRimLight);
        if (captureSettings.useRimLight)
        {
            captureSettings.rimIntensity = EditorGUILayout.Slider("Rim Intensity", captureSettings.rimIntensity, 0f, 8f);
            captureSettings.rimEuler = EditorGUILayout.Vector3Field("Rim Euler", captureSettings.rimEuler);
        }
    }

    void ProcessDropped()
    {
        string prefabRootPath = AssetDatabase.GetAssetPath(prefabRootFolder);
        string iconRootPath = AssetDatabase.GetAssetPath(iconRootFolder);

        int processed = 0, skipped = 0;

        try
        {
            foreach (var modelPath in droppedModelPaths.ToList())
            {
                if (!modelPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    skipped++;
                    continue;
                }

                // 이미 처리한 건 스킵 (원하면 '강제 재처리' 토글을 추가해도 됨)
                if (!ShouldProcess(modelPath, out var guid, out var sig, out var ticks))
                {
                    skipped++;
                    continue;
                }

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null) { skipped++; continue; }

                string fileName = Path.GetFileNameWithoutExtension(modelPath);

                var (partCode, folderName) = DetectPartFromFileName(fileName);

                string prefabFolder = EnsureSubFolder(prefabRootPath, folderName);
                string iconFolder = EnsureSubFolder(iconRootPath, folderName);

                string prefabAssetPath = $"{prefabFolder}/{fileName}.prefab";

                // Prefab 생성/갱신
                var prefab = CreateOrUpdatePrefabFromModel(model, prefabAssetPath);
                if (prefab == null) { skipped++; continue; }

                // Icon 캡처
                string iconPngAssetPath = $"{iconFolder}/{fileName}.png";
                string iconPngFullPath = ToFullPath(iconPngAssetPath);

                bool ok = IconCaptureUtility.CapturePrefabToPng(prefab, iconPngFullPath, partCode, captureSettings);

                if (ok)
                {
                    AssetDatabase.ImportAsset(iconPngAssetPath, ImportAssetOptions.ForceUpdate);
                    IconCaptureUtility.ImportPngAsSprite(iconPngAssetPath);
                }

                // ===================== [PATCH] ClothConfig 생성/갱신 =====================

                // PREF_CONFIG_PATH는 "PartCatalog.asset" 파일 경로로 저장되어 있을 가능성이 높음
                var prefConfigPath = EditorPrefs.GetString(PREF_CONFIG_PATH, "Assets/Game/Configs/PartCatalog.asset");
                var configBaseFolder = System.IO.Path.GetDirectoryName(prefConfigPath).Replace("\\", "/");

                // PartCatalog.asset 옆에 ClothConfigs 폴더 만들고, 그 아래 파츠 폴더(=folderName)로 정리
                var configFolder = EnsureSubFolder(configBaseFolder, "ClothConfigs");
                var clothConfigFolder = EnsureSubFolder(configFolder, folderName);

                // fileName 기준으로 ClothConfig 에셋 생성/갱신
                var clothConfigAssetPath = $"{clothConfigFolder}/{fileName}.asset";

                // ✅ CC_가 아니라, 네가 실제로 가진 함수 이름으로 호출
                CreateOrUpdateClothConfigAsset(
                    clothConfigAssetPath,
                    fileName,
                    prefabAssetPath,
                    iconPngAssetPath,
                    modelPath
                );

                // ===================== [PATCH END] =====================






                // Config 업데이트
                var entry = catalogAsset.GetOrCreate(fileName);
                entry.partType = partCode;   // HD/BD/LG/BP/SH 저장
                entry.prefab = prefab;
                entry.fbx = model;
                entry.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPngAssetPath);

                EditorUtility.SetDirty(catalogAsset);

                // 처리 이력 갱신
                processedDb.Upsert(guid, modelPath, sig, ticks);
                EditorUtility.SetDirty(processedDb);

                processed++;
            }
        }



        finally
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"드롭 처리 완료: {processed}개 처리, {skipped}개 스킵");
    }

    // ======= Naming Rule =======
    (string partCode, string folderName) DetectPartFromFileName(string fileName)
    {
        // 기대 형식: Chr_001_HD_001
        var tokens = fileName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 3)
        {
            var code = tokens[2].ToUpperInvariant();
            if (CodeToFolder.TryGetValue(code, out var folder))
                return (code, folder);
        }
        return ("MISC", "Misc");
    }

    // ======= Prefab Create/Update =======
    GameObject CreateOrUpdatePrefabFromModel(GameObject model, string prefabAssetPath)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (inst == null) return null;

        try
        {
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            var saved = PrefabUtility.SaveAsPrefabAsset(inst, prefabAssetPath, out bool success);
            return (success && saved != null) ? saved : null;
        }
        finally
        {
            DestroyImmediate(inst);
        }
    }

    // ======= Folders =======
    string EnsureSubFolder(string rootAssetFolder, string subName)
    {
        string subPath = $"{rootAssetFolder}/{subName}";
        if (!AssetDatabase.IsValidFolder(subPath))
            AssetDatabase.CreateFolder(rootAssetFolder, subName);
        return subPath;
    }

    // ======= Processed DB (skip if already done) =======
    string ComputeFbxSignature(string assetPath)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        string fullPath = ToFullPath(assetPath);
        long ticks = File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath).Ticks : 0;

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        string importerKey = "";
        if (importer != null)
        {
            importerKey = $"{importer.globalScale}|{importer.importNormals}|{importer.animationType}|{importer.avatarSetup}";
        }

        return $"{guid}|{ticks}|{importerKey}";
    }

    bool ShouldProcess(string assetPath, out string guid, out string signature, out long ticks)
    {
        guid = AssetDatabase.AssetPathToGUID(assetPath);
        signature = ComputeFbxSignature(assetPath);

        string fullPath = ToFullPath(assetPath);
        ticks = File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath).Ticks : 0;

        if (!processedDb.TryGet(guid, out var item))
            return true;

        return item.hash != signature;
    }

    // ======= Paths =======
    string ToFullPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName.Replace("\\", "/");
        return $"{projectRoot}/{assetPath}".Replace("\\", "/");
    }

    // ======= Prefs =======
    void LoadPrefs()
    {
        prefabRootFolder = LoadFolder(PREF_PREFAB_ROOT);
        iconRootFolder = LoadFolder(PREF_ICON_ROOT);

        var cpath = EditorPrefs.GetString(PREF_CONFIG_PATH, "");
        if (!string.IsNullOrEmpty(cpath))
            catalogAsset = AssetDatabase.LoadAssetAtPath<PartCatalog>(cpath);

        var dbpath = EditorPrefs.GetString(PREF_DB_PATH, "");
        if (!string.IsNullOrEmpty(dbpath))
            processedDb = AssetDatabase.LoadAssetAtPath<ProcessedFbxDb>(dbpath);
    }

    void SavePrefs()
    {
        SaveFolder(PREF_PREFAB_ROOT, prefabRootFolder);
        SaveFolder(PREF_ICON_ROOT, iconRootFolder);

        if (catalogAsset != null)
            EditorPrefs.SetString(PREF_CONFIG_PATH, AssetDatabase.GetAssetPath(catalogAsset));

        if (processedDb != null)
            EditorPrefs.SetString(PREF_DB_PATH, AssetDatabase.GetAssetPath(processedDb));
    }

    DefaultAsset LoadFolder(string key)
    {
        var path = EditorPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
    }

    void SaveFolder(string key, DefaultAsset folder)
    {
        if (folder == null) return;
        var path = AssetDatabase.GetAssetPath(folder);
        if (AssetDatabase.IsValidFolder(path))
            EditorPrefs.SetString(key, path);
    }


    // ===================== [PATCH] ClothConfig Auto Create/Update =====================
    static void EnsurePngImportedAsSprite(string pngPath)
    {
        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (!importer) return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
        if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (changed) importer.SaveAndReimport();
    }

    static Game.Config.ClothElementType GuessClothTypeFromName(string fileNameNoExt)
    {
        var n = fileNameNoExt.ToLowerInvariant();
        if (n.Contains("helmet") || n.Contains("_hm_") || n.EndsWith("_hm")) return Game.Config.ClothElementType.Helmet;
        if (n.Contains("vest") || n.Contains("_vs_") || n.EndsWith("_vs")) return Game.Config.ClothElementType.Vest;
        if (n.Contains("uniform") || n.Contains("_un_") || n.EndsWith("_un")) return Game.Config.ClothElementType.Uniform;
        if (n.Contains("gloves") || n.Contains("_gv_") || n.EndsWith("_gv")) return Game.Config.ClothElementType.Gloves;
        if (n.Contains("shoes") || n.Contains("_sh_") || n.EndsWith("_sh")) return Game.Config.ClothElementType.Shoes;
        return Game.Config.ClothElementType.Uniform;
    }

    static Mesh ExtractMeshFromModelAsset(GameObject modelAssetRoot)
    {
        if (!modelAssetRoot) return null;

        var smr = modelAssetRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr && smr.sharedMesh) return smr.sharedMesh;

        var mf = modelAssetRoot.GetComponentInChildren<MeshFilter>(true);
        if (mf && mf.sharedMesh) return mf.sharedMesh;

        return null;
    }

    static void TrySetIconOnBaseEquipmentConfig(ScriptableObject cfg, Sprite sprite)
    {
        if (!cfg || !sprite) return;

        var t = cfg.GetType();
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        string[] candidates = { "Icon", "icon", "IconSprite", "iconSprite", "Sprite", "sprite" };

        foreach (var name in candidates)
        {
            var f = t.GetField(name, flags);
            if (f != null && typeof(Sprite).IsAssignableFrom(f.FieldType))
            {
                f.SetValue(cfg, sprite);
                return;
            }

            var p = t.GetProperty(name, flags);
            if (p != null && p.CanWrite && typeof(Sprite).IsAssignableFrom(p.PropertyType))
            {
                p.SetValue(cfg, sprite);
                return;
            }
        }
    }

    static Game.Config.ClothConfig CreateOrUpdateClothConfigAsset(
        string clothConfigAssetPath,
        string fileNameNoExt,
        string prefabPath,
        string iconPngPath,
        string modelFbxPath
    )
    {
        // 1) load or create
        var cfg = AssetDatabase.LoadAssetAtPath<Game.Config.ClothConfig>(clothConfigAssetPath);
        if (!cfg)
        {
            cfg = ScriptableObject.CreateInstance<Game.Config.ClothConfig>();
            AssetDatabase.CreateAsset(cfg, clothConfigAssetPath);
        }

        // 2) fill
        cfg.ClothType = GuessClothTypeFromName(fileNameNoExt);

        if (!string.IsNullOrEmpty(prefabPath))
            cfg.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        var modelRoot = AssetDatabase.LoadAssetAtPath<GameObject>(modelFbxPath);
        cfg.Mesh = ExtractMeshFromModelAsset(modelRoot);

        if (!string.IsNullOrEmpty(iconPngPath))
        {
            EnsurePngImportedAsSprite(iconPngPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPngPath);
            TrySetIconOnBaseEquipmentConfig(cfg, sprite); // EquipmentConfig 쪽 Icon 필드 자동 주입
        }

        EditorUtility.SetDirty(cfg);
        return cfg;
    }
    // ===================== [PATCH END] =====================

}
