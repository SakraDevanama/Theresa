using BaseLib.Patches.Content;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 死魂灵余息 (DeadSoulBreath)
/// 3费技能牌（消耗）
/// 获得8（+2）点格挡。
/// 如果下一张打出的牌是技能牌，则复制该牌回到手中并赋予消耗且本回合可以免费打出，
/// 然后赋予所有敌人6（+2）层凋亡。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DeadSoulBreath() : TheresaCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const int BaseBlock = 8;
    private const int UpgradeBlockBonus = 2;
    private const int BaseApoptosis = 6;
    private const int UpgradeApoptosisBonus = 2;

    public override bool GainsBlock => true;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust,
        Apoptosis.Apopto
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BlockAmount", BaseBlock),
        new DynamicVar("ApoptosisAmount", BaseApoptosis)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        var blockAmount = (int)DynamicVars["BlockAmount"].BaseValue;
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(blockAmount, ValueProp.Move), cardPlay);

        var apoptosisAmount = (int)DynamicVars["ApoptosisAmount"].BaseValue;
        await PowerCmd.Apply<DeadSoulBreathEffect>(Owner.Creature, apoptosisAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BlockAmount"].UpgradeValueBy(UpgradeBlockBonus);
        DynamicVars["ApoptosisAmount"].UpgradeValueBy(UpgradeApoptosisBonus);
    }
}

/// <summary>
/// 死魂灵余息效果
/// 监听持有者打出的下一张牌。
/// 如果是技能牌，复制回手牌（赋予消耗、本回合免费），并给所有敌人凋亡。
/// 无论是否是技能牌，都在打出后移除自身。
/// </summary>
public sealed class DeadSoulBreathEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 只监听持有者自己打出的牌
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        // 跳过死魂灵余息自己，等待真正的下一张牌
        if (cardPlay.Card is DeadSoulBreath) return;

        // 如果是技能牌，触发复制和凋亡
        if (cardPlay.Card.Type == CardType.Attack)
        {
            // 使用 CombatState.CreateCard 创建一张新的副本（比 CreateClone 更干净，避免播放状态残留）
            var copiedCard = Owner.CombatState?.CreateCard(cardPlay.Card.CanonicalInstance, cardPlay.Card.Owner);
            if (copiedCard == null) return;

            copiedCard.AddKeyword(CardKeyword.Exhaust);
            copiedCard.SetToFreeThisTurn();
            await CardPileCmd.Add(copiedCard, PileType.Hand);

            // 给所有敌人施加凋亡（不包括自己）
            var combatState = Owner.CombatState;
            if (combatState != null)
            {
                var enemies = combatState.GetOpponentsOf(Owner)
                    .Where(c => c.IsAlive)
                    .ToList();

                foreach (var enemy in enemies)
                {
                    await PowerCmd.Apply<ApoptosisPower>(enemy, (int)Amount, Owner, null);
                }
            }
        }

        // 无论是否是技能牌，都在打出后移除效果
        await PowerCmd.Remove(this);
    }
}
