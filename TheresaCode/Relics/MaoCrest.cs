using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 动态变量：实时显示当前 MaxDust
/// </summary>
public class MaxDustVar() : DynamicVar("MaxDust", 0m)
{
    protected override decimal GetBaseValueForIConvertible()
    {
        // 在角色选择等场景中 relic 可能还是 Canonical（不可变）模型，此时无法访问 Owner。
        if (_owner is RelicModel relic && relic.IsMutable && relic.Owner != null)
            return DustManager.MaxDust(relic.Owner);
        return DustManager.MaxDust();
    }

    public override string ToString()
    {
        return GetBaseValueForIConvertible().ToString();
    }
}

[Pool(typeof(TheresaRelicPool))]
public sealed class MaoCrest : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new MaxDustVar()];

    public override Task AfterObtained()
    {
        // 确保微尘上限为 3（重置任何之前的修改）
        if (Owner != null)
            DustManager.ResetMaxDust(Owner);
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player == null || player.NetId != Owner.NetId)
            return;

        const int totalDraws = 3;
        const int maxMantraConversions = 2;
        int mantraConverted = 0;

        for (int i = 0; i < totalDraws; i++)
        {
            // 若 Dust 已满且还未达转化上限，将此次抽牌转化为 MantraPower
            if (DustManager.IsFull(player) && mantraConverted < maxMantraConversions)
            {
                await PowerCmd.Apply<MantraPower>(new ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null);
                mantraConverted++;
            }
            else
            {
                await CardPileCmd.Draw(choiceContext, 1, player);
            }
        }

        // 抽牌完成后触发 Dust 萦绕
        await DustManager.AtTurnStartPostDraw(player);

        Flash();
    }
        
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<BabelWord>();
}
