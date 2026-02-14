#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SelectionSanitizer
    {
        static SelectionSanitizer()
        {
            EditorApplication.delayCall += RemoveMissingReferencesFromSelection;
            Selection.selectionChanged += RemoveMissingReferencesFromSelection;
        }

        private static void RemoveMissingReferencesFromSelection()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
                return;

            var validSelection = selectedObjects.Where(obj => obj != null).ToArray();
            if (validSelection.Length == selectedObjects.Length)
                return;

            Selection.objects = validSelection;
            Debug.LogWarning("Removed missing object references from the current Unity selection to prevent inspector errors.");
        }
    }
}
#endif
