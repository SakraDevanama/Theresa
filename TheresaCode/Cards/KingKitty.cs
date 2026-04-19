using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Enchantments;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 魔王猫咪 (KingKitty)
/// 1费攻击牌，罕见稀有度
/// 
/// 效果：造成 {Damage:diff()} 点伤害。对目标敌人施加 !M! 层茧缚。
/// 对随机微尘牌编织：茧笼。
/// 升级：伤害+4，茧缚层数+3
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class KingKitty() : TheresaCardModel(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move), new PowerVar<SilkCocoon>(5)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<SilkCocoon>(),
        HoverTipFactory.FromPower<Broken>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 1. 对目标敌人造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        // 2. 对目标敌人施加 SilkCocoon（茧缚）
        await PowerCmd.Apply<SilkCocoon>(
            cardPlay.Target,
            DynamicVars["SilkCocoon"].BaseValue,
            Owner.Creature,
            this
        );

        // 3. 对随机微尘牌编织：茧笼
        WeaveCocoonOnRandomDust();
    }

    /// <summary>
    /// 对随机微尘牌编织茧笼丝线
    /// 优先选择没有丝线的牌，如果都有丝线则随机替换
    /// </summary>
    private void WeaveCocoonOnRandomDust()
    {
        if (Owner == null) return;

        var dustCards = DustManager.Cards.Where(c => c.Owner == Owner).ToList();
        if (dustCards.Count == 0) return;

        // 创建茧笼附魔实例（使用 ModelDb 获取可变副本）
        var cocoonEnchantment = ModelDb.Enchantment<SilkThreadEnchantment>().ToMutable();

        // 优先选择没有丝线的牌
        var cardsWithoutSilk = dustCards
            .Where(c => c.Enchantment == null || c.Enchantment is not SilkThreadEnchantment)
            .ToList();

        CardModel? targetCard = null;

        if (cardsWithoutSilk.Count > 0)
        {
            // 随机选择一张没有丝线的牌
            var rng = Owner.RunState.Rng.Shuffle;
            targetCard = cardsWithoutSilk[rng.NextInt(cardsWithoutSilk.Count)];
        }
        else
        {
            // 所有微尘牌都有丝线，随机选择一张替换
            var rng = Owner.RunState.Rng.Shuffle;
            targetCard = dustCards[rng.NextInt(dustCards.Count)];
            // 先清除旧的附魔
            CardCmd.ClearEnchantment(targetCard);
        }

        if (targetCard != null)
        {
            // 应用茧笼附魔
            CardCmd.Enchant(cocoonEnchantment, targetCard, 1);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["SilkCocoon"].UpgradeValueBy(3);
    }
}
