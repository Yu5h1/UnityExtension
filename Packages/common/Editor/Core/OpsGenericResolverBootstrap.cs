using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// 把 OpsFactory 的 Component↔Ops 資料接到 AddonRegistry。
    ///
    /// 這個檔案是橋接層：
    ///   - AddonRegistry 不認識 OpsFactory / IOps（保持解耦）
    ///   - OpsFactory 不認識 AddonRegistry（Runtime 不依賴 Editor）
    ///   - 由這個 Editor-only bootstrap 把兩邊串起來
    /// </summary>
    public class OpsGenericResolver : AddonRegistry.IAddonResolver
    {
        private static readonly Type _opsInterfaceType = typeof(IOps);

        public IEnumerable<Type> GetTargetComponents(Type addonType)
        {
            for (var t = addonType.BaseType; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                if (!t.IsGenericType) continue;

                foreach (var arg in t.GetGenericArguments())
                {
                    if (!arg.IsInterface) continue;
                    if (!_opsInterfaceType.IsAssignableFrom(arg)) continue;

                    if (OpsFactory.Ops.TryGetValue(arg, out var componentTypes))
                        foreach (var componentType in componentTypes)
                            yield return componentType;
                }
            }
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            AddonRegistry.RegisterResolver(new OpsGenericResolver());
        }
    }
}
