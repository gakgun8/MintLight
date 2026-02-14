#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SelectionSanitizer
    {
        private const string MissingTargetsErrorMarker = "m_Targets of GameObjectInspector";

        static SelectionSanitizer()
        {
            EditorApplication.delayCall += SanitizeSelectionAndInspectors;
            Selection.selectionChanged += SanitizeSelectionAndInspectors;
            Application.logMessageReceived += OnEditorLogMessage;
        }

        private static void OnEditorLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error)
                return;

            if (!condition.Contains(MissingTargetsErrorMarker, StringComparison.Ordinal))
                return;

            EditorApplication.delayCall += RecoverFromBrokenInspectorTargets;
        }

        private static void SanitizeSelectionAndInspectors()
        {
            var sanitized = RemoveMissingReferencesFromSelection();
            var rebuilt = RebuildInspectorTrackersIfNeeded();

            if (sanitized || rebuilt)
            {
                InternalEditorUtilityRepaintAllViews();
            }
        }

        private static void RecoverFromBrokenInspectorTargets()
        {
            RemoveMissingReferencesFromSelection();
            RebuildAllInspectorTrackers();
            UnlockAllInspectors();
            InternalEditorUtilityRepaintAllViews();

            Debug.LogWarning("Recovered Unity Inspector from stale GameObjectInspector targets by rebuilding and unlocking inspector trackers.");
        }

        private static bool RemoveMissingReferencesFromSelection()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return false;

            var validSelection = selectedObjects.Where(obj => obj != null).ToArray();
            if (validSelection.Length == selectedObjects.Length)
                return false;

            Selection.objects = validSelection;
            Debug.LogWarning("Removed missing object references from the current Unity selection to prevent inspector errors.");
            return true;
        }

        private static bool RebuildInspectorTrackersIfNeeded()
        {
            var sharedTracker = ActiveEditorTracker.sharedTracker;
            if (!HasInvalidEditorTargets(sharedTracker))
                return false;

            sharedTracker.ForceRebuild();
            return true;
        }

        private static void RebuildAllInspectorTrackers()
        {
            ActiveEditorTracker.sharedTracker.ForceRebuild();

            foreach (var inspector in GetAllInspectorWindows())
            {
                var trackerProperty = inspector.GetType().GetProperty("tracker", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var tracker = trackerProperty?.GetValue(inspector) as ActiveEditorTracker;
                tracker?.ForceRebuild();
            }
        }

        private static bool HasInvalidEditorTargets(ActiveEditorTracker tracker)
        {
            if (tracker == null)
                return false;

            var editors = tracker.activeEditors;
            if (editors == null || editors.Length == 0)
                return false;

            foreach (var editor in editors)
            {
                if (editor == null)
                    return true;

                var targets = editor.targets;
                if (targets == null || targets.Any(target => target == null))
                    return true;
            }

            return false;
        }

        private static void UnlockAllInspectors()
        {
            foreach (var inspector in GetAllInspectorWindows())
            {
                var isLockedProperty = inspector.GetType().GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (isLockedProperty?.CanWrite == true)
                {
                    isLockedProperty.SetValue(inspector, false);
                }
            }
        }

        private static EditorWindow[] GetAllInspectorWindows()
        {
            var inspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorWindowType == null)
                return Array.Empty<EditorWindow>();

            return Resources.FindObjectsOfTypeAll(inspectorWindowType)
                .OfType<EditorWindow>()
                .ToArray();
        }

        private static void InternalEditorUtilityRepaintAllViews()
        {
            var internalEditorUtilityType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditorInternal.InternalEditorUtility");
            var repaintAllViewsMethod = internalEditorUtilityType?.GetMethod("RepaintAllViews", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            repaintAllViewsMethod?.Invoke(null, null);
        }
    }
}
#endif
