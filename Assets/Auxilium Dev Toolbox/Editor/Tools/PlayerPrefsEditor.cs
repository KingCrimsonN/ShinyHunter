using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

namespace Auxilium.DevToolbox
{
    /// <summary>
    /// Lists, edits, adds and deletes the project's PlayerPrefs from one window, so you stop writing
    /// throwaway code just to peek at a saved value. Listing reads the Windows registry, so on other
    /// systems you can still add, edit and delete by key.
    /// </summary>
    /// <remarks>
    /// Unity gives no way to enumerate PlayerPrefs, so the listing reads them straight from where
    /// the editor stores them in the Windows registry, then edits go back through the normal
    /// PlayerPrefs calls so the values stay correct.
    /// </remarks>
    public class PlayerPrefsEditor : EditorWindow
    {
        private enum PrefType
        {
            Int,
            Float,
            String
        }

        private struct Pref
        {
            public string Key;
            public PrefType Type;
            public string Value;
        }

        private readonly List<Pref> m_Prefs = new List<Pref>();
        private Vector2 m_Scroll;
        private string m_Filter = string.Empty;

        private string m_NewKey = string.Empty;
        private string m_NewValue = string.Empty;
        private PrefType m_NewType = PrefType.String;

        [MenuItem("Tools/Auxilium/Dev Toolbox/PlayerPrefs Editor")]
        public static void Open()
        {
            var window = GetWindow<PlayerPrefsEditor>(false, "PlayerPrefs", true);
            window.minSize = new Vector2(420f, 360f);
            window.Refresh();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4f);
            ToolboxUI.Header("🔧", "PlayerPrefs Editor", ToolboxUI.Data);

            EditorGUILayout.BeginHorizontal();
            this.m_Filter = EditorGUILayout.TextField("Filter", this.m_Filter);
            if (GUILayout.Button("Refresh", GUILayout.Width(70f))) this.Refresh();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            this.DrawList();

            EditorGUILayout.Space(6f);
            this.DrawAdd();

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(this.m_Prefs.Count == 0))
            {
                if (GUILayout.Button("Delete All PlayerPrefs", GUILayout.Height(22f)) && Confirm("Delete every PlayerPref?"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    this.Refresh();
                }
            }
        }

        private void DrawList()
        {
            this.m_Scroll = EditorGUILayout.BeginScrollView(this.m_Scroll, GUILayout.MinHeight(160f));

            for (int i = 0; i < this.m_Prefs.Count; i++)
            {
                Pref pref = this.m_Prefs[i];
                if (!string.IsNullOrEmpty(this.m_Filter) && pref.Key.IndexOf(this.m_Filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{pref.Key}  ({pref.Type})", GUILayout.MinWidth(120f));

                string edited = EditorGUILayout.TextField(pref.Value, GUILayout.MinWidth(80f));
                if (edited != pref.Value)
                {
                    this.Write(pref.Key, pref.Type, edited);
                    pref.Value = edited;
                    this.m_Prefs[i] = pref;
                }

                if (GUILayout.Button("X", GUILayout.Width(22f)))
                {
                    PlayerPrefs.DeleteKey(pref.Key);
                    PlayerPrefs.Save();
                    this.Refresh();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (this.m_Prefs.Count == 0)
            {
#if UNITY_EDITOR_WIN
                EditorGUILayout.HelpBox("No PlayerPrefs found for this project yet.", MessageType.Info);
#else
                EditorGUILayout.HelpBox("Listing PlayerPrefs is Windows only. You can still add, edit and delete by key below.", MessageType.Info);
#endif
            }
        }

        private void DrawAdd()
        {
            EditorGUILayout.LabelField("Add Or Set A Key", EditorStyles.boldLabel);
            this.m_NewKey = EditorGUILayout.TextField("Key", this.m_NewKey);
            this.m_NewType = (PrefType) EditorGUILayout.EnumPopup("Type", this.m_NewType);
            this.m_NewValue = EditorGUILayout.TextField("Value", this.m_NewValue);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(this.m_NewKey)))
            {
                if (GUILayout.Button("Set", GUILayout.Height(22f)))
                {
                    this.Write(this.m_NewKey, this.m_NewType, this.m_NewValue);
                    this.m_NewKey = string.Empty;
                    this.m_NewValue = string.Empty;
                    this.Refresh();
                }
            }
        }

        private void Write(string key, PrefType type, string value)
        {
            switch (type)
            {
                case PrefType.Int:
                    int.TryParse(value, out int i);
                    PlayerPrefs.SetInt(key, i);
                    break;
                case PrefType.Float:
                    float.TryParse(value, out float f);
                    PlayerPrefs.SetFloat(key, f);
                    break;
                default:
                    PlayerPrefs.SetString(key, value);
                    break;
            }

            PlayerPrefs.Save();
        }

        private static bool Confirm(string message)
        {
            return EditorUtility.DisplayDialog("PlayerPrefs Editor", message, "Yes", "Cancel");
        }

        private void Refresh()
        {
            this.m_Prefs.Clear();

#if UNITY_EDITOR_WIN
            string path = $"Software\\Unity\\UnityEditor\\{PlayerSettings.companyName}\\{PlayerSettings.productName}";
            using RegistryKey rk = Registry.CurrentUser.OpenSubKey(path);
            if (rk == null) return;

            foreach (string valueName in rk.GetValueNames())
            {
                string key = StripHash(valueName);
                object raw = rk.GetValue(valueName);
                RegistryValueKind kind = rk.GetValueKind(valueName);

                Pref pref = new Pref { Key = key };

                if (kind == RegistryValueKind.DWord)
                {
                    pref.Type = PrefType.Int;
                    pref.Value = System.Convert.ToInt32(raw).ToString();
                }
                else if (raw is byte[] bytes)
                {
                    if (bytes.Length == 4 && !AllPrintable(bytes))
                    {
                        pref.Type = PrefType.Float;
                        pref.Value = System.BitConverter.ToSingle(bytes, 0).ToString();
                    }
                    else
                    {
                        pref.Type = PrefType.String;
                        pref.Value = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
                    }
                }
                else
                {
                    pref.Type = PrefType.String;
                    pref.Value = raw != null ? raw.ToString() : string.Empty;
                }

                this.m_Prefs.Add(pref);
            }

            this.m_Prefs.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
#endif
            this.Repaint();
        }

        private static string StripHash(string valueName)
        {
            int h = valueName.LastIndexOf("_h", System.StringComparison.Ordinal);
            return h > 0 ? valueName.Substring(0, h) : valueName;
        }

        private static bool AllPrintable(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                if (b != 0 && (b < 32 || b > 126)) return false;
            }
            return true;
        }
    }
}
