using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

[Pool(typeof(TheresaCardPool))]
public class EternalDust() : TheresaCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
    ];
           
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, LingerKeyword.Linger];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("Damage", 2m, ValueProp.Move),
        new MantraHitsVar(),
    ];
        
        protected override bool IsPlayable
        
        {
            get
            {
                var wood = Owner.Creature.Powers.OfType<MantraPower>().FirstOrDefault()?.Amount ?? 0;
                var stone = Owner.Creature.Powers.OfType<MantraPower>().FirstOrDefault()?.Amount ?? 0;
                return wood + stone > 0;
            }
        }

        protected override async Task OnPlay(
            PlayerChoiceContext choiceContext,
            CardPlay play)
        {
            var owner = Owner.Creature;
            if (owner.CombatState is not { } combatState) return;

            var woodPower = owner.Powers.OfType<MantraPower>().FirstOrDefault();
            var stonePower = owner.Powers.OfType<MantraPower>().FirstOrDefault();
            var woodAmount = woodPower?.Amount ?? 0;
            var stoneAmount = stonePower?.Amount ?? 0;
            var totalHits = woodAmount + stoneAmount;

            if (totalHits <= 0) return;

            if (woodPower != null && woodAmount > 0)
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), woodPower, -(decimal)woodAmount, null, this, false);
            if (stonePower != null && stoneAmount > 0)
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), stonePower, -(decimal)stoneAmount, null, this, false);

            var attackCmd = await DamageCmd.Attack(DynamicVars["Damage"].BaseValue)
                .WithHitCount(totalHits)
                .FromCard(this)
                .TargetingRandomOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 升级后每次命中赋予目标一层 SilkCocoon
            if (IsUpgraded)
            {
                foreach (var hitResults in attackCmd.Results)
                {
                    foreach (var result in hitResults)
                    {
                        if (result.Receiver.IsAlive)
                        {
                            await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), result.Receiver, 1m, owner, this);
                        }
                    }
                }
            }
        }

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Retain);
        }

        private static decimal CalcDamage(CardModel? card)
        {
            if (card == null) return 4m;
            if (!card.DynamicVars.TryGetValue("Damage", out var dynamicVar)) return 4m;

            var creature = card?.Owner.Creature;
            if (creature == null) return dynamicVar.BaseValue;

            var hits = CalcHits(card);
            return hits > 0
                ? dynamicVar.BaseValue * hits
                : dynamicVar.BaseValue;
        }

        private static decimal CalcHits(CardModel? card)
        {
            var creature = card?.Owner.Creature;
            if (creature == null) return 0m;

            var wood = creature.Powers.OfType<MantraPower>().FirstOrDefault()?.Amount ?? 0;
            var stone = creature.Powers.OfType<MantraPower>().FirstOrDefault()?.Amount ?? 0;
            return wood + stone;
        }
}

/// <summary>
/// 基于当前 MantraPower 层数计算命中次数的动态变量。
/// 替代原 RitsuLib 的 ModCardVars.Computed。
/// </summary>
public class MantraHitsVar() : DynamicVar("Hits", 0m)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        PreviewValue = GetMantraAmount(card);
    }

    protected override decimal GetBaseValueForIConvertible()
    {
        if (_owner is CardModel card)
            return GetMantraAmount(card);
        return BaseValue;
    }

    private static decimal GetMantraAmount(CardModel? card)
    {
        var creature = card?.Owner?.Creature;
        if (creature == null) return 0m;

        var wood = creature.Powers.OfType<MantraPower>().FirstOrDefault()?.Amount ?? 0;
        var stone = creature.Powers.OfType<MantraPower>().FirstOrDefault()?.Amount ?? 0;
        return wood + stone;
    }
}
