using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 倾诉命运的怒意
/// 1费（升级后0费）技能牌
/// 每损失10点生命值（升级后9点），本回合的攻击牌增加2点伤害
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class VentFateAnger : TheresaCardModel
{
    // 标记本回合效果是否激活
    private bool _effectActive;
    // 记录当前阈值
    private int _currentThreshold;

    public VentFateAnger() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpThreshold", 10m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 激活效果
        _effectActive = true;
        _currentThreshold = (int)DynamicVars["HpThreshold"].BaseValue;
        
        // 触发视觉效果或音效可以在这里添加
        await Task.CompletedTask;
    }

    /// <summary>
    /// 修改伤害 - 当效果激活时，根据已损失生命值增加攻击牌伤害
    /// </summary>
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 效果未激活，不修改伤害
        if (!_effectActive) return 0m;
        
        // 不是攻击牌，不修改
        if (cardSource?.Type != CardType.Attack) return 0m;
        
        // 不是伤害施加者，不修改
        if (Owner?.Creature != dealer) return 0m;
        
        // 不是 powered 攻击，不修改 (ValueProp.Move 且没有 Unpowered 标记)
        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered)) return 0m;

        // 计算已损失的生命值
        if (Owner?.Creature == null) return 0m;
        
        int maxHp = Owner.Creature.MaxHp;
        int currentHp = Owner.Creature.CurrentHp;
        int hpLost = maxHp - currentHp;

        if (hpLost <= 0 || _currentThreshold <= 0) return 0m;

        // 每损失_currentThreshold点生命值，增加2点伤害
        int bonusDamage = (hpLost / _currentThreshold) * 2;
        return bonusDamage;
    }

    /// <summary>
    /// 回合结束时清除效果
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 如果是玩家回合结束，清除效果
        if (side == Owner?.Creature.Side && _effectActive)
        {
            _effectActive = false;
        }
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        // 升级后费用变为0
        EnergyCost.UpgradeBy(-1);
        // 升级后阈值从10变为9
        DynamicVars["HpThreshold"].UpgradeValueBy(-1m);
    }
}
