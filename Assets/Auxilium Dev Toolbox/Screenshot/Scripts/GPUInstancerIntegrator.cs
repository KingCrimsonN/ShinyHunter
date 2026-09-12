#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Auxilium.ScreenshotPro
{
    /// <summary>
    /// Automatically detects GPU Instancer packages and sets scripting define symbols.
    /// Wrapped in #if UNITY_EDITOR so it does not break runtime builds.
    /// </summary>
    [InitializeOnLoad]
    public class GPUInstancerIntegrator
    {
        static GPUInstancerIntegrator()
        {
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;

#pragma warning disable CS0618
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

            bool pro = System.AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetType("GPUInstancerPro.GPUIRenderingSystem") != null);
            bool std = System.AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetType("GPUInstancer.GPUInstancerAPI") != null);

            UpdateDefine(target, "GPUI_PRO", pro, ref defines);
            UpdateDefine(target, "GPUI_STANDARD", std, ref defines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
#pragma warning restore CS0618
        }

        private static void UpdateDefine(BuildTargetGroup group, string sym, bool active, ref string current)
        {
            if (active && !current.Contains(sym))
                current += ";" + sym;
            else if (!active && current.Contains(sym))
                current = current.Replace(sym, "").Replace(";;", ";").Trim(';');
        }
    }
}
#endif
