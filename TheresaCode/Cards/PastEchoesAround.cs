using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 往昔萦绕身旁
/// 2费（升级后1费）能力牌
/// 在你回合结束时，给予你当前微尘层数同等的忘却
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class PastEchoesAround() : TheresaCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [LingerKeyword.Linger];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
        HoverTipFactory.FromPower<OblivionPower>(),
    ];
    
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出时施加效果 Power
        if (Owner?.Creature != null)
        {
            await PowerCmd.Apply<PastEchoesAroundEffect>(Owner.Creature, 1, Owner.Creature, this);
        }
    }
    
    

    protected override void OnUpgrade()
    {
        // 升级后费用 2 -> 1
        EnergyCost.UpgradeBy(-1);
    }
}

/// <summary>
/// 往昔萦绕身旁 - 效果实现
/// 回合结束时根据当前 MantraPower 层数给予 OblivionPower
/// </summary>
public sealed class PastEchoesAroundEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None; // 不叠加，只存在有无

    // 内部隐藏：不在 UI 上显示这个能力图标
    protected override bool IsVisibleInternal => true;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 确保是拥有此 Power 的生物回合结束了
        if (Owner?.Side != side) return;

        // 获取当前 MantraPower 层数
        var mantraPower = Owner.Powers.OfType<MantraPower>().FirstOrDefault();
        int mantraAmount = mantraPower?.Amount ?? 0;

        // 根据 MantraPower 层数给予 OblivionPower
        if (mantraAmount > 0)
        {
            await PowerCmd.Apply<OblivionPower>(
                Owner,
                mantraAmount,
                Owner,
                null
            );
        }
    }
}
