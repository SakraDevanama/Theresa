using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 花茧
/// 能量：2费
/// 效果：
/// 1. 打出后判断：敌人每有2层茧缚就抽一张牌
/// 2. 如果在神威姿态状态下就免费打出
/// </summary>

[Pool(typeof(TheresaCardPool))]
public sealed class FlowerCocoon() : TheresaCardModel(baseCost: 2, type: CardType.Skill, rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<SilkCocoon>(),
        HoverTipFactory.FromPower<Broken>(),
    ];
    
    
    // 动态变量：每X层茧缚抽一张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("SilkCocoonThreshold", 2m),  // 基础值：每2层抽1张
            new CardsVar(1)
        ];

    // 基础费用2
    // 技能牌
    // 稀有度：罕见
    // 目标：自身

    /// <summary>
    /// 检查是否在神威姿态下，如果是则本回合免费
    /// </summary>
    protected override bool IsPlayable
    {
        get
        {
            // 检查是否在神威姿态
            if (IsInDivinityStance())
            {
                // 在神威姿态下，设置本回合免费
                SetToFreeThisTurn();
            }
            return base.IsPlayable;
        }
    }

    /// <summary>
    /// 检查当前是否在神威姿态
    /// </summary>
    private bool IsInDivinityStance()
    {
        if (Owner?.Creature == null) return false;
        return Owner.Creature.Powers.OfType<DivinityStance>().Any();
    }

    /// <summary>
    /// 卡牌打出时的效果
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 获取所有敌人
        var enemies = combatState.GetOpponentsOf(Owner.Creature).ToList();
        
        // 计算所有敌人的茧缚层数总和
        int totalSilkCocoonStacks = 0;
        foreach (var enemy in enemies)
        {
            totalSilkCocoonStacks += enemy.GetPowerAmount<SilkCocoon>();
        }

        // 每X层茧缚抽一张牌
        int threshold = (int)DynamicVars["SilkCocoonThreshold"].BaseValue;
        int cardsToDraw = totalSilkCocoonStacks / threshold;
        
        if (cardsToDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, cardsToDraw, Owner);
        }
    }

    /// <summary>
    /// 卡牌升级效果
    /// </summary>
    protected override void OnUpgrade()
    {
        // 升级后：每1层茧缚就抽一张牌（更强力）
        // 阈值从2降到1
        DynamicVars["SilkCocoonThreshold"].UpgradeValueBy(-1m);
    }
}
