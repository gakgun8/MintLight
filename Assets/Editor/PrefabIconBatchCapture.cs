using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PrefabIconBatchCapture : EditorWindow
{
    [Header("Input")]
    private DefaultAsset prefabFolder;

    [Header("Output")]
    private DefaultAsset outputFolder;
    private int resolution = 512;
    private bool transparentBackground = true;

    [Header("Camera / Framing")]
    private Vector3 cameraEuler = new Vector3(20f, 35f, 0f);
    private float cameraDistance = 2.5f;          // 기본 거리(오토프레이밍 끄면 사용)
    private bool autoFrameByBounds = true;
    private float boundsPadding = 1.15f;

    [Header("Lighting")]
    private float keyIntensity = 2.0f;
    private Vector3 keyEuler = new Vector3(50f, -30f, 0f);
    private float fillIntensity = 0.8f;
    private Vector3 fillEuler = new Vector3(15f, 120f, 0f);
    private float rimIntensity = 1.2f;
    private Vector3 rimEuler = new Vector3(70f, 160f, 0f);

    [MenuItem("Tools/Icon Capture/Prefab Folder Batch Capture")]
    public static void Open()
    {
        GetWindow<PrefabIconBatchCapture>("Prefab Icon Capture");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab Folder", EditorStyles.boldLabel);
        prefabFolder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", prefabFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
        resolution = EditorGUILayout.IntPopup("Resolution", resolution, new[] { "256", "512", "1024", "2048" }, new[] { 256, 512, 1024, 2048 });
        transparentBackground = EditorGUILayout.Toggle("Transparent BG (PNG)", transparentBackground);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Camera / Framing", EditorStyles.boldLabel);
        cameraEuler = EditorGUILayout.Vector3Field("Camera Euler", cameraEuler);
        autoFrameByBounds = EditorGUILayout.Toggle("Auto Frame By Bounds", autoFrameByBounds);
        if (autoFrameByBounds)
        {
            boundsPadding = EditorGUILayout.Slider("Bounds Padding", boundsPadding, 1.0f, 2.0f);
        }
        else
        {
            cameraDistance = EditorGUILayout.FloatField("Camera Distance", cameraDistance);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
        keyIntensity = EditorGUILayout.Slider("Key Intensity", keyIntensity, 0f, 8f);
        keyEuler = EditorGUILayout.Vector3Field("Key Euler", keyEuler);
        fillIntensity = EditorGUILayout.Slider("Fill Intensity", fillIntensity, 0f, 8f);
        fillEuler = EditorGUILayout.Vector3Field("Fill Euler", fillEuler);
        rimIntensity = EditorGUILayout.Slider("Rim Intensity", rimIntensity, 0f, 8f);
        rimEuler = EditorGUILayout.Vector3Field("Rim Euler", rimEuler);

        EditorGUILayout.Space(12);

        using (new EditorGUI.DisabledScope(prefabFolder == null || outputFolder == null))
        {
            if (GUILayout.Button("Capture All Prefabs"))
            {
                CaptureAll();
            }
        }

        EditorGUILayout.HelpBox(
            "Tips:\n" +
            "- 프리팹 크기가 제각각이면 Auto Frame By Bounds를 켜세요.\n" +
            "- 머티리얼/쉐이더가 URP/HDRP면 프로젝트 렌더파이프라인에 맞게 라이트만 조정하면 됩니다.\n" +
            "- 결과 파일명: PrefabName.png",
            MessageType.Info
        );
    }

    private void CaptureAll()
    {
        string prefabFolderPath = AssetDatabase.GetAssetPath(prefabFolder);
        string outputFolderPath = AssetDatabase.GetAssetPath(outputFolder);

        if (!AssetDatabase.IsValidFolder(prefabFolderPath) || !AssetDatabase.IsValidFolder(outputFolderPath))
        {
            Debug.LogError("폴더 경로가 올바르지 않습니다.");
            return;
        }

        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
        var prefabPaths = prefabGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        if (prefabPaths.Length == 0)
        {
            Debug.LogWarning("해당 폴더에서 Prefab을 찾지 못했습니다.");
            return;
        }

        // 임시 스테이지(씬) 오브젝트들
        var root = new GameObject("__ICON_CAPTURE_ROOT__");
        root.hideFlags = HideFlags.HideAndDontSave;

        var camGO = new GameObject("__ICON_CAMERA__");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = transparentBackground ? new Color(0, 0, 0, 0) : new Color(0.15f, 0.15f, 0.15f, 1);
        cam.orthographic = false;
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;

        // 라이트 3점 세팅
        var key = CreateLight("__KEY__", keyIntensity, keyEuler, root.transform);
        var fill = CreateLight("__FILL__", fillIntensity, fillEuler, root.transform);
        var rim = CreateLight("__RIM__", rimIntensity, rimEuler, root.transform);

        // RenderTexture 준비
        var rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        rt.Create();
        cam.targetTexture = rt;

        // 알파 저장을 위해
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);

        try
        {
            int count = 0;
            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // 인스턴스 생성
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.hideFlags = HideFlags.HideAndDontSave;
                inst.transform.SetParent(root.transform, false);
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;

                // Bounds 계산(모든 Renderer 포함)
                if (!TryGetBounds(inst, out var b))
                {
                    Debug.LogWarning($"Renderer Bounds를 찾지 못함: {prefab.name}");
                    Object.DestroyImmediate(inst);
                    continue;
                }

                // 카메라 세팅
                camGO.transform.rotation = Quaternion.Euler(cameraEuler);

                Vector3 forward = camGO.transform.forward;
                Vector3 center = b.center;

                if (autoFrameByBounds)
                {
                    // bounds 크기에 따라 거리 자동 계산
                    // 구의 반지름 기반으로 FOV에 맞춰 거리 산출
                    float radius = b.extents.magnitude * boundsPadding;
                    float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
                    float dist = radius / Mathf.Sin(fovRad * 0.5f);
                    camGO.transform.position = center - forward * dist;
                }
                else
                {
                    camGO.transform.position = center - forward * cameraDistance;
                }

                camGO.transform.LookAt(center);

                // 렌더
                cam.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0, false);
                tex.Apply(false, false);
                RenderTexture.active = null;

                // 저장
                byte[] png = tex.EncodeToPNG();
                string outPath = Path.Combine(outputFolderPath, $"{prefab.name}.png").Replace("\\", "/");
                File.WriteAllBytes(outPath, png);

                count++;

                Object.DestroyImmediate(inst);
            }

            AssetDatabase.Refresh();
            Debug.Log($"아이콘 캡처 완료: {count}개 저장됨 → {outputFolderPath}");
        }
        finally
        {
            // 정리
            if (rt != null)
            {
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(key);
            Object.DestroyImmediate(fill);
            Object.DestroyImmediate(rim);
            Object.DestroyImmediate(camGO);
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateLight(string name, float intensity, Vector3 euler, Transform parent)
    {
        var go = new GameObject(name);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(parent, false);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        go.transform.rotation = Quaternion.Euler(euler);
        return go;
    }

    private static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }
}
