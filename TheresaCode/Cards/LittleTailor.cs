using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Enchantments;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 小衣匠
/// 1费技能牌
/// 普通
/// 获得7（+3）点格挡。
/// 对随机手牌编织：茧笼。
/// 给予所有敌人1层茧缚。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class LittleTailor() : TheresaCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        SilkKeyword.Silk,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(SilkKeyword.Silk),
        HoverTipFactory.FromPower<SilkSpreadPower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 确保玩家有丝线传播能力
        if (Owner?.Creature != null)
        {
            var spreadPower = Owner.Creature.GetPower<SilkSpreadPower>();
            if (spreadPower == null)
            {
                await PowerCmd.Apply<SilkSpreadPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
            }
        }

        // 对随机手牌编织：丝线（茧笼）
        await EnchantRandomHandCard();
        
        // 立即验证手牌中的丝线状态
        var verifyHand2 = PileType.Hand.GetPile(Owner);
        if (verifyHand2 != null)
        {
            GD.Print($"[LittleTailor] After enchant, hand has {verifyHand2.Cards.Count} cards:");
            foreach (var c in verifyHand2.Cards)
            {
                GD.Print($"[LittleTailor]   {c.Id} enchantment={c.Enchantment?.GetType().Name ?? "null"}");
            }
        }

        // 给予所有敌人1层茧缚
        var creatureCombatState = Owner.Creature.CombatState;
        if (creatureCombatState != null)
        {
            var opponents = creatureCombatState.GetOpponentsOf(Owner.Creature).ToList();
            if (opponents.Any())
            {
                await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), opponents, 1, Owner.Creature, this);
            }
        }
    }

    private async Task EnchantRandomHandCard()
    {
        if (Owner == null) return;

        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null || !hand.Cards.Any()) return;

        // 获取可被编织的手牌（排除已有丝线附魔的）
        var candidates = hand.Cards
            .Where(c => c.Enchantment is not SilkThreadEnchantment)
            .ToList();

        GD.Print($"[LittleTailor] Hand has {hand.Cards.Count} cards, {candidates.Count} candidates for silk");

        if (!candidates.Any()) return;

        // 随机选择一张
        var rng = Owner.RunState.Rng.Shuffle;
        var targetCard = candidates[rng.NextInt(candidates.Count)];

        GD.Print($"[LittleTailor] Selected card {targetCard.Id} for silk enchantment");

        // 添加丝线（茧笼）附魔
        try
        {
            var enchantmentId = ModelDb.GetId<SilkThreadEnchantment>();
            GD.Print($"[LittleTailor] SilkThreadEnchantment ID: {enchantmentId}");
            var enchantmentPrototype = ModelDb.GetById<EnchantmentModel>(enchantmentId);
            GD.Print($"[LittleTailor] Got prototype: {enchantmentPrototype?.Id}");
            var enchantment = (EnchantmentModel)enchantmentPrototype.MutableClone();
            CardCmd.Enchant(enchantment, targetCard, 1);
            GD.Print($"[LittleTailor] Enchantment applied! Target card ref: {targetCard.GetHashCode()}, enchantment: {targetCard.Enchantment?.GetType().Name}");
            
            // 验证手牌中同ID卡的附魔状态
            var verifyHand = PileType.Hand.GetPile(Owner);
            if (verifyHand != null)
            {
                foreach (var c in verifyHand.Cards)
                {
                    if (c.Id == targetCard.Id)
                    {
                        GD.Print($"[LittleTailor] Verify hand card {c.Id} ref={c.GetHashCode()} enchantment={c.Enchantment?.GetType().Name}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LittleTailor] Enchant failed: {ex}");
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
