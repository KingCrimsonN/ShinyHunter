#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Auxilium.ScreenshotPro
{
    public static class GameViewPresetCleanup
    {
        public static int RemoveScreenshotProPresets()
        {
            var assembly = typeof(Editor).Assembly;
            var gvSizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = assembly.GetType("UnityEditor.ScriptableSingleton`1")?.MakeGenericType(gvSizesType);
            var instance = singleton?.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");

            if (gvSizesType == null || instance == null || groupType == null) return 0;

            var getGroup = gvSizesType.GetMethod("GetGroup", BindingFlags.Public | BindingFlags.Instance);
            int total = 0;

            foreach (var val in Enum.GetValues(groupType))
            {
                var group = getGroup?.Invoke(instance, new object[] { val });
                if (group != null) total += ProcessGroup(group, assembly);
            }
            return total;
        }

        private static int ProcessGroup(object group, Assembly asm)
        {
            var type = group.GetType();
            var getCount = type.GetMethod("GetCustomCount");
            var getSize = type.GetMethod("GetGameViewSize");
            var removeSize = type.GetMethod("RemoveCustomSize");
            var gvSizeType = asm.GetType("UnityEditor.GameViewSize");

            int count = (int)getCount.Invoke(group, null);
            int removed = 0;

            for (int i = count - 1; i >= 0; i--)
            {
                var sizeObj = getSize.Invoke(group, new object[] { i });
                string label = sizeObj.GetType().GetProperty("baseText")?.GetValue(sizeObj) as string;
                if (!string.IsNullOrEmpty(label) && label.Contains("ScreenshotPro"))
                {
                    removeSize.Invoke(group, new object[] { i });
                    removed++;
                }
            }
            return removed;
        }
    }
}
#endif