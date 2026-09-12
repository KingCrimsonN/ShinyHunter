// Copyright (c) 2025 PinePie. All rights reserved.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PinePie.PieTabs
{
    [InitializeOnLoad]
    public static class PieTabsLoader
    {
        static PieTabsLoader() => EditorApplication.update += RunOnceOnLoad;

        static void RunOnceOnLoad()
        {
            EditorApplication.update -= RunOnceOnLoad;

            if (!Directory.Exists($"{PathUtility.GetPieTabsPath()}/PinePie/PieTabs"))
                return;

            Navigator.Setup();
        }

        public static void EnsurePieDeskOverlay()
        {
            var lastFocused = EditorWindow.focusedWindow;
            if (lastFocused != null && lastFocused.GetType().Name == "ProjectBrowser")
            {
                Selection.selectionChanged -= EnsurePieDeskOverlay;
                Navigator.Setup();
            }
        }

        /// please press F5 if anyway PieTabs UI is not visible. ///

        [MenuItem("Tools/Refresh PieTabs _F5")]
        public static void RefreshPieDesk()
        {
            Navigator.Setup();
        }
    }


    public static partial class Navigator
    {
        public static List<Color> palette = new();
        private const string SplitterKey = "PieTabs_LastSplitterSpacing";
        private const string SearchBarOpenKey = "PieTabs_SearchBarOpen";

        private static float LastCreatorWidth
        {
            get => EditorPrefs.GetFloat(SplitterKey, 300f);
            set => EditorPrefs.SetFloat(SplitterKey, value);
        }

        private static bool IsSearchBarOpen
        {
            get => EditorPrefs.GetBool(SearchBarOpenKey, false);
            set => EditorPrefs.SetBool(SearchBarOpenKey, value);
        }


        public static void Setup()
        {
            GetProjectBrowserUIs();
            foreach (var info in windowsInfo)
            {
                if (info.ProjUI.panel == null)
                    continue;

                SetupForWindow(info);

                info.ProjUI.RegisterCallback<DetachFromPanelEvent>(evt =>
                {
                    Selection.selectionChanged -= PieTabsLoader.EnsurePieDeskOverlay;

                    EditorApplication.delayCall += () =>
                    {
                        if (info.ProjUI.panel == null)
                            Selection.selectionChanged += PieTabsLoader.EnsurePieDeskOverlay;
                    };
                });
            }
        }

        private static void SetupForWindow(WinInstanceInfo info)
        {
            info.mainUI = LoadUXML("PieDeskMainUI.uxml").Instantiate().Q<VisualElement>("PieDeskUI");
            info.mainUI.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>($"{PathUtility.GetPieTabsPath()}/PinePie/PieTabs/editor/Core/UI/PieDeskStyling.uss"));

            UI ui = info.uiState;

            ui.shortcutButtonAsset = LoadUXML("shortcutButton.uxml");
            ui.creatorButtonAsset = LoadUXML("creatorButton.uxml");
            ui.placeholderNeedle = LoadUXML("placeholderLine.uxml").Instantiate().Q<VisualElement>("line");

            ui.copiedText = info.mainUI.Q<VisualElement>("copiedText");
            ui.colorPopup = info.mainUI.Q<VisualElement>("colorPopup");
            CallbacksForColorPopup(info);

            info.ProjUI.Q<VisualElement>("PieDeskUI")?.RemoveFromHierarchy(); // remove if found

            info.isTwoColumn = IsTwoColumnMode(info.editorWindow);
            PieTabsPrefs.LoadKeyComb();

            ui.shortcutButtonArea = info.mainUI.Q<ScrollView>("shorcutsDragArea");
            ui.creatorButtonArea = info.mainUI.Q<ScrollView>("CreationMenuDragArea");
            if (!info.isTwoColumn)
            {
                ui.shortcutButtonArea.style.display = DisplayStyle.None;
                ui.creatorButtonArea.style.flexGrow = 1;

                info.mainUI.Q<VisualElement>("splitter").style.display = DisplayStyle.None;

                VisualElement bottomBar = info.mainUI.Q<VisualElement>("bottomAddressBar");
                bottomBar.style.marginRight = 0;
                bottomBar.style.marginLeft = 0;

                ui.shortcutButtonArea = info.mainUI.Q<ScrollView>("CreationMenuDragArea");
            }
            else // two coloumn mode 
            {
                SetupSplitter(info);
                SetupBottomBarMargin(info);
                ui.creatorButtonArea.style.width = LastCreatorWidth;

                // asset creator button 
                ui.creatorButtons.LoadFromJson(ui.creatorButtonAsset, info);
                FillCreatorButtons(info);
            }

            CallbacksForPopupBoxes(info);
            RegisterAddressCopyCallbacks(info);
            SetupDragAreaStyling(info);
            SearchBarAndCreatorTabBtnCallbacks(info);

            // shortcut buttons
            ui.navButtons.LoadFromJson(ui.shortcutButtonAsset, info);
            SetupDragNDropForShortcutArea(info);
            FillShortcutButtons(info);


            info.ProjUI.Add(info.mainUI);
        }


        // click callbacks
        public static void OnShortcutButtonClicked(
            VisualElement UIbutton,
            ShortcutButton buttonProp,
            WinInstanceInfo info)
        {
            // callbacks
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(buttonProp.Path);
            UIbutton.AddManipulator(new ShortcutDragManipulator(obj));


            UIbutton.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    UIbutton.Q<VisualElement>("shade").style.backgroundColor = new Color(255, 255, 255, 0.15f);

                    evt.StopPropagation();
                }
                else if (evt.button == 1)
                {
                    evt.StopPropagation();
                }
            });

            UIbutton.RegisterCallback<PointerUpEvent>(evt =>
            {
                // single click
                if (obj == null) return;

                if (PieTabsPrefs.KeyMatches(evt, PieTabsPrefs.directOpenShortcut))
                {
                    if (AssetDatabase.IsValidFolder(buttonProp.Path))
                        OpenFolder(buttonProp.Path, info?.editorWindow);
                    else
                        AssetDatabase.OpenAsset(obj);

                    evt.StopPropagation();
                }
                else if (PieTabsPrefs.KeyMatches(evt, PieTabsPrefs.minimalShortcut))
                {
                    buttonProp.isMinimal = !buttonProp.isMinimal;

                    UIbutton.Q<Label>("buttonLabel").text = buttonProp.isMinimal ? "" : buttonProp.Label;
                    UIbutton.tooltip = buttonProp.isMinimal ? buttonProp.Label : null;

                    var buttonShade = UIbutton.Q<VisualElement>("shade");
                    buttonShade.Q<Label>("buttonLabel").text = buttonProp.isMinimal ? "" : buttonProp.Label;

                    info.uiState.navButtons.SaveToJson();

                    evt.StopPropagation();
                }
                else if (PieTabsPrefs.KeyMatches(evt, PieTabsPrefs.colorShortcut))
                {
                    ShowBoxAtPos(info.uiState.colorPopup, evt.position.x - 100, info?.mainUI);

                    info.uiState.popupTarget.isForCreator = false;
                    info.uiState.popupTarget.popupIsOpen = true;

                    info.uiState.popupTarget.activeNavButton = buttonProp;
                    info.uiState.popupTarget.activeVisualItem = UIbutton;

                    evt.StopPropagation();
                }

                else if (PieTabsPrefs.KeyMatches(evt, PieTabsPrefs.deleteShortcut))
                {
                    if (PieTabsPrefs.AskBeforeDelete)
                    {
                        bool confirm = EditorUtility.DisplayDialog(
                            "Delete Tab",
                            $"Are you sure you want to delete \"{buttonProp.Label}\" Tab?",
                            "Delete", "Cancel"
                        );

                        if (confirm) RemoveButton(buttonProp, info);
                    }
                    else RemoveButton(buttonProp, info);

                    evt.StopPropagation();
                }

                else if (evt.button == 0)
                {
                    string path = buttonProp.Path;
                    PieAssetType type = GetAssetType(path);

                    if (type == PieAssetType.Folder && PieTabsPrefs.FastFolderOpen)
                        OpenFolder(path, info?.editorWindow);
                    else if ((type == PieAssetType.Scene && PieTabsPrefs.FastSceneOpen)
                            || (type == PieAssetType.Script && PieTabsPrefs.FastScriptOpen)
                            || (type == PieAssetType.Prefab && PieTabsPrefs.FastPrefabOpen)
                            || (type == PieAssetType.ShaderGraph && PieTabsPrefs.FastShaderGraphOpen)
                            || (type == PieAssetType.VisualScriptingGraph && PieTabsPrefs.FastVisScrGraphOpen))
                        AssetDatabase.OpenAsset(obj);
                    else
                        FocusAssetByObj(obj, info?.editorWindow);


                    evt.StopPropagation();
                }

                UIbutton.Q<VisualElement>("shade").style.backgroundColor = new Color(255, 255, 255, 0.12f);
            });

        }

        public static void OnAssetCreatorButtonClicked(
            VisualElement UIbutton,
            CreatorButton buttonProp)
        {
            WinInstanceInfo info = GetFocusedWindow();

            // callbacks
            UIbutton.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    UIbutton.Q<VisualElement>("shade").style.backgroundColor = new Color(255, 255, 255, 0.15f);

                    info.uiState.dragState.dragStartPos = evt.position;
                    info.uiState.dragState.isMouseDown = true;
                    info.uiState.dragState.isDragging = false;

                    evt.StopPropagation();
                }
                else if (evt.button == 1)
                {
                    evt.StopPropagation();
                }
            });

            UIbutton.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!info.uiState.dragState.isMouseDown) return;

                if (!info.uiState.dragState.isDragging && Vector2.Distance(evt.position, info.uiState.dragState.dragStartPos) > 3f)
                {
                    info.uiState.dragState.isDragging = true;
                    UIbutton.CaptureMouse();
                }

                if (UIbutton.HasMouseCapture() && info.uiState.dragState.isDragging) OnDrag(evt.position.x, info);
            });

            UIbutton.RegisterCallback<PointerUpEvent>(evt =>
            {
                // single click
                if (evt.button == 0) info.uiState.dragState.isMouseDown = false;

                if (info.uiState.dragState.isDragging)
                {
                    info.uiState.dragState.isDragging = false;
                    UIbutton.ReleaseMouse();

                    EndDrag(UIbutton, info);
                    FillCreatorButtons(info);

                    evt.StopPropagation();
                    return;
                }

                if (PieTabsPrefs.KeyMatches(evt, PieTabsPrefs.changeMenuEntryShortcut))
                {
                    List<string> items = GetAssetCreateMenuEntries();

                    var menu = new GenericMenu();
                    foreach (var item in CleanEntries(items))
                    {
                        var trimmedItem = item;
                        const string prefix = "Assets/Create/";

                        if (item.StartsWith(prefix))
                            trimmedItem = item[prefix.Length..];

                        menu.AddItem(new GUIContent(trimmedItem), false, () =>
                        {
                            OnEditMenuEntry(buttonProp, item, info);
                        });
                    }
                    menu.DropDown(new Rect(evt.position, Vector2.zero));

                    evt.StopPropagation();
                }
                // color setting
                else if (PieTabsPrefs.KeyMatches(evt, PieTabsPrefs.colorShortcut))
                {
                    ShowBoxAtPos(info.uiState.colorPopup, evt.position.x - 100, info?.mainUI);

                    info.uiState.popupTarget.isForCreator = true;
                    info.uiState.popupTarget.popupIsOpen = true;

                    info.uiState.popupTarget.activeCreatorButton = buttonProp;
                    info.uiState.popupTarget.activeVisualItem = UIbutton;

                    evt.StopPropagation();
                }
                else if (evt.button == 0)
                {
                    EditorApplication.ExecuteMenuItem(buttonProp.menuEntry);

                    evt.StopPropagation();
                }

                else if (PieTabsPrefs.KeyMatches(evt, PieTabsPrefs.deleteShortcut))
                {
                    if (PieTabsPrefs.AskBeforeDelete)
                    {
                        bool confirm = EditorUtility.DisplayDialog(
                            "Delete Tab",
                            $"Are you sure you want to delete \"{buttonProp.Label}\" Tab?",
                            "Delete",
                            "Cancel"
                        );

                        if (confirm) RemoveButton(buttonProp, info);
                    }
                    else RemoveButton(buttonProp, info);

                    evt.StopPropagation();
                }
            });

        }


        // filling and removing
        public static void FillShortcutButtons(WinInstanceInfo info)
        {
            info.uiState.shortcutButtonArea.Clear();

            foreach (var button in info.uiState.navButtons.buttons)
            {
                info.uiState.shortcutButtonArea.Add(button.UIbutton);
            }
        }
        public static void FillCreatorButtons(WinInstanceInfo info)
        {
            info.uiState.creatorButtonArea.Clear();

            foreach (var button in info.uiState.creatorButtons.buttons)
            {
                info.uiState.creatorButtonArea.Add(button.UIbutton);
            }
        }

        public static void RemoveButton(ShortcutButton toRemove, WinInstanceInfo info)
        {
            info.uiState.navButtons.RemoveButton(toRemove);

            FillShortcutButtons(info);
        }
        public static void RemoveButton(CreatorButton toRemove, WinInstanceInfo info)
        {
            info.uiState.creatorButtons.RemoveButton(toRemove);

            FillCreatorButtons(info);
        }


        // shortcut bar dragging
        public static void SetupDragNDropForShortcutArea(WinInstanceInfo info)
        {
            info.uiState.shortcutButtonArea.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                PlaceholderNeedleAtPos(info, evt.localMousePosition.x);

                evt.StopPropagation();
            });

            info.uiState.shortcutButtonArea.RegisterCallback<DragPerformEvent>(evt =>
            {
                DragAndDrop.AcceptDrag();

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));

                    Texture2D iconTexture = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image as Texture2D;

                    var foundButton = info.uiState.navButtons.buttons.FirstOrDefault(b => b.buttonProp.guid == guid);
                    ShortcutButton buttonAtSamePath = foundButton?.buttonProp;

                    var button = new ShortcutButton(obj.name, guid);
                    if (buttonAtSamePath != null) button = new ShortcutButton(obj.name, guid, buttonAtSamePath.isMinimal, buttonAtSamePath.color);

                    if (info.uiState.placeHolderIndex != -1) info.uiState.navButtons.InsertAt(button, info);

                    if (buttonAtSamePath != null) info.uiState.navButtons.RemoveButton(buttonAtSamePath);

                    info.uiState.placeholderNeedle.RemoveFromHierarchy();
                    info.uiState.placeHolderIndex = -1;
                }

                FillShortcutButtons(info);

                evt.StopPropagation();
            });

            info.uiState.shortcutButtonArea.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    info.uiState.placeholderNeedle.RemoveFromHierarchy();

                    info.uiState.placeHolderIndex = -1;
                }
            });
        }

    }

    public class UI
    {
        public ShortcutButtonBundle navButtons = new();
        public CreatorButtonBundle creatorButtons = new();

        public VisualElement placeholderNeedle;
        public VisualElement copiedText;

        public VisualElement colorPopup;

        public VisualTreeAsset shortcutButtonAsset;
        public VisualTreeAsset creatorButtonAsset;


        public VisualElement shortcutButtonArea;
        public VisualElement creatorButtonArea;

        public int placeHolderIndex;

        public DragState dragState = new();
        public ColorPopupTarget popupTarget = new();

    }

    public class ColorPopupTarget
    {
        public bool isForCreator = false;
        public bool popupIsOpen = false;

        public ShortcutButton activeNavButton;
        public CreatorButton activeCreatorButton;
        public VisualElement activeVisualItem;
    }

    public class DragState
    {
        public Vector2 dragStartPos;
        public bool isDragging = false;
        public bool isMouseDown = false;
    }



    public class WinInstanceInfo
    {
        public bool isTwoColumn;
        public EditorWindow editorWindow;
        public VisualElement ProjUI;
        public VisualElement mainUI;

        public UI uiState = new();
    }



}
#endif
