using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 启奏 (Petition)
/// 0费攻击牌
/// 消耗。给予目标6层凋亡，3层茧缚。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class Petition() : TheresaCardModel(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<ApoptosisPower>(),
        HoverTipFactory.FromPower<SilkCocoon>(),
        HoverTipFactory.FromPower<Broken>(),
    ];
    
    
    // 基础凋亡层数
    private const int BaseApoptosis = 6;
    // 基础虚弱层数
    private const int BaseCocoon = 1;
    // 升级后凋亡增加
    private const int UpgradeApoptosisBonus = 2;
    // 升级后虚弱增加
    private const int UpgradeCocoonBonus = 1;

    // 添加消耗关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust,
        CardKeyword.Ethereal,
        DimKeyword.Dim];

    // 定义自定义变量，用于在卡面上显示数值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Apoptosis", BaseApoptosis),
        new DynamicVar("Cocoon", BaseCocoon)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 获取目标敌人
        var target = cardPlay.Target as Creature;
        if (target == null || !target.IsAlive) return;

        // 给予凋亡 - 使用DynamicVars中的值
        await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), target, (int)DynamicVars["Apoptosis"].BaseValue, Owner.Creature, this);
        
        // 给予茧缚 - 使用DynamicVars中的值
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), target, (int)DynamicVars["Cocoon"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后凋亡+2，茧缚+1
        DynamicVars["Apoptosis"].UpgradeValueBy(UpgradeApoptosisBonus);
        DynamicVars["Cocoon"].UpgradeValueBy(UpgradeCocoonBonus);
    }
}
