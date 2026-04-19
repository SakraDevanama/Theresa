using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using Theresa.TheresaCode.Commands;


namespace Theresa.TheresaCode.Powers;


public class Test : TheresaPowerModel
{
    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Buff;
    // 叠加类型，Counter表示可叠加，Single表示不可叠加
    public override PowerStackType StackType => PowerStackType.Counter;
    

    // 自定义图标路径，自己指定，或者创建一个基类来统一指定图标路径
    //public override string? CustomPackedIconPath => "res://test/powers/test_power.png";
    //public override string? CustomBigIconPath => "res://test/powers/test_power.png";
    // 当该能力的层数（Amount）发生变化时触发（例如获得1层真言）
    public override async Task AfterPowerAmountChanged(
        PowerModel power,           // 发生变化的能力实例
        decimal amount,             // 变化后的总层数
        Creature? applier,          // 施加该能力的来源生物（通常是玩家自己）
        CardModel? cardSource)      // 触发该变化的卡牌（可能为 null）
    {
        // 获取当前能力拥有者对应的玩家对象（用于后续进入姿态）
        var player = Owner.Player;
        // 安全检查：如果以下任意条件不满足，就直接返回，不执行后续逻辑
        // - 当前能力不是 MantraPower（理论上不会发生，但保险起见）
        // - 真言层数 ≤ 0（没有意义）
        // - 能力不是由自己施加的（防止敌人或其他来源误触发）
        // - 玩家对象为空（异常情况）
        if (power is not Test || amount <= 0 || applier != Owner || player == null)
            return;
        // 计算当前真言层数能触发多少次“姿态转换”：
        // 每积累 10 层真言，就会触发一次进入“神威”（Divinity）姿态
        var triggers = Amount / 3;

        // 如果不足 10 层，不触发任何事，直接返回
        if (triggers <= 0) return;

        // 计算总共要消耗的真言层数（例如 23 层 → triggers=2 → 消耗 20 层）
        var totalCost = triggers * 3m;
        await StanceCmd.EnterDivinity(player.Creature, cardSource);
    }
    // 每回合开始时抽1张牌
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);
    }
}



