using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Relics;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// UnknownRelic Action 计数补丁
/// 
/// 使用 Harmony 拦截 CardCmd.AutoPlay 来计数卡牌打出动作
/// </summary>
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.AutoPlay))]
public static class UnknownRelicActionCounterPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerChoiceContext choiceContext, CardModel card, Creature? target)
    {
        if (card?.Owner == null) return;

        // 联机模式下关闭 Action 级计数，改用回合开始/结束固定 +20（在遗物类中实现）
        if (RunManager.Instance?.NetService?.Type.IsMultiplayer() == true)
            return;

        // 只处理 Theresa 玩家
        var player = card.Owner;
        if (player?.Character?.Id?.Entry != Theresa.TheresaCode.Character.Theresa.CharacterId)
            return;

        // 查找 UnknownRelic
        var unknownRelic = player.Relics.FirstOrDefault(r => r is UnknownRelic) as UnknownRelic;
        if (unknownRelic == null)
            return;

        // 增加计数
        unknownRelic.IncrementActionCount();
    }

    /// <summary>
    /// 初始化补丁（Harmony 补丁会自动注册，此方法仅用于兼容旧调用）
    /// </summary>
    public static void Initialize()
    {
        // Harmony 补丁通过特性自动注册，无需手动初始化
    }

    /// <summary>
    /// 卸载补丁（Harmony 补丁会自动处理，此方法仅用于兼容旧调用）
    /// </summary>
    public static void Deinitialize()
    {
        // Harmony 补丁通过特性自动处理，无需手动卸载
    }
}
