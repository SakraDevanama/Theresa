using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;

namespace TheresaCode.Patches;

/// <summary>
/// 修复 NHoverTipSet.CreateAndShow 重复添加同一 owner 导致的 ArgumentException
/// 原版代码直接使用 _activeHoverTips.Add(owner, nHoverTipSet)，没有检查重复
/// </summary>
[HarmonyPatch(typeof(NHoverTipSet), nameof(NHoverTipSet.CreateAndShow), new[] { typeof(Control), typeof(IEnumerable<IHoverTip>), typeof(HoverTipAlignment) })]
public static class NHoverTipSetCreateAndShowFixPatch
{
    [HarmonyPrefix]
    public static void Prefix(Control owner)
    {
        // 如果该 owner 已经有活跃的 hover tip，先移除旧的
        // 使用反射访问私有静态字段 _activeHoverTips
        var field = typeof(NHoverTipSet).GetField("_activeHoverTips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (field != null)
        {
            var dict = field.GetValue(null) as Dictionary<Control, NHoverTipSet>;
            if (dict != null && dict.ContainsKey(owner))
            {
                NHoverTipSet.Remove(owner);
            }
        }
    }
}
