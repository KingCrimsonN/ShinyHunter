using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Flips the selected objects between active and inactive with one shortcut, so you can hide and
    /// show parts of a scene while you work without reaching for the inspector checkbox.
    /// </summary>
    public static class ToggleActive
    {
        [MenuItem("Tools/Auxilium/Dev Toolbox/Toggle Active %#a")]
        public static void Toggle()
        {
            GameObject[] objects = Selection.gameObjects;
            if (objects.Length == 0) return;

            // Flip everything to the opposite of the first object, so a mixed selection lands in one
            // consistent state rather than each object toggling against itself.
            bool target = !objects[0].activeSelf;

            Undo.RecordObjects(objects, "Toggle Active");
            foreach (GameObject go in objects) go.SetActive(target);
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Toggle Active %#a", true)]
        private static bool Validate() => Selection.gameObjects.Length > 0;
    }
}
