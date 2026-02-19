using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object; // ✅ 이 줄 추가 (Object 모호성 방지)

public static class IconCaptureUtility
{
    public struct CaptureSettings
    {
        public int resolution;
        public bool transparentBackground;

        public Vector3 cameraEuler;
        public bool autoFrameByBounds;
        public float boundsPadding;
        public float cameraDistance;

        public float keyIntensity;
        public Vector3 keyEuler;

        public bool useFillLight;
        public float fillIntensity;
        public Vector3 fillEuler;

        public bool useRimLight;
        public float rimIntensity;
        public Vector3 rimEuler;
    }

    // ✅ 기존 3개 인수 버전은 그대로 유지
    public static bool CapturePrefabToPng(GameObject prefab, string outputPngPath, CaptureSettings s)
    {
        return CapturePrefabToPng(prefab, outputPngPath, null, s); // ✅ 내부에서 4개 버전으로 통일 호출
    }

    // ✅ 추가: 4개 인수 버전 (partCode 받음)
    public static bool CapturePrefabToPng(GameObject prefab, string outputPngPath, string partCode, CaptureSettings s)
    {
        if (prefab == null) return false;

        EnsureParentFolderExists(outputPngPath);

        var root = new GameObject("__ICON_CAPTURE_ROOT__");
        root.hideFlags = HideFlags.HideAndDontSave;

        var camGO = new GameObject("__ICON_CAMERA__");
        camGO.hideFlags = HideFlags.HideAndDontSave;
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = s.transparentBackground ? new Color(0, 0, 0, 0) : new Color(0.15f, 0.15f, 0.15f, 1);
        cam.orthographic = false;
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 200f;

        GameObject key = null;
        GameObject fill = null;
        GameObject rim = null;

        var rt = new RenderTexture(s.resolution, s.resolution, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;
        rt.Create();
        cam.targetTexture = rt;

        var tex = new Texture2D(s.resolution, s.resolution, TextureFormat.RGBA32, false, true);

        GameObject inst = null;

        try
        {
            // Instantiate prefab
            inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (inst == null) return false;
            inst.hideFlags = HideFlags.HideAndDontSave;
            inst.transform.SetParent(root.transform, false);
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;

            // ✅ BP면 캡처용 인스턴스만 Y 180 회전
            if (!string.IsNullOrEmpty(partCode) && partCode.Equals("BP", StringComparison.OrdinalIgnoreCase))
                inst.transform.Rotate(0f, 180f, 0f);

            if (!TryGetBounds(inst, out var b))
                return false;

            // Lights
            key = CreateLight("__KEY__", s.keyIntensity, s.keyEuler, root.transform);
            if (s.useFillLight) fill = CreateLight("__FILL__", s.fillIntensity, s.fillEuler, root.transform);
            if (s.useRimLight) rim = CreateLight("__RIM__", s.rimIntensity, s.rimEuler, root.transform);

            // Camera placement
            camGO.transform.rotation = Quaternion.Euler(s.cameraEuler);
            var center = b.center;
            var forward = camGO.transform.forward;

            if (s.autoFrameByBounds)
            {
                float fitDistance = CalculateFitDistanceForBounds(b, center, camGO.transform.rotation, cam.fieldOfView, cam.aspect, cam.nearClipPlane);
                float distanceScale = Mathf.Max(0.01f, s.boundsPadding);
                camGO.transform.position = center - forward * (fitDistance * distanceScale);
            }
            else
            {
                camGO.transform.position = center - forward * Mathf.Max(0.01f, s.cameraDistance);
            }

            camGO.transform.LookAt(center);

            // Render
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, s.resolution, s.resolution), 0, 0, false);
            tex.Apply(false, false);
            RenderTexture.active = null;

            // Save PNG
            var png = tex.EncodeToPNG();
            File.WriteAllBytes(outputPngPath, png);

            return true;
        }
        finally
        {
            cam.targetTexture = null;
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
            if (tex != null) Object.DestroyImmediate(tex);

            if (inst != null) Object.DestroyImmediate(inst);

            if (key) Object.DestroyImmediate(key);
            if (fill) Object.DestroyImmediate(fill);
            if (rim) Object.DestroyImmediate(rim);

            Object.DestroyImmediate(camGO);
            Object.DestroyImmediate(root);
        }
    }

    public static void ImportPngAsSprite(string assetPath)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return;

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.sRGBTexture = true;
        ti.SaveAndReimport();
    }

    static GameObject CreateLight(string name, float intensity, Vector3 euler, Transform parent)
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

    static float CalculateFitDistanceForBounds(Bounds bounds, Vector3 center, Quaternion cameraRotation, float verticalFovDeg, float aspect, float nearClip)
    {
        var corners = GetBoundsCorners(bounds);
        var invRot = Quaternion.Inverse(cameraRotation);

        float tanHalfV = Mathf.Tan(verticalFovDeg * Mathf.Deg2Rad * 0.5f);
        float tanHalfH = tanHalfV * Mathf.Max(0.0001f, aspect);

        float requiredDistance = 0f;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = invRot * (corners[i] - center);

            float reqByX = Mathf.Abs(local.x) / Mathf.Max(0.0001f, tanHalfH) - local.z;
            float reqByY = Mathf.Abs(local.y) / Mathf.Max(0.0001f, tanHalfV) - local.z;
            float reqByNear = nearClip - local.z;

            requiredDistance = Mathf.Max(requiredDistance, reqByX, reqByY, reqByNear);
        }

        return Mathf.Max(requiredDistance, nearClip + 0.01f);
    }

    static Vector3[] GetBoundsCorners(Bounds b)
    {
        var min = b.min;
        var max = b.max;

        return new[]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
        };
    }

    static bool TryGetBounds(GameObject go, out Bounds bounds)
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

    static void EnsureParentFolderExists(string fullPath)
    {
        var dir = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(dir)) return;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }
}
