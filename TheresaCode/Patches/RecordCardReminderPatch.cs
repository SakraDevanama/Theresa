using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 存续提示补丁：在选卡奖励界面的卡牌上方显示提示条带
/// 
/// Java 原版在选卡奖励时，在卡牌上方显示"移除卡牌添加：XXX"的提示条带。
/// STS2 中通过 CardRewardAlternative 在底部添加按钮，不够明显。
/// 此补丁在检测到"存续"选项时，在每张卡牌上方添加醒目的提示条带。
/// </summary>
[HarmonyPatch(typeof(NCardRewardSelectionScreen), nameof(NCardRewardSelectionScreen.RefreshOptions))]
public static class RecordCardReminderPatch
{
    // 提示条带的颜色（深棕色背景 + 金色文字，类似Java版本风格）
    private static readonly Color BannerBgColor = new Color(0.25f, 0.15f, 0.08f, 0.92f);
    private static readonly Color BannerTextColor = new Color(0.95f, 0.85f, 0.55f, 1.0f);
    private static readonly Color BannerBorderColor = new Color(0.6f, 0.5f, 0.25f, 1.0f);
    
    // 条带尺寸和位置
    private const float BannerWidth = 260f;
    private const float BannerHeight = 36f;
    private const float BannerYOffset = -290f; // 卡牌上方的位置

    [HarmonyPostfix]
    public static void RefreshOptionsPostfix(NCardRewardSelectionScreen __instance)
    {
        // 条带视觉效果已移除，此补丁不再执行任何操作
    }
}
