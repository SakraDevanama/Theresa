using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 明日渺远不及
/// 2费普通攻击
/// 造成6点伤害。
/// 未升级：微尘中每有1张基础牌，将其消耗，并萦绕1次（最多3次）。
/// 升级后：微尘中每有1张消耗牌，萦绕1次（最多3次）。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class UnseenFuture() : TheresaCardModel(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        DustKeyword.Dust,
        LingerKeyword.Linger,
        DivinityStanceKeyword.DivinityStance,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(DustKeyword.Dust),
        HoverTipFactory.FromKeyword(LingerKeyword.Linger),
        HoverTipFactory.FromKeyword(DivinityStanceKeyword.DivinityStance),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 微尘效果
        await ProcessDustEffect();
    }

    private async Task ProcessDustEffect()
    {
        if (Owner == null) return;

        var dustCards = DustManager.Cards.Where(c => c.Owner == Owner).ToList();
        if (dustCards.Count == 0) return;

        int dustCount = 0;

        if (!IsUpgraded)
        {
            // 未升级：给微尘中的基础牌添加消耗，每有1张基础牌萦绕1次（最多3次）
            foreach (var card in dustCards)
            {
                if (card.Rarity == CardRarity.Basic)
                {
                    if (!card.Keywords.Contains(CardKeyword.Exhaust))
                    {
                        card.AddKeyword(CardKeyword.Exhaust);
                    }
                    dustCount++;
                }
            }
        }
        else
        {
            // 升级后：微尘中每有1张消耗牌，萦绕1次（最多3次）
            foreach (var card in dustCards)
            {
                if (card.Keywords.Contains(CardKeyword.Exhaust))
                {
                    dustCount++;
                }
            }
        }

        // 最多3次
        if (dustCount > 3)
            dustCount = 3;

        // 执行萦绕
        for (int i = 0; i < dustCount; i++)
        {
            await DustManager.DustIt(false, false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
