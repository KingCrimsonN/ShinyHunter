using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// The little transform chores you do a hundred times a day: group a selection under a fresh
    /// parent, and reset a transform back to zero. Both keep an undo step.
    /// </summary>
    public static class QuickTransformTools
    {
        [MenuItem("Tools/Auxilium/Dev Toolbox/Group Selected %#g")]
        public static void GroupSelected()
        {
            Transform[] transforms = Selection.transforms;
            if (transforms.Length == 0) return;

            Vector3 center = Vector3.zero;
            foreach (Transform t in transforms) center += t.position;
            center /= transforms.Length;

            var group = new GameObject("Group");
            Undo.RegisterCreatedObjectUndo(group, "Group Selected");
            group.transform.position = center;

            // Keep the group under whatever the first object's parent was, so it lands in the same
            // place in the hierarchy rather than at the root.
            Transform commonParent = transforms[0].parent;
            group.transform.SetParent(commonParent, true);

            foreach (Transform t in transforms)
            {
                Undo.SetTransformParent(t, group.transform, "Group Selected");
            }

            Selection.activeGameObject = group;
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Group Selected %#g", true)]
        private static bool ValidateGroup() => Selection.transforms.Length > 0;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Reset Transform")]
        public static void ResetTransform()
        {
            Transform[] transforms = Selection.transforms;
            if (transforms.Length == 0) return;

            Undo.RecordObjects(transforms, "Reset Transform");
            foreach (Transform t in transforms)
            {
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Reset Transform", true)]
        private static bool ValidateReset() => Selection.transforms.Length > 0;
    }
}
