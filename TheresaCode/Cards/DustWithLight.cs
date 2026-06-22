using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 和光同尘 (DustWithLight) 
/// 1费能力牌，稀有稀有度
/// 
/// 效果：获得 !M! 个微尘上限。抽 !M! 张牌。
/// 升级：数值+1
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DustWithLight() : TheresaCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const int BaseAmount = 2;
    private const int UpgradeDelta = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Amount", BaseAmount)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        int amount = (int)DynamicVars["Amount"].BaseValue;

        // 1. 增加微尘上限
        DustManager.IncreaseMaxDust(amount, Owner);

        // 2. 抽牌
        await CardPileCmd.Draw(choiceContext, amount, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(UpgradeDelta);
    }
}
