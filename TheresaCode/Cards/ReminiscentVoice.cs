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
/// 记忆中的回声 (ReminiscentVoice)
/// 1费技能牌 / 罕见
/// 自身当前生命值低于15（含15）则回复12（+9）点生命值；
/// 自身当前生命值高于16（含16）则获得2层茧缚。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class ReminiscentVoice() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 生命值阈值
    private const int HpThreshold = 15;
    // 基础回复生命值
    private const int BaseHeal = 12;
    // 升级后回复增加
    private const int UpgradeHealBonus = 9;
    // 茧缚层数（固定2层，不升级）
    private const int CocoonAmount = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SilkCocoon>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Heal", BaseHeal),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        int currentHp = Owner.Creature.CurrentHp;

        // 生命值 ≤ 15：回复生命值
        if (currentHp <= HpThreshold)
        {
            int healAmount = (int)DynamicVars["Heal"].BaseValue;
            await CreatureCmd.Heal(Owner.Creature, healAmount);
        }
        // 生命值 ≥ 16：获得2层茧缚
        else if (currentHp >= HpThreshold + 1)
        {
            await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), 
                Owner.Creature,
                CocoonAmount,
                Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后回复+9（12 → 21）
        DynamicVars["Heal"].UpgradeValueBy(UpgradeHealBonus);
    }
}