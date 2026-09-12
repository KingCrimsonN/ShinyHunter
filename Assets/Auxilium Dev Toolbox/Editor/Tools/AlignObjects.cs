using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Lines the selected objects up on an axis, or spreads them out evenly. The quick way to tidy a
    /// row of props, a set of waypoints, or a stack of UI markers.
    /// </summary>
    public class AlignObjects : EditorWindow
    {
        private enum Edge
        {
            Min,
            Center,
            Max
        }

        [MenuItem("Tools/Auxilium/Dev Toolbox/Align Objects")]
        public static void Open()
        {
            var window = GetWindow<AlignObjects>(false, "Align", true);
            window.minSize = new Vector2(320f, 240f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("📐", "Align Objects", ToolboxUI.Object);

            int count = Selection.transforms.Length;
            EditorGUILayout.HelpBox(count < 2 ? "Select two or more objects." : $"{count} object(s) selected.",
                count < 2 ? MessageType.Warning : MessageType.Info);

            using (new EditorGUI.DisabledScope(count < 2))
            {
                EditorGUILayout.Space(4f);
                this.AxisRow("X", 0);
                this.AxisRow("Y", 1);
                this.AxisRow("Z", 2);

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Distribute Evenly", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Along X")) this.Distribute(0);
                if (GUILayout.Button("Along Y")) this.Distribute(1);
                if (GUILayout.Button("Along Z")) this.Distribute(2);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AxisRow(string label, int axis)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Align {label}", GUILayout.Width(70f));
            if (GUILayout.Button("Min")) this.Align(axis, Edge.Min);
            if (GUILayout.Button("Center")) this.Align(axis, Edge.Center);
            if (GUILayout.Button("Max")) this.Align(axis, Edge.Max);
            EditorGUILayout.EndHorizontal();
        }

        private void Align(int axis, Edge edge)
        {
            Transform[] transforms = Selection.transforms;
            float min = float.MaxValue, max = float.MinValue, sum = 0f;

            foreach (Transform t in transforms)
            {
                float v = t.position[axis];
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
                sum += v;
            }

            float targetValue = edge switch
            {
                Edge.Min => min,
                Edge.Max => max,
                _ => sum / transforms.Length
            };

            Undo.RecordObjects(transforms, "Align Objects");
            foreach (Transform t in transforms)
            {
                Vector3 p = t.position;
                p[axis] = targetValue;
                t.position = p;
            }
        }

        private void Distribute(int axis)
        {
            Transform[] transforms = Selection.transforms;
            System.Array.Sort(transforms, (a, b) => a.position[axis].CompareTo(b.position[axis]));

            float start = transforms[0].position[axis];
            float end = transforms[transforms.Length - 1].position[axis];
            float step = (end - start) / (transforms.Length - 1);

            Undo.RecordObjects(transforms, "Distribute Objects");
            for (int i = 0; i < transforms.Length; i++)
            {
                Vector3 p = transforms[i].position;
                p[axis] = start + step * i;
                transforms[i].position = p;
            }
        }
    }
}
