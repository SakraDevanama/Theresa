using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 宽恕 (Forgive)
/// 1费技能牌，稀有度 Uncommon
/// 
/// 保留。消耗。
/// 获得 1 层希望，共 MagicNumber 次。
/// 你每次失去生命，这张牌的次数增加 1。
/// 
/// 对应原版 Java 的 Forgive 卡牌。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class Forgive()
    : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Retain,
        CardKeyword.Exhaust,
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TheresiasHopePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MagicNumber", 2m)
    ];

    /// <summary>
    /// 增加 MagicNumber（受到伤害时调用）
    /// </summary>
    public void IncrementMagicNumber()
    {
        DynamicVars["MagicNumber"].BaseValue++;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        int times = (int)DynamicVars["MagicNumber"].BaseValue;

        // 获得希望，共 MagicNumber 次
        for (int i = 0; i < times; i++)
        {
            await PowerCmd.Apply<TheresiasHopePower>(choiceContext, new[] { Owner.Creature }, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }

    /// <summary>
    /// 战斗开始时确保追踪器存在
    /// </summary>
    public override Task BeforeCombatStart()
    {
        EnsureTracker();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 当卡牌进入手牌时，确保玩家身上有 ForgiveTrackerPower
    /// </summary>
    public override Task OnGlobalMove(PileType from, PileType to, AbstractModel? source)
    {
        if (to == PileType.Hand && Owner?.Creature != null)
        {
            EnsureTracker();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 确保玩家身上有 ForgiveTrackerPower
    /// </summary>
    private void EnsureTracker()
    {
        if (Owner?.Creature == null) return;

        var tracker = Owner.Creature.Powers.FirstOrDefault(p => p is ForgiveTrackerPower);
        if (tracker == null)
        {
            _ = CreateTrackerPower();
        }
    }

    private async Task CreateTrackerPower()
    {
        if (Owner?.Creature == null) return;
        await PowerCmd.Apply<ForgiveTrackerPower>(new ThrowingPlayerChoiceContext(), new[] { Owner.Creature }, 1, Owner.Creature, null);
    }
}
