using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;


[Pool(typeof(TheresaCardPool))]
public sealed class FinalCountdown() : TheresaCardModel(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<TheresiasHopePower>(),
        HoverTipFactory.FromPower<ZaakathHatePower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 获取当前希望层数
        var hopePower = Owner.Creature?.Powers.FirstOrDefault(p => p is TheresiasHopePower) as TheresiasHopePower;
        int hopeAmount = hopePower?.Amount ?? 0;

        if (hopeAmount <= 0 || hopePower == null) return;

        // 获取或创建恨意Power
        var hatePower = Owner.Creature?.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower;
        
        // 逐层转化：每移除1层希望，获得1层恨意
        for (int i = 0; i < hopeAmount; i++)
        {
            // 移除1层希望
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), hopePower, -1, Owner.Creature, this);
            
            // 获得1层恨意
            if (hatePower == null)
            {
                // 第一次：创建恨意Power并获得1层
                await PowerCmd.Apply<ZaakathHatePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
                // 创建后重新获取引用
                hatePower = Owner.Creature?.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower;
            }
            else
            {
                // 已有恨意Power：增加1层
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), hatePower, 1, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后能量消耗变为0
        EnergyCost.UpgradeBy(-1);
    }
}
