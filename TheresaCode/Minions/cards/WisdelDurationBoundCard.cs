using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Minions.Powers;
using Theresa.TheresaCode.Minions.Interfaces;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Minions.Cards;

/// <summary>
/// 维什戴尔绑定卡：延续
/// 1费 Token 技能牌
/// 消耗2微尘，给予绑定的维什戴尔3层持续
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class WisdelDurationBoundCard()
    : TheresaCardModel(1, CardType.Skill, CardRarity.Token, TargetType.Self), IMinionBoundCard
{
    
    private const bool shouldShowInCardLibrary = false;
    private const int DustCost = 2;
    private const int DurationAmount = 3;

    // IMinionBoundCard 接口实现
    public uint? BoundMinionCombatId { get; set; }
    public string? BoundMinionNameSnapshot { get; set; }

    // 如果绑定的随从已死亡，框变为红色
    protected override bool ShouldGlowRedInternal => this.ResolveBoundMinion() is not { IsAlive: true };

    // 卡牌关键词：消耗、保留、消耗微尘
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ 
        CardKeyword.Retain,
        CardKeyword.Exhaust,
        CardKeyword.Innate,
        LingerKeyword.Linger,
        DimKeyword.Dim];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DustCost", DustCost),
        new DynamicVar("DurationAmount", DurationAmount)
    ];

    // 提示文本：微尘
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
    ];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        this.AddBoundNameToDescription(description);
    }

    /// <summary>
    /// 检查是否可打出：需要至少2个微尘
    /// </summary>
    protected override bool IsPlayable =>
        Owner?.Creature?.Powers.OfType<MantraPower>().Sum(p => (int)p.Amount) >= DustCost;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner?.Creature;
        if (owner == null) return;

        // 解析绑定的维什戴尔
        var minion = this.ResolveBoundMinion();
        if (minion is not { IsAlive: true }) return;

        // 消耗2个微尘
        var mantraPower = owner.Powers.OfType<MantraPower>().FirstOrDefault();
        if (mantraPower != null)
        {
            await PowerCmd.Apply<MantraPower>(owner, -DustCost, owner, this);
        }

        // 给予维什戴尔3层持续
        await PowerCmd.Apply<WisdelSummonDurationPower>(minion, DurationAmount, owner, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后消耗1个微尘
        DynamicVars["DustCost"].UpgradeValueBy(-1);
    }
}
