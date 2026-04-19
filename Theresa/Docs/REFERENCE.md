# Theresa Mod 开发参考

## 项目路径速查

| 项目 | 路径 |
|------|------|
| Theresa mod Java 版本参考 | `C:\Users\admin\Desktop\Theresa-master` |
| Slay the Spire 2 游戏源码 | `C:\Users\admin\Desktop\Slay the Spire 2` |
| BaseLib 源码 | `C:\Users\admin\Documents\GitHub\BaseLib-StS2` |
| BaseLib 快速使用指南 | `C:\Users\admin\Desktop\Theresa\Theresa_BaseLib\Theresa\Docs` |
| 当前 mod 工作目录 | `C:\Users\admin\Desktop\Theresa\Theresa_BaseLib` |

---

## 丝线系统（Silk System）

### 核心概念

**丝线 = 茧笼**，两者是同一个东西。回合结束时触发效果。

| 组件 | 说明 |
|------|------|
| `SilkThreadEnchantment` | 丝线附魔（标记卡牌拥有丝线） |
| `SilkSpreadPower` | 丝线传播 Power（处理效果和传播） |
| `SilkKeyword` | 丝线关键词 |

### 丝线效果

**触发时机**：`BeforeTurnEnd`（回合结束前，手牌被丢弃前）

**效果范围**：仅手牌和微尘中的带丝线卡牌

| 卡牌类型 | 效果 |
|---------|------|
| 攻击牌 | 对随机敌人造成 3 点伤害 |
| 技能牌 | 获得 3 点格挡 |

### 传播规则

| 位置 | 传播方式 |
|------|---------|
| 手牌 | 向左右相邻卡牌复制丝线 |
| 微尘 | 首尾相连（环形传播） |

### 限制

- 每张牌最多 1 张丝线（`CanEnchant` 检查）
- 传播目标不能已有丝线
- 需要至少 2 张卡才会触发传播

### 相关本地化 Key

```json
// card_keywords.json
"THERESA-SILK.description": "会给予卡牌额外效果。在你回合结束时，向相邻的卡牌进行复制。"
"THERESA-SILK.title": "丝线"

// enchantments.json
"THERESA-SILK_THREAD_ENCHANTMENT.title": "茧笼"
"THERESA-SILK_THREAD_ENCHANTMENT.description": "在你回合结束时，若为攻击牌则对随机敌人造成3点伤害，否则使你获得3点格挡。"

// powers.json
"THERESA-SILK_SPREAD_POWER.title": "丝线传播"
"THERESA-SILK_SPREAD_POWER.description": "丝线在手牌和微尘中传播并触发茧笼效果。"
"THERESA-SILK_SPREAD_POWER.smartDescription": "在你回合结束前，带有丝线的卡牌会触发茧笼效果（攻击牌造成伤害，技能牌获得格挡），并向相邻卡牌复制丝线。"
```

---

## 卡牌开发

### 基类选择

| 基类 | 用途 | 推荐度 |
|------|------|--------|
| `ConstructedCardModel` | 链式 API，简洁快速 | ⭐ 推荐 |
| `CustomCardModel` | 完全自定义 | 复杂场景 |
| `TheresaCardModel` | Theresa 专属基类 | 本项目使用 |

### ConstructedCardModel 快速创建

```csharp
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

[Pool(typeof(TheresaCardPool))]
public class MyCard : ConstructedCardModel
{
    public MyCard() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithBlock(7);           // 格挡
        WithDamage(6);          // 伤害
        WithPower<StrengthPower>(1); // 施加能力
        WithKeywords(CardKeyword.Exhaust); // 关键词
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 自定义逻辑
    }
}
```

### 常用 DynamicVar

| 变量 | 用途 | 示例 |
|------|------|------|
| `DamageVar` | 伤害 | `new DamageVar(6m, ValueProp.Move)` |
| `BlockVar` | 格挡 | `new BlockVar(7m, ValueProp.Move)` |
| `DynamicVar` | 自定义数值 | `new DynamicVar("MyVar", 1m)` |
| `PowerVar<T>` | 能力层数 | `new PowerVar<StrengthPower>(1m)` |

### 常用命令

```csharp
// 攻击
await DamageCmd.Attack(damage)
    .FromCard(this)
    .Targeting(target)
    .WithHitFx("vfx/vfx_attack_slash")
    .Execute(choiceContext);

// 格挡
await CreatureCmd.GainBlock(Owner.Creature, blockAmount, cardPlay);

// 施加 Power
await PowerCmd.Apply<StrengthPower>(targetCreature, amount, applierCreature, this);

// 抽牌
await CardPileCmd.Draw(choiceContext, count, player);

// 给予能量
PlayerCmd.GainEnergy(amount, player).Wait();

// 伤害（非卡牌攻击，如 Power 效果）
await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Unpowered | ValueProp.Move, attacker, null);
```

---

## Power 开发

### 基类

```csharp
public abstract class TheresaPowerModel : CustomPowerModel
{
    // 自动生成 Power ID: MyGreatPower -> my_great_power
    protected virtual string PowerId => ...
    
    // 自动图标路径
    protected virtual string IconBasePath => $"res://Theresa/images/powers/{PowerId}.png";
}
```

### 常用钩子

| 钩子方法 | 触发时机 |
|---------|---------|
| `BeforeTurnEnd` | 回合结束前（手牌丢弃前）⭐ |
| `AfterTurnEnd` | 回合结束后（手牌丢弃后） |
| `AfterSideTurnStart` | 某一方回合开始时 |
| `BeforeDamageReceived` | 受到伤害前 |
| `AfterDamageDealt` | 造成伤害后 |
| `OnAmountChanged` | 层数变化时 |

### Power 类型

| 类型 | 说明 |
|------|------|
| `PowerType.Buff` | 增益（绿色图标） |
| `PowerType.Debuff` | 减益（红色图标） |

### 叠加类型

| 类型 | 说明 |
|------|------|
| `PowerStackType.Counter` | 可叠加层数 |
| `PowerStackType.Single` | 不可叠加，显示单图标 |
| `PowerStackType.None` | 不显示层数 |

---

## 本地化 Key 规则

### 命名格式

| 类型 | Key 格式 | 示例 |
|------|---------|------|
| 卡牌 | `MODID-CARD_ID.title/description` | `THERESA-LITTLE_TAILOR.description` |
| Power | `MODID-POWER_ID.title/description/smartDescription` | `THERESA-SILK_SPREAD_POWER.title` |
| 关键词 | `MODID-KEYWORD_ID.title/description` | `THERESA-SILK.title` |
| 附魔 | `MODID-ENCHANTMENT_ID.title/description` | `THERESA-SILK_THREAD_ENCHANTMENT.title` |
| 遗物 | `MODID-RELIC_ID.title/description/flavor` | `THERESA-MY_RELIC.title` |

### Power ID 生成规则

类名 `SilkSpreadPower` → `silk_spread_power`

```csharp
// CamelCase 转 snake_case
"SilkSpreadPower" → "silk_spread_power"
```

---

## 常见问题

### 1. 回合结束时手牌为空

**原因**：STS2 回合结束时手牌自动丢弃到弃牌堆。

**解决**：使用 `BeforeTurnEnd` 代替 `AfterTurnEnd`。

```csharp
public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
{
    // 此时手牌还在
}
```

### 2. 附魔后检查不到

**原因**：`OnPlay` 中修改的卡牌实例可能在后续流程中被替换。

**解决**：`CardCmd.Enchant` 是同步的，直接修改实例。确保检查的是同一集合。

### 3. `DamageCmd.Attack().FromCard(null)` 报错

**原因**：`FromCard` 需要非 null 的 CardModel。

**解决**：Power 造成的伤害使用 `CreatureCmd.Damage`。

```csharp
// ❌ 错误
await DamageCmd.Attack(damage).FromCard(null).Targeting(target).Execute(...);

// ✅ 正确
await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Unpowered | ValueProp.Move, attacker, null);
```

### 4. Tween 动画报错

**原因**：`NCard.FindOnTable` 可能找不到节点，或 Tween 创建失败。

**解决**：添加 try-catch，使用 `modulate` 变化代替 `scale`。

```csharp
try
{
    var nCard = NCard.FindOnTable(card);
    if (nCard == null) return;
    
    var tween = ((Node)nCard).CreateTween();
    tween.TweenProperty(nCard, "modulate", flashColor, 0.2f)
        .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
    tween.Play();
}
catch { /* 忽略 */ }
```

---

## 编译与部署

```bash
cd "C:\Users\admin\Desktop\Theresa\Theresa_BaseLib"
dotnet build Theresa.csproj -c Release
```

编译成功后自动复制到游戏 mods 文件夹。

---

## Java 版本参考对照

| Java (STS1) | C# (STS2) | 说明 |
|------------|-----------|------|
| `AbstractSilk` | `SilkThreadEnchantment` | 丝线标记 |
| `SilkPatch.atTurnEnd()` | `SilkSpreadPower.BeforeTurnEnd()` | 回合结束处理 |
| `SilkFlashAction` | `CreatureCmd.Damage` / `CreatureCmd.GainBlock` | 效果执行 |
| `SetSilkAction` | `CardCmd.Enchant` | 添加丝线 |
| `CardGroup.CardGroupType.HAND` | `PileType.Hand` | 手牌 |
| `CardGroup.CardGroupType.DRAW_PILE` | `PileType.Draw` | 抽牌堆 |
| `CardGroup.CardGroupType.DISCARD_PILE` | `PileType.Discard` | 弃牌堆 |
| `AbstractDungeon.player.hand` | `PileType.Hand.GetPile(player)` | 获取手牌 |
| `DustPatch.dustManager.dustCards` | `DustManager.Cards` | 微尘 |
| `AbstractCard.tags.add(REMOVED_FROM_DECK)` | `RemovedCardsTracker` + `RemoveFromDeckPatch` | 追踪牌组移除 |
| `AbstractDungeon.srcRemoveCard` | `RemovedCardsTracker.RemovedCards` | 获取移除的卡牌 |


---

## 重现机制（Replay System）

### 核心概念

**重现** = 检视本局游戏从牌组**永久移除**的卡牌（商店移除、事件移除等），选择复制到手牌。

| 组件 | 说明 |
|------|------|
| `RemovedCardsTracker` | 追踪本局游戏中从牌组移除的卡牌 |
| `RemoveFromDeckPatch` | Harmony Patch，拦截 `CardPileCmd.RemoveFromDeck` |
| `ReplayHelper` | 执行重现效果 |
| `ReplayKeyword` | 重现关键词 |

### 重现效果

1. **检视范围**：本局游戏中从牌组永久移除的卡牌（`RemovedCardsTracker.RemovedCards`）
2. **选择数量**：通常为 1 张
3. **复制方式**：`CardModel.FromSerializable()` 创建全新实例
4. **添加属性**：消耗（`CardKeyword.Exhaust`）
5. **费用修改**：本回合耗能 -1（`EnergyCost.AddThisTurnOrUntilPlayed(-1)`）

### 限制

- **源卡牌限制**：每张带重现关键词的卡牌每场战斗只能触发一次重现
- **目标卡牌限制**：每张被移除的卡牌每场战斗只能被重现一次
- 通过 `ReplayKeyword._replayedThisCombat` 和 `RemovedCardsTracker._replayedThisCombat` 分别跟踪

### 使用示例

```csharp
// 在卡牌 OnPlay 中触发重现
protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
{
    // 先执行卡牌本身的效果（如攻击、格挡）
    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

    // 再执行重现效果
    if (CombatState != null)
    {
        await ReplayHelper.ExecuteReplay(
            choiceContext,
            this,           // 源卡牌
            CombatState,    // 战斗状态
            count: 1,       // 选择 1 张
            upgradeForRun: false
        );
    }
}
```

### 相关卡牌

| 卡牌 | 基础效果 | 重现 |
|------|---------|------|
| `SarkazSee` (萨卡兹见证) | 造成 9 点伤害 | 检视移除的卡牌，复制 1 张到手牌 |
| `AStory` (萨卡兹叙说) | 获得 7 点格挡 | 检视移除的卡牌，复制 1 张到手牌 |
| `CivilightEterna` (文明的存续) | 虚无 | 重现 1 张，使其在整局游戏升级 |
