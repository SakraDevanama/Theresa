using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 过往尘埃
/// 1费能力牌
/// 每消耗一个MantraPower，获得2（升级后+1）点格挡
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DustOfThePast() : TheresaCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [LingerKeyword.Linger];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
    ];

    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(2m, ValueProp.Move) // 基础格挡值2，升级后+1
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出时施加"过往尘埃"效果Power
        if (Owner?.Creature != null)
        {
            await PowerCmd.Apply<DustOfThePastEffect>(Owner.Creature, 1, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后格挡值 2 -> 1
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}

/// <summary>
/// 过往尘埃 - 效果实现
/// 监听MantraPower的消耗，每消耗一层获得格挡
/// </summary>
public sealed class DustOfThePastEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None; // 不叠加，只存在有无

    // 内部隐藏：不在UI上显示这个能力图标
    protected override bool IsVisibleInternal => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2m, ValueProp.Move)];

    private decimal _lastMantraAmount = 0m;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // 从来源卡牌同步 Block 值到 Power 的动态变量，用于本地化显示
        if (cardSource?.DynamicVars.TryGetValue("Block", out var blockVar) == true)
        {
            base.DynamicVars["Block"].BaseValue = blockVar.BaseValue;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 当自身被应用时（首次获得此Power），初始化记录的MantraPower层数
        if (power == this && amount > 0 && _lastMantraAmount == 0m)
        {
            var mantraPower = Owner?.Powers.OfType<MantraPower>().FirstOrDefault();
            _lastMantraAmount = mantraPower?.Amount ?? 0m;
            return;
        }

        // 只关注MantraPower的变化
        if (power is not MantraPower) return;
        
        // 只关注持有者自己的MantraPower
        if (power.Owner != Owner) return;

        // 计算变化量（减少即为消耗）
        decimal change = amount - _lastMantraAmount;
        _lastMantraAmount = amount;

        // 如果是减少（消耗），则获得格挡
        if (change < 0)
        {
            // 获取触发来源的卡牌（如果有）
            var sourceCard = cardSource;
            
            // 计算消耗的层数（绝对值）
            int consumedAmount = (int)(-change);
            
            // 获取格挡值：优先使用来源卡牌的动态变量，否则使用默认值2
            decimal blockPerConsume = 2m;
            if (sourceCard?.DynamicVars.TryGetValue("Block", out var blockVar) == true)
            {
                blockPerConsume = blockVar.BaseValue;
            }
            
            // 计算总格挡
            decimal totalBlock = blockPerConsume * consumedAmount;
            
            if (totalBlock > 0 && Owner != null)
            {
                await CreatureCmd.GainBlock(Owner, new BlockVar(totalBlock, ValueProp.Unpowered), null);
            }
        }
    }
}
