using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Copies one object's position, rotation and scale and pastes it onto others. The fast way to
    /// line one object up exactly with another, or stamp the same pose across a group.
    /// </summary>
    public static class CopyPasteTransform
    {
        private static bool s_Has;
        private static Vector3 s_Position;
        private static Quaternion s_Rotation;
        private static Vector3 s_Scale;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Transform/Copy World Transform")]
        public static void Copy()
        {
            Transform t = Selection.activeTransform;
            if (t == null) return;

            s_Position = t.position;
            s_Rotation = t.rotation;
            s_Scale = t.lossyScale;
            s_Has = true;
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Transform/Paste World Transform")]
        public static void Paste()
        {
            if (!s_Has) return;

            Transform[] transforms = Selection.transforms;
            if (transforms.Length == 0) return;

            Undo.RecordObjects(transforms, "Paste Transform");
            foreach (Transform t in transforms)
            {
                t.SetPositionAndRotation(s_Position, s_Rotation);
                // lossyScale cannot be set directly, so back out the parent scale to hit it.
                if (t.parent == null)
                {
                    t.localScale = s_Scale;
                }
                else
                {
                    Vector3 parent = t.parent.lossyScale;
                    t.localScale = new Vector3(
                        parent.x != 0f ? s_Scale.x / parent.x : s_Scale.x,
                        parent.y != 0f ? s_Scale.y / parent.y : s_Scale.y,
                        parent.z != 0f ? s_Scale.z / parent.z : s_Scale.z);
                }
            }
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Transform/Copy World Transform", true)]
        private static bool ValidateCopy() => Selection.activeTransform != null;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Transform/Paste World Transform", true)]
        private static bool ValidatePaste() => s_Has && Selection.transforms.Length > 0;
    }
}
