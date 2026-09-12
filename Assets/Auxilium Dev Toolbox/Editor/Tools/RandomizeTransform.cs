using UnityEditor;
using UnityEngine;

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Nudges the selected objects by random amounts so scattered props stop looking stamped from a
    /// mould. Give it ranges for rotation and scale, and a little position jitter, and it does the
    /// rest with one undo step.
    /// </summary>
    public class RandomizeTransform : EditorWindow
    {
        private bool m_RotateY = true;
        private bool m_RotateAll;
        private Vector2 m_ScaleRange = new Vector2(0.9f, 1.1f);
        private bool m_UniformScale = true;
        private float m_PositionJitter;

        [MenuItem("Tools/Auxilium/Dev Toolbox/Randomize Transform")]
        public static void Open()
        {
            var window = GetWindow<RandomizeTransform>(false, "Randomize", true);
            window.minSize = new Vector2(320f, 260f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("🎲", "Randomize Transform", ToolboxUI.Object);

            int count = Selection.transforms.Length;
            EditorGUILayout.HelpBox(count == 0 ? "Select some objects to scatter." : $"{count} object(s) selected.",
                count == 0 ? MessageType.Warning : MessageType.Info);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
            this.m_RotateY = EditorGUILayout.ToggleLeft("Spin around Y (upright props)", this.m_RotateY);
            this.m_RotateAll = EditorGUILayout.ToggleLeft("Tumble on all axes (debris, rocks)", this.m_RotateAll);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
            this.m_UniformScale = EditorGUILayout.ToggleLeft("Keep scale uniform", this.m_UniformScale);
            EditorGUILayout.MinMaxSlider(
                new GUIContent($"Range  {this.m_ScaleRange.x:0.00} to {this.m_ScaleRange.y:0.00}"),
                ref this.m_ScaleRange.x, ref this.m_ScaleRange.y, 0.1f, 3f);

            EditorGUILayout.Space(4f);
            this.m_PositionJitter = EditorGUILayout.Slider("Position Jitter", this.m_PositionJitter, 0f, 5f);

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(count == 0))
            {
                if (GUILayout.Button("Randomize", GUILayout.Height(26f))) this.Apply();
            }
        }

        private void Apply()
        {
            Transform[] transforms = Selection.transforms;
            Undo.RecordObjects(transforms, "Randomize Transform");

            foreach (Transform t in transforms)
            {
                if (this.m_RotateAll)
                {
                    t.localRotation = Random.rotationUniform;
                }
                else if (this.m_RotateY)
                {
                    Vector3 e = t.localEulerAngles;
                    e.y = Random.Range(0f, 360f);
                    t.localEulerAngles = e;
                }

                if (this.m_UniformScale)
                {
                    float s = Random.Range(this.m_ScaleRange.x, this.m_ScaleRange.y);
                    t.localScale = new Vector3(s, s, s);
                }
                else
                {
                    t.localScale = new Vector3(
                        Random.Range(this.m_ScaleRange.x, this.m_ScaleRange.y),
                        Random.Range(this.m_ScaleRange.x, this.m_ScaleRange.y),
                        Random.Range(this.m_ScaleRange.x, this.m_ScaleRange.y));
                }

                if (this.m_PositionJitter > 0f)
                {
                    Vector2 offset = Random.insideUnitCircle * this.m_PositionJitter;
                    t.position += new Vector3(offset.x, 0f, offset.y);
                }
            }
        }
    }
}
