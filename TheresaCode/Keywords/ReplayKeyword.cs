using System.Collections.Generic;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Keywords;

public static class ReplayKeyword
{
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Replay;

    public static bool IsReplay(this CardModel card) => card.Keywords.Contains(Replay);

    private static readonly HashSet<string> _replayedThisCombat = new();

    public static void OnTurnStart() => _replayedThisCombat.Clear();

    public static bool HasBeenReplayedThisCombat(this CardModel card) 
        => _replayedThisCombat.Contains(GetCardKey(card));

    public static void MarkReplayed(this CardModel card)
    {
        _replayedThisCombat.Add(GetCardKey(card));
    }

    private static string GetCardKey(CardModel card) => $"{card.Id.Entry}_{card.GetHashCode()}";
}