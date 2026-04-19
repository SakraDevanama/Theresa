using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 意志（MindPower）
/// 
/// 用于跟踪 MindSilk 的缓冲次数（paddingRemains）。
/// 对应原版 Java 的 MindPower。
/// 
/// 当玩家打出卡牌时，重置 MindSilk 的缓冲次数。
/// </summary>
public class MindPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 不显示图标（隐藏Power）
    protected override bool IsVisibleInternal => false;
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // 打出卡牌时重置 MindSilk 的缓冲次数
        MindSilkEnchantment.ResetPaddingRemains();
        return Task.CompletedTask;
    }
}
