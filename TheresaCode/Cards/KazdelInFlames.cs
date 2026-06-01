using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 战火中的卡兹戴尔
/// 1费
/// 打出后开始记录玩家打出CardType.Attack的牌
/// 每打出CardType.Attack的牌就获得1个ZaakathHatePower
/// </summary>
[Pool(typeof(TheresaCardPool))]
public class KazdelInFlames() : TheresaCardModel(1, CardType.Power, CardRarity.Common, TargetType.None)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<ZaakathHatePower>(),
    ];
    
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 应用战火中的卡兹戴尔能力，开始监听攻击牌
        await PowerCmd.Apply<KazdelInFlamesEffect>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
    
}

/// <summary>
/// 战火中的卡兹戴尔 - 效果实现
/// </summary>

public class KazdelInFlamesEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    
    protected override bool IsVisibleInternal => true;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // 检查是否是持有者打出的攻击牌
        if (cardPlay.Card.Owner.Creature != Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        // 获得1层恨意
        await PowerCmd.Apply<ZaakathHatePower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
    }

    // public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    // {
    //    // 玩家回合结束后移除该效果
    //    if (Owner?.Side == side)
    //   {
    //        await PowerCmd.Remove(this);
    //    }
    //  }
}
