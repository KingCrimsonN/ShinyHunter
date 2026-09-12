using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Drops the selected objects straight down onto whatever collider is beneath them, so scattering
    /// props on uneven ground stops being a manual chore. Optionally aligns each object to the
    /// surface it lands on.
    /// </summary>
    public static class SnapToGround
    {
        [MenuItem("Tools/Auxilium/Dev Toolbox/Snap To Ground _F8")]
        public static void Snap()
        {
            SnapObjects(false);
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Snap To Ground And Align")]
        public static void SnapAndAlign()
        {
            SnapObjects(true);
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Snap To Ground _F8", true)]
        [MenuItem("Tools/Auxilium/Dev Toolbox/Snap To Ground And Align", true)]
        private static bool Validate() => Selection.transforms.Length > 0;

        private static void SnapObjects(bool align)
        {
            Transform[] transforms = Selection.transforms;
            if (transforms.Length == 0) return;

            Undo.RecordObjects(transforms, "Snap To Ground");
            int landed = 0;

            foreach (Transform t in transforms)
            {
                // Start a little above the object so it can find ground that is slightly higher
                // than the pivot, and ignore the object's own colliders.
                Vector3 origin = t.position + Vector3.up * 0.5f;

                RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 5000f);
                float bestDistance = float.MaxValue;
                RaycastHit best = default;
                bool found = false;

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.transform == t || hit.collider.transform.IsChildOf(t)) continue;
                    if (hit.distance >= bestDistance) continue;

                    bestDistance = hit.distance;
                    best = hit;
                    found = true;
                }

                if (!found) continue;

                t.position = best.point;
                if (align) t.up = best.normal;
                landed++;
            }

            if (landed == 0)
            {
                Debug.LogWarning("[Auxilium Dev Toolbox] Snap To Ground found no colliders under the selection. The ground needs a collider.");
            }
        }
    }
}
