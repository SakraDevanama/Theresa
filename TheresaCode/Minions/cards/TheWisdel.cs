using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Commands;
using MinionLib.Minion;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Minions.Models;
using Theresa.TheresaCode.Minions.Powers;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Minions.Cards;

/// <summary>
/// 祖宗发射器：维什戴尔 (TheWisdel)
/// 2费能力牌
/// 消耗2点微尘。召唤维什戴尔。
/// 维什戴尔每回合自动对随机敌人造成9点伤害。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class TheWisdel() : TheresaCardModel(3, CardType.Quest, CardRarity.Event, TargetType.Self)
{
    private const int DustCost = 2;
    private const int BaseHp = 25;
    private const int UpgradedHp = 30;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [ 
        CardKeyword.Retain,
        CardKeyword.Exhaust,
        LingerKeyword.Linger,
        DimKeyword.Dim,
    ];
    // 提示文本：微尘
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<WisdelYuZhenPower>(),
        HoverTipFactory.FromPower<WisdelCanYingPower>(),
        HoverTipFactory.FromPower<WisdelDawnChargePower>(),
        HoverTipFactory.FromCard<BurstDawnCard>(),
    ];
    /// <summary>
    /// Sovereign 使用自定义 Spine 动画场景作为卡面
    /// </summary>
    public override string? CustomSpinePortraitScenePath => "res://Theresa/animations/cards/THE_WISDEL_BG.tscn";
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DustCost", DustCost),
        new DynamicVar("WisdelHp", IsUpgraded ? UpgradedHp : BaseHp),
        new DynamicVar("WisdelDamage", 9)
    ];

    /// <summary>
    /// 检查是否可打出：需要至少3个微尘
    /// </summary>
    protected override bool IsPlayable =>
        Owner?.Creature?.Powers.OfType<MantraPower>().Sum(p => (int)p.Amount) >= DustCost;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner?.Creature;
        if (owner == null) return;

        // 消耗3个微尘
        var mantraPower = owner.Powers.OfType<MantraPower>().FirstOrDefault();
        if (mantraPower != null)
        {
            await PowerCmd.Apply<MantraPower>(new ThrowingPlayerChoiceContext(), owner, -DustCost, owner, this);
        }

        // 召唤维什戴尔（升级后30生命，未升级25生命）
        var hp = IsUpgraded ? UpgradedHp : BaseHp;
        _ = await MinionCmd.AddMinion<WisdelMinion>(Owner, new MinionSummonOptions(
            hp,   // 生命
            0m,   // 不需要额外力量，伤害固定为9点
            Source: this,
            Position: MinionPosition.Front));
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
