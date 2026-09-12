// Copyright (c) 2025 PinePie. All rights reserved.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace PinePie.PieTabs
{
    public static partial class Navigator
    {
        // UI Setup

        public static void OnDrag(float mouseX, WinInstanceInfo info)
        {
            UI ui = info.uiState;

            VisualElement area = ui.creatorButtonArea;

            List<VisualElement> tabs = area.Children().Where(c => c.name != "line").ToList();

            int newIndex = tabs.Count;
            for (int i = tabs.Count - 1; i >= 0; i--)
            {
                var child = tabs[i];
                if (child == ui.placeholderNeedle) continue;

                Rect rect = child.worldBound;
                float midX = rect.x + rect.width / 2;

                if (mouseX > midX) newIndex--;
            }

            if (newIndex != ui.placeHolderIndex)
            {
                ui.placeHolderIndex = newIndex;

                if (area.Contains(ui.placeholderNeedle)) ui.placeholderNeedle.RemoveFromHierarchy();
                area.Insert(newIndex, ui.placeholderNeedle);
            }

            if (!area.Contains(ui.placeholderNeedle)) // on over inserting section
                area.Insert(newIndex, ui.placeholderNeedle);
        }

        public static void EndDrag(VisualElement btn, WinInstanceInfo info)
        {
            VisualElement area = info.uiState.creatorButtonArea;

            if (info.uiState.placeHolderIndex < 0)
                return;

            info.uiState.placeholderNeedle.RemoveFromHierarchy();

            int oldIndex = area.IndexOf(btn);

            MoveItem(info.uiState.creatorButtons.buttons, oldIndex, info.uiState.placeHolderIndex);
            info.uiState.creatorButtons.SaveToJson();

            info.uiState.placeHolderIndex = -1;
        }

        public static void MoveItem<T>(List<T> list, int fromIndex, int toIndex)
        {
            toIndex = Mathf.Clamp(toIndex, 0, list.Count);

            if (fromIndex == toIndex || (fromIndex == list.Count - 1 && toIndex == list.Count))
                return;

            T item = list[fromIndex];
            list.RemoveAt(fromIndex);

            if (toIndex > fromIndex) toIndex--;

            if (toIndex >= list.Count) list.Add(item);
            else list.Insert(toIndex, item);
        }


        // split bars
        public static void SetupDragAreaStyling(WinInstanceInfo info)
        {
            foreach (var view in new VisualElement[] { info.uiState.shortcutButtonArea, info.uiState.creatorButtonArea })
            {
                view.contentContainer.style.flexDirection = FlexDirection.RowReverse;
                view.contentContainer.style.justifyContent = Justify.FlexStart;
            }
        }

        public static void SetupSplitter(WinInstanceInfo info)
        {
            VisualElement splitter = info.mainUI.Q<VisualElement>("splitter");

            bool isDragging = false;
            int pointerId = -1;
            float distFromMouse = 0;

            info.uiState.shortcutButtonArea.style.flexGrow = 0;
            info.uiState.shortcutButtonArea.style.width = LastCreatorWidth;

            splitter.RegisterCallback<PointerDownEvent>(evt =>
            {
                isDragging = true;
                pointerId = evt.pointerId;

                distFromMouse = evt.position.x - splitter.worldBound.x - 5f;

                splitter.CapturePointer(pointerId);

                evt.StopPropagation();
            });

            splitter.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!isDragging || evt.pointerId != pointerId) return;

                info.uiState.shortcutButtonArea.style.width = evt.position.x - 107f - distFromMouse;
                info.uiState.creatorButtonArea.style.flexGrow = 1;

                evt.StopPropagation();
            });

            splitter.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId != pointerId) return;

                LastCreatorWidth = info.uiState.shortcutButtonArea.resolvedStyle.width;
                isDragging = false;

                splitter.ReleasePointer(pointerId);
                evt.StopPropagation();
            });
        }

        public static void SearchBarAndCreatorTabBtnCallbacks(WinInstanceInfo info)
        {
            VisualElement splitter = info.mainUI.Q<VisualElement>("splitter");
            splitter.style.backgroundImage = LoadTex("grip.png");

            Button searchBtn = info.mainUI.Q<Button>("searchToggle");
            searchBtn.Q<VisualElement>("icon").style.backgroundImage = LoadTex("magnifying-glass (1).png");

            Button creatorTabAddingBtn = info.mainUI.Q<Button>("addCreatorBtn");
            creatorTabAddingBtn.style.backgroundImage = LoadTex("plus 1.png");

            ScrollView AssetCreatorButtonArea = info.mainUI.Q<ScrollView>("CreationMenuDragArea");
            ScrollView shortcutTabsArea = info.mainUI.Q<ScrollView>("shorcutsDragArea");

            Button sceneMenu = info.mainUI.Q<Button>("sceneSel");

            if (IsSearchBarOpen)
                OpenSearchBar(info, sceneMenu, creatorTabAddingBtn, splitter, searchBtn, AssetCreatorButtonArea);
            else
                CloseSearchBar(info, sceneMenu, creatorTabAddingBtn, splitter, searchBtn, AssetCreatorButtonArea, shortcutTabsArea);


            sceneMenu.clicked += () =>
            {
                var menu = new GenericMenu();

                string[] guids = AssetDatabase.FindAssets("t:Scene");
                string activeScenePath = EditorSceneManager.GetActiveScene().path;

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.StartsWith("Assets/"))
                        continue;

                    string name = Path.GetFileNameWithoutExtension(path);

                    string capturedPath = path;
                    menu.AddItem(new GUIContent(name), capturedPath == activeScenePath, () =>
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(capturedPath);
                    });
                }

                menu.ShowAsContext();
            };


            searchBtn.clicked += () =>
            {
                if (IsSearchBarOpen)
                    CloseSearchBar(info, sceneMenu, creatorTabAddingBtn, splitter, searchBtn, AssetCreatorButtonArea, shortcutTabsArea);
                else
                    OpenSearchBar(info, sceneMenu, creatorTabAddingBtn, splitter, searchBtn, AssetCreatorButtonArea);
            };

            creatorTabAddingBtn.clicked += () =>
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
                        AddCreatorBtn(item, info);
                    });
                }
                menu.DropDown(new Rect(creatorTabAddingBtn.worldBound.position, Vector2.zero));
            };

        }

        private static void CloseSearchBar(WinInstanceInfo info, Button sceneBtn, Button creatorBtn, VisualElement splitter, Button openSearchButton, ScrollView AssetCreatorButtonArea, ScrollView shortcutTabsArea)
        {
            if (info.isTwoColumn)
            {
                splitter.style.display = DisplayStyle.Flex;
                creatorBtn.style.display = DisplayStyle.Flex;
                sceneBtn.style.display = DisplayStyle.Flex;
                shortcutTabsArea.scrollOffset = Vector2.zero;
            }

            openSearchButton.style.width = 30f;

            AssetCreatorButtonArea.style.width = LastCreatorWidth;
            AssetCreatorButtonArea.style.marginRight = 0f;

            IsSearchBarOpen = false;
        }

        private static void OpenSearchBar(WinInstanceInfo info, Button sceneBtn, Button creatorBtn, VisualElement splitter, Button openSearchButton, ScrollView AssetCreatorButtonArea)
        {
            if (!info.isTwoColumn)
            {
                openSearchButton.style.width = 20f;
                sceneBtn.style.display = DisplayStyle.None;
            }

            splitter.style.display = DisplayStyle.None;
            creatorBtn.style.display = DisplayStyle.None;

            AssetCreatorButtonArea.style.width = 0f;
            AssetCreatorButtonArea.style.marginRight = 425;

            IsSearchBarOpen = true;
        }


        // address copy from bottom bar
        public static void SetupBottomBarMargin(WinInstanceInfo winInfo)
        {
            VisualElement bottomAddressBar = winInfo.mainUI.Q<VisualElement>("bottomAddressBar");

            bottomAddressBar.style.marginLeft = GetSideRectWidth(winInfo.editorWindow);
        }

        public static void RegisterAddressCopyCallbacks(WinInstanceInfo info)
        {
            VisualElement bottomAddressBar = info.mainUI.Q<VisualElement>("bottomAddressBar");
            bottomAddressBar.RegisterCallback<MouseDownEvent>((evt) =>
            {
                if (info.isTwoColumn) bottomAddressBar.style.marginLeft = GetSideRectWidth(info.editorWindow);

                string copyingStr = "";

                if (evt.button == 0)
                {
                    copyingStr = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject))
                        ? AssetDatabase.GetAssetPath(Selection.activeObject)
                        : GetActiveFolderPath(info.editorWindow);
                }
                else if (evt.button == 1)
                {
                    string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                    copyingStr = !string.IsNullOrEmpty(assetPath)
                        ? Path.GetFileName(assetPath)
                        : "";
                }

                EditorGUIUtility.systemCopyBuffer = copyingStr;

                ShowCopiedNotification(evt.mousePosition, info);
            });

            if (info.isTwoColumn)
                Selection.selectionChanged += () =>
                {
                    SetupBottomBarMargin(info);
                };
        }

        public static void ShowCopiedNotification(Vector2 position, WinInstanceInfo info)
        {
            info.uiState.copiedText.style.left = position.x - 20;

            info.uiState.copiedText.style.display = DisplayStyle.Flex;

            info.mainUI.schedule.Execute(() =>
            {
                info.uiState.copiedText.style.display = DisplayStyle.None;
            }).ExecuteLater(1000);
        }


        // icon popup
        public static void ShowBoxAtPos(VisualElement box, float posX, VisualElement win)
        {
            win.pickingMode = PickingMode.Position;

            float rightOffset = win.resolvedStyle.width - 200;

            box.style.left = Mathf.Clamp(posX, 0, rightOffset);

            box.style.display = DisplayStyle.Flex;
        }

        public static void CallbacksForPopupBoxes(WinInstanceInfo info)
        {
            info.mainUI.RegisterCallback<MouseDownEvent>((evt) =>
            {
                CloseAllPopups(info);
            });
        }

        private static void CloseAllPopups(WinInstanceInfo info)
        {
            info.uiState.colorPopup.style.display = DisplayStyle.None;

            info.uiState.popupTarget.popupIsOpen = false;

            info.mainUI.pickingMode = PickingMode.Ignore;
        }

        public static void CallbacksForColorPopup(WinInstanceInfo info)
        {
            var colorButtons = info.uiState.colorPopup.Query<Button>("icon").ToList();
            var removeColorBtn = info.uiState.colorPopup.Q<Button>("removeColorBtn");

            palette.Clear();
            foreach (Button clrBtn in colorButtons)
            {
                var buttonColor = clrBtn.resolvedStyle.backgroundColor;
                palette.Add(buttonColor);

                clrBtn.clicked += () =>
                {
                    if (!info.uiState.popupTarget.popupIsOpen) return;

                    ApplyColorToTargetElement(ColorToHex(buttonColor), info);
                };
            }

            removeColorBtn.style.backgroundImage = LoadTex("cross icon.png");
            removeColorBtn.clicked += () =>
            {
                if (!info.uiState.popupTarget.popupIsOpen) return;

                ApplyColorToTargetElement("#3E3E3E", info);
            };

            info.uiState.colorPopup.RegisterCallback<MouseDownEvent>((evt) => evt.StopPropagation());
        }

        public static void ApplyColorToTargetElement(string hex, WinInstanceInfo info)
        {
            ColorPopupTarget target = info.uiState.popupTarget;

            if (target.isForCreator)
            {
                target.activeCreatorButton.color = hex;
                info.uiState.creatorButtons.SaveToJson();
            }
            else
            {
                target.activeNavButton.color = hex;
                info.uiState.navButtons.SaveToJson();
            }

            Color clr = HexToColor(hex);

            target.activeVisualItem.style.backgroundColor = clr;
            target.activeVisualItem.Q<Label>("buttonLabel").style.color = IsColorDark(clr)
                ? HexToColor("#f7f7f7")
                : HexToColor("#2e2e2e");

            CloseAllPopups(info);
        }

    }
}

#endif