// Copyright (c) 2025 PinePie. All rights reserved.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PinePie.PieTabs
{
    public static partial class Navigator
    {
        private static MethodInfo showFolderContents;
        private static MethodInfo EndRenamingMI;

        private static Type winType;
        private static Type entityIdType;
        public static List<WinInstanceInfo> windowsInfo;

        public static WinInstanceInfo GetFocusedWindow()
        {
            return windowsInfo.FirstOrDefault(w => w.editorWindow == EditorWindow.focusedWindow);
        }

        public static void ForceEndRenaming(EditorWindow window)
        {
            if (winType == null) winType = Type.GetType("UnityEditor.ProjectBrowser, UnityEditor");

            if (EndRenamingMI == null) EndRenamingMI = winType?.GetMethod("EndRenaming", BindingFlags.Instance | BindingFlags.Public);

            // if (window == null) window = GetWin();

            EndRenamingMI.Invoke(window, null);
        }

        // internal values fetcher
        private static float GetSideRectWidth(EditorWindow win)
        {
            if (win.GetType().ToString() == "UnityEditor.ProjectBrowser")
            {
                var type = win.GetType();
                FieldInfo Field = type.GetField("m_DirectoriesAreaWidth", BindingFlags.NonPublic | BindingFlags.Instance);

                if (Field != null)
                {
                    var rect = Field.GetValue(win);
                    return (float)rect;
                }
                else
                    return 0;
            }
            else
                return 0;
        }

        public static bool IsTwoColumnMode(EditorWindow win)
        {
            if (win.GetType().ToString() == "UnityEditor.ProjectBrowser")
            {
                var viewModeField = win.GetType().GetField("m_ViewMode", BindingFlags.Instance | BindingFlags.NonPublic);
                if (viewModeField != null)
                {
                    object viewModeValue = viewModeField.GetValue(win);
                    return viewModeValue.ToString() == "TwoColumns";
                }
            }

            return false;
        }

        public static List<VisualElement> GetProjectBrowserUIs()
        {
            List<VisualElement> result = new();

            var infos = GetWindowsInfo();

            foreach (var info in infos)
            {
                result.Add(info.ProjUI);
            }

            return result;
        }

        public static List<WinInstanceInfo> GetWindowsInfo()
        {
            List<WinInstanceInfo> result = new();

            var projectBrowsers = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            foreach (EditorWindow window in projectBrowsers.Cast<EditorWindow>())
            {
                if (window.GetType().ToString() == "UnityEditor.ProjectBrowser")
                {
                    result.Add(new()
                    {
                        editorWindow = window,
                        ProjUI = window.rootVisualElement
                    });
                }
            }

            windowsInfo = result;
            return result;
        }

        public static string GetActiveFolderPath(EditorWindow win)
        {
            MethodInfo method = win.GetType().GetMethod("GetActiveFolderPath", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null)
            {
                string result = method.Invoke(win, null) as string;
                if (!string.IsNullOrEmpty(result))
                    return result;
            }

            return "Assets/";
        }


        public static MethodInfo GetInternalMethod(Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static void OpenFolder(string folderPath, EditorWindow win)
        {
            if (winType == null) winType = win.GetType();

            Object obj = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (obj == null)
                return;

            if (showFolderContents == null)
                showFolderContents = GetInternalMethod(winType, "ShowFolderContents");

            if (showFolderContents != null)
            {
                var paramType = showFolderContents.GetParameters()[0].ParameterType;

#if UNITY_6000_0_OR_NEWER
                if (paramType.Name.Contains("EntityId"))
                {
                    showFolderContents.Invoke(win, new object[] { obj.GetEntityId(), true });
                    return;
                }
#endif

                if (paramType == typeof(int))
                {
#pragma warning disable CS0618

                    showFolderContents.Invoke(win, new object[] { obj.GetInstanceID(), true });
#pragma warning restore CS0618
                    return;
                }
            }

            AssetDatabase.OpenAsset(obj);
        }

        public static void FocusAssetByObj(Object asset, EditorWindow win)
        {
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorUtility.FocusProjectWindow();
                win.Focus();
            }
        }

        public static List<string> CleanEntries(List<string> items)
        {
            // HashSet for fast lookup
            HashSet<string> set = new(items);

            List<string> output = new();

            foreach (var item in items)
            {
                bool isParent = items.Any(child =>
                    child != item &&
                    child.StartsWith(item + "/")
                );

                if (!isParent)
                    output.Add(item);
            }

            return output;
        }


        public static void AddCreatorBtn(string fullEntry, WinInstanceInfo info)
        {
            CreatorButton button = new(fullEntry);

            info.uiState.creatorButtons.AddButton(button, info);

            FillCreatorButtons(info);
        }

        public static void OnEditMenuEntry(CreatorButton btnProp, string newEntry, WinInstanceInfo info)
        {
            btnProp.menuEntry = newEntry;

            FillCreatorButtons(info);
        }



        // placeholder helper
        public static int PlaceholderNeedleAtPos(WinInstanceInfo info, float mouseX)
        {
            VisualElement area = info.uiState.shortcutButtonArea;

            bool containsPlaceholder = area.Contains(info.uiState.placeholderNeedle);

            // getting drop index
            int dropIndex = area.childCount;
            for (int i = area.childCount - 1; i >= 0; i--)
            {
                var child = area[i];

                if (containsPlaceholder && child == info.uiState.placeholderNeedle) continue;

                if (true)
                {
                    if (mouseX > child.layout.center.x) dropIndex--;
                    else break;
                }
            }
            if (containsPlaceholder) dropIndex--;

            if (dropIndex != info.uiState.placeHolderIndex)
            {
                info.uiState.placeHolderIndex = dropIndex;

                info.uiState.placeholderNeedle.RemoveFromHierarchy();
                area.Insert(dropIndex, info.uiState.placeholderNeedle);
            }

            return dropIndex;
        }


        // asset create menu items fetcher
        public static List<string> GetAssetCreateMenuEntries()
        {
            var list = new List<string>();

            var menuType = typeof(Editor).Assembly.GetType("UnityEditor.Menu");
            if (menuType == null) return list;

            var method = menuType.GetMethod(
                "GetMenuItems",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(bool), typeof(bool) },
                null
            );

            if (method == null) return list;
            var raw = method.Invoke(null, new object[] { "Assets/Create/", true, false });
            if (raw == null) return list;

            foreach (var item in (Array)raw)
            {
                string path = item.GetType().GetProperty("path")?.GetValue(item) as string;

                if (!string.IsNullOrEmpty(path))
                    list.Add(path);
            }

            return list;
        }


        // loader
        public static VisualTreeAsset LoadUXML(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{PathUtility.GetPieTabsPath()}/PinePie/PieTabs/editor/Core/UI/{relativePath}");

        public static Texture2D LoadTex(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{PathUtility.GetPieTabsPath()}/PinePie/PieTabs/editor/Core/UI/{relativePath}");


        // button setup 
        public static void SetupButtonProperties(VisualElement button, string Label, string color, bool isCreatorBtn = false, bool isMinimal = true)
        {
            // color and tooltip
            Color col = HexToColor(color);

            button.style.backgroundColor = col;
            button.tooltip = isMinimal ? Label : null;

            // label props
            var labelElement = button.Q<Label>("buttonLabel");
            if (labelElement != null)
            {
                labelElement.text = isCreatorBtn ? Label[0].ToString() : (isMinimal ? "" : Label);

                labelElement.style.color = IsColorDark(col)
                ? HexToColor("#f7f7f7")
                : HexToColor("#2e2e2e");
            }
        }


        // color setup helpers
        public static bool IsColorDark(Color color)
        {
            float brightness = (color.r * 0.299f) + (color.g * 0.587f) + (color.b * 0.114f);
            return brightness < 0.5f;
        }

        public static string ColorToHex(Color color)
        {
            return ColorUtility.ToHtmlStringRGBA(color);
        }

        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return HexToColor("#3E3E3E");

            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            hex = hex.ToUpperInvariant();

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            return HexToColor("#3E3E3E");
        }

        public enum PieAssetType
        {
            Folder,
            Scene,
            Prefab,
            Script,
            ShaderGraph,
            VisualScriptingGraph,
            Other
        }

        public static PieAssetType GetAssetType(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return PieAssetType.Folder;

            Type t = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (t == typeof(SceneAsset))
                return PieAssetType.Scene;

            if (t == typeof(GameObject))
                return PieAssetType.Prefab;

            if (t == typeof(MonoScript))
                return PieAssetType.Script;

            if (t.FullName == "UnityEditor.ShaderGraph.GraphData" ||
                t.FullName?.Contains("ShaderGraph") == true)
                return PieAssetType.ShaderGraph;

            if (t.FullName?.Contains("VisualScripting") == true)
                return PieAssetType.VisualScriptingGraph;

            return PieAssetType.Other;
        }


    }
}

#endif