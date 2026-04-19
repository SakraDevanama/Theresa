using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;



namespace Theresa.TheresaCode.Cards;

// 【1】将此卡注册到 Watcher 角色的卡池中

[Pool(typeof(TheresaCardPool))]
public sealed class Candle() : TheresaCardModel(
    0,                      // 能量消耗：0（免费）
    CardType.Skill,         // 卡牌类型：技能
    CardRarity.Ancient,    // 稀有度：罕见
    TargetType.Self)        // 目标：自身
{
    // 【2】定义关键词：此卡会“消耗”（Exhaust），即打出后移出本场战斗
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 【3】指定卡牌美术资源路径：
    // - 移除命名空间前缀（如 "Watcher.Card.ForeignInfluence" → "ForeignInfluence"）
    // - 转为小写 + ".png"
    // - 通过扩展方法 .CardImagePath() 拼接完整资源路径

    // 【4】卡牌打出时的核心逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 【5】从【所有卡池】中筛选出所有攻击卡（Attack 类型）
        var allAttacks = ModelDb.AllCards.Where(c => c.Type == CardType.Attack);

        // 【6】使用卡牌工厂生成 3 张【互不相同】的攻击卡（用于本次战斗）
        // - Owner：当前玩家
        // - allAttacks：候选卡列表
        // - 3：生成数量
        // - Owner.RunState.Rng.CombatCardGeneration：战斗专用随机数生成器（确保可重放）
        var randomAttacks = CardFactory.GetDistinctForCombat(
            Owner,
            allAttacks,
            3,
            Owner.RunState.Rng.CombatCardGeneration
        ).ToList();

        // 【7】如果成功生成了至少一张卡
        if (randomAttacks.Any())
        {
            // 【8】弹出选择界面，让玩家从 3 张中选 1 张
            var chosenCard = await CardSelectCmd.FromChooseACardScreen(
                choiceContext,   // 当前交互上下文
                randomAttacks,   // 候选卡列表
                Owner            // 玩家
            );

            // 【9】如果玩家确实选择了一张卡（未取消）
            if (chosenCard != null)
            {
                // 【10】如果此卡已升级（Upgraded），则让选中的卡本回合免费
                if (IsUpgraded)
                    chosenCard.SetToFreeThisTurn(); // 设置 costOverride = 0

                // 【11】将选中的卡加入手牌（并标记为“生成卡”，影响某些遗物/能力）
                await CardPileCmd.AddGeneratedCardToCombat(
                    chosenCard,
                    PileType.Hand, // 加入手牌
                    true           // isGenerated: 是程序生成的卡（非初始卡组）
                );
            }
        }
    }

    // 【12】卡牌升级效果：
    // - 无额外视觉变化（关键词、描述等不变）
    // - 实际效果已在 OnPlay 中实现：选中的卡本回合免费
    protected override void OnUpgrade()
    {
        // 升级逻辑已内联在 OnPlay 中，此处可留空或添加日志
        // （保留方法以符合框架约定）
    }
}