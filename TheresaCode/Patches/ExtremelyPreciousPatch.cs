using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 为ExtremelyPrecious卡牌记录打出前在手牌中的位置
/// </summary>
public static class ExtremelyPreciousPositionTracker
{
    // 存储每个玩家最近打出的ExtremelyPrecious在手牌中的索引
    public static readonly Dictionary<Player, int> LastPlayedPosition = new();
}

/// <summary>
/// 在CardPileCmd.AddDuringManualCardPlay执行前记录ExtremelyPrecious在手牌中的位置
/// 这是手动出牌时调用的方法
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddDuringManualCardPlay))]
public static class CardPileCmd_AddDuringManualCardPlay_Patch
{
    public static void Prefix(CardModel card)
    {
        // 只处理ExtremelyPrecious卡牌 (注意ID格式是 THERESA-EXTREMELY_PRECIOUS)
        if (card.Id.Entry != "THERESA-EXTREMELY_PRECIOUS")
        {
            return;
        }
        
        var owner = card.Owner;
        if (owner == null) return;
        
        var handPile = PileType.Hand.GetPile(owner);
        if (handPile?.Cards == null) return;
        
        // 记录这张牌在手牌中的索引
        var handCards = handPile.Cards.ToList();
        int index = handCards.IndexOf(card);
        
        if (index >= 0)
        {
            ExtremelyPreciousPositionTracker.LastPlayedPosition[owner] = index;
        }
    }
}
