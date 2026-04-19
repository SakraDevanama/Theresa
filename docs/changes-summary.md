# Theresa BaseLib 修改总结

## 一、卡牌修改

### 1. 愿景 (Wish)
**文件**: `TheresaCode/Cards/Wish.cs`

| 属性 | 旧版 | 新版 |
|------|------|------|
| 费用 | 1 | 1 (不变) |
| 稀有度 | ? | 普通 Common |
| 格挡 | 5 | 7 (+1) |
| 抽牌 | 条件抽(5-微尘数) | 若微尘未满抽1(+1)张 |
| Keywords | 无 | 黯淡 |
| Keywords Tip | 无 | 黯淡、微尘、萦绕 |

**描述**: 获得 {Block} 点格挡。若固有微尘数量低于上限，抽 {Cards} 张牌。

---

### 2. 点缀 (Spot)
**文件**: `TheresaCode/Cards/Spot.cs`

| 属性 | 旧版 | 新版 |
|------|------|------|
| 费用 | 1 | 0 |
| 稀有度 | 基础 Basic | 基础 Basic (不变) |
| 伤害 | 7 (+3) | 4 (+3) |
| 效果 | 获得1个微尘+造成伤害 | 造成伤害+从微尘选1张加入手牌 |
| Keywords | 黯淡 | 黯淡、微尘、萦绕 |
| Keywords Tip | 无 | 黯淡、微尘、萦绕 |

**描述**: 造成 {Damage} 点伤害。将一张固有微尘放入手中。

**选择界面**: 多张微尘时弹出选择框（`static_hover_tips` 表 `choose_dust_to_hand`）

---

### 3. 在回忆中坠落 (FallFromMemory) — 新卡
**文件**: `TheresaCode/Cards/FallFromMemory.cs`

| 属性 | 值 |
|------|-----|
| 费用 | 0 |
| 类型 | 技能 |
| 稀有度 | 普通 Common |
| Keywords | 萦绕 |

**效果**:
- 打出时：萦绕1次，增加1点耗能
- 被萦绕时：从微尘移到手牌，降低2(+1)点耗能

**实现要点**:
- 实现 `IDustCard` 接口
- `TriggerWhenLingered()` 返回 `true` 跳过后续默认打出逻辑
- `DustManager.DustIt()` 已修改支持此机制

---

### 4. 小衣匠 (LittleTailor)
**文件**: `TheresaCode/Cards/LittleTailor.cs`

| 属性 | 旧版 | 新版 |
|------|------|------|
| 费用 | 2 | 1 |
| 稀有度 | 罕见 Uncommon | 普通 Common |
| 格挡 | 6 (+1) | 7 (+3) |
| 效果 | 格挡+给予敌人茧缚+回手 | 格挡+编织茧笼+回手 |
| Keywords | 无 | 丝线、茧笼 |

**描述**: 获得 {Block} 点格挡。对随机手牌编织：茧笼。每次打出会回到自己手里。

---

## 二、丝线系统重构

### 核心架构

```
编织 (Action)
  └─→ 给目标牌添加丝线附魔 (Enchantment)
        ├── 基础效果: 回合结束时向相邻卡复制
        └── 额外效果: 茧笼 (攻击牌造成伤害 / 技能牌获得格挡)
```

### 1. SilkThreadEnchantment（丝线附魔基类）
**文件**: `TheresaCode/Enchantments/SilkThreadEnchantment.cs`

- 纯标记类，无 `OnPlay` 效果
- `CanApplyTo` 重写：每张牌最多1张丝线（检查是否已有 `SilkThreadEnchantment`）

### 2. CocoonSilkEnchantment（茧笼丝线）
**文件**: `TheresaCode/Enchantments/CocoonSilkEnchantment.cs`

- 继承 `SilkThreadEnchantment`
- 标记卡牌带有茧笼效果
- 具体效果由 `SilkSpreadPower` 处理

### 3. SilkSpreadPower（丝线传播Power）
**文件**: `TheresaCode/Powers/SilkSpreadPower.cs`

**回合结束时执行（`AfterTurnEnd`）**:

1. **触发茧笼效果**（手牌 + 固有微尘）:
   - 攻击牌 → 对随机敌人造成 3 点伤害
   - 技能牌 → 获得 3 点格挡

2. **传播丝线**（仅手牌）:
   - 有丝线的卡向左右相邻卡复制同类型丝线
   - 相邻卡已有丝线则不复制

---

## 三、DustManager 修改

**文件**: `TheresaCode/Dust/DustManager.cs`

### 1. IDustCard 接口更新
```csharp
// 旧: void TriggerWhenLingered()
// 新: Task<bool> TriggerWhenLingered()
```
- 返回 `true` 表示卡自己处理了萦绕逻辑，跳过后续默认打出

### 2. DustIt() 逻辑更新
- 调用 `TriggerWhenLingered()` 后检查返回值
- 若返回 `true`，跳过 `CreateClone` + `AutoPlay` 流程
- 仅处理 `hasExhaust` 消耗逻辑

---

## 四、新增 Keywords

### 1. 萦绕 (Linger)
**文件**: `TheresaCode/Keywords/LingerKeyword.cs`

- 已有定义，无需修改
- 效果: 随机打出一张固有微尘的复制，消耗属性的牌会被消耗

### 2. 茧笼 (Cocoon) — 新增
**文件**: `TheresaCode/Keywords/CocoonKeyword.cs`

- `[CustomEnum]` + `[KeywordProperties(AutoKeywordPosition.None)]`
- 效果: 在你回合结束时，若为攻击牌则对随机敌人造成3点伤害，否则使你获得3点格挡

---

## 五、本地化更新

### cards.json
| Key | 内容 |
|-----|------|
| `THERESA-WISH.description` | 获得格挡。若固有微尘数量低于上限，抽 {Cards} 张牌。 |
| `THERESA-SPOT.description` | 造成 {Damage} 点伤害。将一张固有微尘放入手中。 |
| `THERESA-LITTLE_TAILOR.description` | 获得格挡。对随机手牌编织：茧笼。每次打出会回到自己手里。 |
| `THERESA-FALL_FROM_MEMORY.description` | 萦绕1次。增加1点耗能。萦绕：降低 {CostReduction} 点耗能并放入手中。 |

### card_keywords.json
| Key | 内容 |
|-----|------|
| `THERESA-SILK.description` | 在你回合结束时，向相邻的卡牌进行复制。 |
| `THERESA-COCOON.description` | 在你回合结束时，若为攻击牌则对随机敌人造成3点伤害，否则使你获得3点格挡。 |

### static_hover_tips.json
| Key | 内容 |
|-----|------|
| `choose_dust_to_hand` | 选择一张固有微尘加入手牌 |

---

## 六、BabelWord 遗物实现

**文件**: `TheresaCode/Relics/BabelWord.cs`

### 1. 回合开始抽牌（基于微尘容量差值）
- 在 `AfterPlayerTurnStart` 中计算 `MaxDust - DustCount`
- 差值大于 0 时触发抽牌

### 2. 回合结束 forget 效果
- 方法签名修正为 `BeforeTurnEnd(PlayerChoiceContext, CombatSide)`
- 检查 `side == CombatSide.Player` 确保只在玩家回合结束时触发
- 给本回合被萦绕过的固有微尘添加：
  - `CardKeyword.Ethereal`（虚无）
  - `CardKeyword.Exhaust`（消耗）
- 将处理过的卡牌移入弃牌堆
- 遗物闪光反馈

### 3. DustManager 冗余清理
- 移除 `DustManager.LingeredThisTurn` 静态列表
- 萦绕记录完全由 `BabelWord._lingeredThisTurn` 管理
- 避免双重记录带来的维护负担

### 4. 本地化更新
| 文件 | Key | 内容 |
|------|-----|------|
| `Theresa/localization/zhs/relics.json` | `THERESA-BABEL_WORD.description` | ...获得[red]虚无[/red]和[red]消耗[/red]并被移入弃牌堆。 |
| `Theresa/localization/eng/relics.json` | `THERESA-BABEL_WORD.title` | The Final Chapter |
| `Theresa/localization/eng/relics.json` | `THERESA-BABEL_WORD.description` | ...gain [red]Ethereal[/red] and [red]Exhaust[/red], then move to discard pile. |
| `Theresa/localization/eng/relics.json` | `THERESA-BABEL_WORD.flavor` | The legacy of Babel, the last page of the story. |

---

## 七、文件清单

### 修改的文件
1. `TheresaCode/Cards/Wish.cs`
2. `TheresaCode/Cards/Spot.cs`
3. `TheresaCode/Cards/LittleTailor.cs`
4. `TheresaCode/Dust/DustManager.cs`
5. `TheresaCode/Enchantments/SilkThreadEnchantment.cs`
6. `TheresaCode/Relics/BabelWord.cs`
7. `Theresa/localization/zhs/cards.json`
8. `Theresa/localization/zhs/card_keywords.json`
9. `Theresa/localization/zhs/static_hover_tips.json`
10. `Theresa/localization/zhs/relics.json`
11. `Theresa/localization/eng/relics.json`
12. `docs/changes-summary.md`

### 新增的文件
1. `TheresaCode/Cards/FallFromMemory.cs`
2. `TheresaCode/Enchantments/CocoonSilkEnchantment.cs`
3. `TheresaCode/Powers/SilkSpreadPower.cs`
4. `TheresaCode/Keywords/CocoonKeyword.cs`
