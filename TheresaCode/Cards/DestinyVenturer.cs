using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 命运的抉择 (DestinyVenturer)
/// 2费技能牌 / 罕见
/// 自身当前生命值低于10（含10）则获得5（+2）层希望；
/// 自身当前生命值高于11（含11）则获得5（+2）层茧缚。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DestinyVenturer() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 基础层数
    private const int BaseAmount = 7;
    // 升级后增加
    private const int UpgradeAmountBonus = 3;
    // 生命值阈值
    private const int HpThreshold = 10;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TheresiasHopePower>(),
        HoverTipFactory.FromPower<SilkCocoon>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Amount", BaseAmount),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        int currentHp = Owner.Creature.CurrentHp;
        int amount = (int)DynamicVars["Amount"].BaseValue;

        // 生命值 ≤ 10：获得希望
        if (currentHp <= HpThreshold)
        {
            await PowerCmd.Apply<TheresiasHopePower>(new ThrowingPlayerChoiceContext(), 
                Owner.Creature,
                amount,
                Owner.Creature,
                this
            );
        }
        // 生命值 ≥ 11：获得茧缚
        else if (currentHp >= HpThreshold + 1)
        {
            await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), 
                Owner.Creature,
                amount,
                Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后层数+2（5 → 7）
        DynamicVars["Amount"].UpgradeValueBy(UpgradeAmountBonus);
    }
}
