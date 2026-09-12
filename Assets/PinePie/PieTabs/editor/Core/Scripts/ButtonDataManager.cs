// Copyright (c) 2025 PinePie. All rights reserved.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PinePie.PieTabs
{
    public interface IButtonData
    {
        string Label { get; }

        VisualElement GetButtonElement(VisualTreeAsset template, WinInstanceInfo info);
    }

    public class PieButton<T> where T : IButtonData
    {
        public T buttonProp;
        public VisualElement UIbutton;

        public PieButton(T buttonProp, VisualElement button)
        {
            this.buttonProp = buttonProp;
            UIbutton = button;
        }
    }

    // abstract base class for both buttons
    public abstract class ButtonBundle<TButton> where TButton : IButtonData
    {
        public List<PieButton<TButton>> buttons = new();


        public void AddButton(TButton button, WinInstanceInfo info)
        {
            var btn = new PieButton<TButton>(button, button.GetButtonElement(info.uiState.creatorButtonAsset, info));

            buttons.Add(btn);

            SaveToJson();
        }

        public void InsertAt(TButton button, WinInstanceInfo info)
        {
            var btn = new PieButton<TButton>(button, button.GetButtonElement(info.uiState.shortcutButtonAsset, info));

            buttons.Insert(info.uiState.placeHolderIndex, btn);

            SaveToJson();
        }

        public void RemoveButton(TButton toRemove)
        {
            int index = buttons.FindIndex(b => EqualityComparer<TButton>.Default.Equals(b.buttonProp, toRemove));
            if (index >= 0)
            {
                buttons.RemoveAt(index);
                SaveToJson();
            }
        }

        public void LoadFromJson(VisualTreeAsset template, WinInstanceInfo info)
        {
            buttons = LoadButtonData()
                .Select(b => new { data = b, element = b.GetButtonElement(template, info) })
                .Where(pair => pair.element != null)
                .Select(pair => new PieButton<TButton>(pair.data, pair.element))
                .ToList();
        }

        public abstract void SaveToJson();
        protected abstract List<TButton> LoadButtonData();
    }

    // shortcut button
    [Serializable]
    public class ShortcutButton : IButtonData
    {
        public string label; // keep it public to be serialized
        public string guid;
        public bool isMinimal;
        public string color = "#3E3E3E";

        public string Label => label;
        public string Path => AssetDatabase.GUIDToAssetPath(guid);


        public ShortcutButton(string label, string guid, bool isMinimal = false, string color = "#3E3E3E")
        {
            this.label = label;
            this.guid = guid;
            this.isMinimal = isMinimal;
            this.color = color;
        }

        public VisualElement GetButtonElement(VisualTreeAsset template, WinInstanceInfo info)
        {
            var button = template.Instantiate().Q<VisualElement>("customButton");
            if (button == null) return null;

            Navigator.SetupButtonProperties(button, Label, color, false, !info.isTwoColumn || isMinimal);

            // icon
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Path);
            if (obj == null) return null;

            Texture2D iconTexture = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image as Texture2D;
            button.Q<VisualElement>("buttonIcon").style.backgroundImage = new StyleBackground(iconTexture);

            // shade            
            var btn = button.Q<VisualElement>("shade");
            button.RegisterCallback<MouseEnterEvent>(_ =>
            {
                btn.style.backgroundColor = new Color(255, 255, 255, 0.1f);
            });
            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                btn.style.backgroundColor = new Color(255, 255, 255, 0f);
            });

            Navigator.OnShortcutButtonClicked(button, this, info);
            return button;
        }
    }

    public class ShortcutButtonBundle : ButtonBundle<ShortcutButton>
    {
        public override void SaveToJson() => ButtonCache<ShortcutButton>.Save(buttons.Select(b => b.buttonProp).ToList());

        protected override List<ShortcutButton> LoadButtonData() => ButtonCache<ShortcutButton>.Load();
    }

    // creator button data
    [Serializable]
    public class CreatorButton : IButtonData
    {
        public string menuEntry;
        public string color = "#3E3E3E";

        public string Label => string.IsNullOrEmpty(menuEntry) ? "" : menuEntry[(menuEntry.LastIndexOf('/') + 1)..];


        public CreatorButton(string entry)
        {
            menuEntry = entry;

            int index = UnityEngine.Random.Range(0, Navigator.palette.Count);
            color = Navigator.ColorToHex(Navigator.palette[index]);
        }

        public VisualElement GetButtonElement(VisualTreeAsset template, WinInstanceInfo info)
        {
            var button = template.Instantiate().Q<VisualElement>("customButton");
            if (button == null) return null;

            Navigator.SetupButtonProperties(button, Label, color, true);

            // shade
            var btn = button.Q<VisualElement>("shade");
            button.RegisterCallback<MouseEnterEvent>(_ =>
            {
                btn.style.backgroundColor = new Color(255, 255, 255, 0.1f);
            });
            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                btn.style.backgroundColor = new Color(255, 255, 255, 0f);
            });

            Navigator.OnAssetCreatorButtonClicked(button, this);
            return button;
        }
    }

    public class CreatorButtonBundle : ButtonBundle<CreatorButton>
    {
        public override void SaveToJson() => ButtonCache<CreatorButton>.Save(buttons.Select(b => b.buttonProp).ToList());
        protected override List<CreatorButton> LoadButtonData() => ButtonCache<CreatorButton>.Load();
    }

}

#endif