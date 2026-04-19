

using System.Diagnostics;
using Godot;

namespace Theresa.TheresaCode.Patches;

// 一个公共静态类，用于存放与 Theresa 卡牌相关的通用工具方法
public static class TheresaCardUtils
{
    // 定义允许触发欺骗效果的 UI 组件类型前缀列表
    private static readonly string[] AllowedTypePrefixes = [
        "MegaCrit.Sts2.Core.Nodes.Cards.",
        "MegaCrit.Sts2.Core.Nodes.HoverTips."
    ];

    // 定义允许触发欺骗效果的 UI 组件类型全名列表
    private static readonly string[] AllowedExactTypes = [
        "MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantCard",
        "MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen.NDeckHistoryEntry"
    ];

    // 一个公共静态方法，用于检查当前的调用栈是否来自允许的 UI 组件
    public static bool ShouldSpoofForUi()
    {
        try
        {
            // 获取当前线程的调用堆栈帧
            StackFrame[] frames = new StackTrace(fNeedFileInfo: false).GetFrames() ?? Array.Empty<StackFrame>();
            bool hasAllowedUiFrame = false;
            
            // 遍历堆栈帧
            foreach (StackFrame frame in frames)
            {
                // 获取当前帧所代表的方法的声明类型
                Type? type = frame.GetMethod()?.DeclaringType;
                string? fullName = type?.FullName;
                
                // 如果类型名为空，跳过此帧
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    continue;
                }

                // 先检查排除项：如果调用栈中包含筛选、排序、搜索等数据逻辑，绝不欺骗
                if (fullName.Contains("Filter", StringComparison.OrdinalIgnoreCase) ||
                    fullName.Contains("Sort", StringComparison.OrdinalIgnoreCase) ||
                    fullName.Contains("Search", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 记录是否有允许的 UI 组件
                if (!hasAllowedUiFrame)
                {
                    // 检查当前类型名是否在允许的精确列表中
                    if (AllowedExactTypes.Any(typeName => string.Equals(fullName, typeName, StringComparison.Ordinal)))
                    {
                        hasAllowedUiFrame = true;
                    }
                    // 检查当前类型名是否以允许的前缀开头
                    else if (AllowedTypePrefixes.Any(prefix => fullName.StartsWith(prefix, StringComparison.Ordinal)))
                    {
                        hasAllowedUiFrame = true;
                    }
                }
            }

            // 只有整个调用栈中都没有排除项，且存在允许的 UI 组件时，才进行欺骗
            return hasAllowedUiFrame;
        }
        // 捕获堆栈分析过程中的任何异常，并打印警告
        catch (Exception ex)
        {
            GD.PushWarning($"[TheresaCardAncientUiSpoofPatch] Stack inspection failed: {ex.Message}");
        }
        
        // 如果以上检查都不满足，返回 false，表示不应进行欺骗
        return false;
    }
}