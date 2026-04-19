using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Cards;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 监听 NightBeforeWar 的消耗事件，触发其特殊效果。
/// 替代原 RitsuLib 的 SubscribeLifecycle&lt;CardExhaustedEvent&gt;。
/// </summary>
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Exhaust))]
public static class NightBeforeWarExhaustPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel card)
    {
        if (card is NightBeforeWar nightBeforeWar && nightBeforeWar.IsAwaitingExhaust)
        {
            nightBeforeWar.OnExhausted();
        }
    }
}
