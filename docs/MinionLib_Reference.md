# MinionLib 完整参考文档

> 来源：`D:\Github\MinionLib`  
> 生成时间：2026-04-18  
> 用途：Slay the Spire 2 Mod 开发参考

---

## 目录

- [概述](#概述)
- [目录结构总览](#目录结构总览)
- [模块详解](#模块详解)
  - [Action（随从行动系统）](#1-action随从行动系统)
  - [Commands（命令系统）](#2-commands命令系统)
  - [Component（卡牌组件系统）](#3-component卡牌组件系统)
  - [Example（示例代码）](#4-example示例代码)
  - [Initialization（初始化）](#5-initialization初始化)
  - [Layout（随从布局系统）](#6-layout随从布局系统)
  - [Minion（随从模型）](#7-minion随从模型)
  - [MinionLib.Generators（Roslyn 源码生成器）](#8-minionlibgeneratorsroslyn-源码生成器)
  - [Powers（随从能力）](#9-powers随从能力)
  - [RightClick（右键点击系统）](#10-rightclick右键点击系统)
  - [Targeting（自定义目标类型系统）](#11-targeting自定义目标类型系统)
  - [Utilities（工具类）](#12-utilities工具类)
- [Auto-Generated 文件汇总](#auto-generated-文件汇总)
- [依赖关系](#依赖关系)
- [已移植到 Theresa 的文件](#已移植到-theresa-的文件)
- [未移植的文件](#未移植的文件)

---

## 概述

MinionLib 是一个用于 **Slay the Spire 2 (STS2)** 的 C# Mod 库，基于 Godot 引擎和 Harmony 补丁框架。它提供了随从(Minion)系统、卡牌组件系统、自定义目标类型、右键点击系统等功能。

**总文件数：** 113 个 `.cs` 文件，分布在 14 个主要目录中。

**外部依赖：**
- `BaseLib` — 基础抽象库（`BaseLib.Abstracts`, `BaseLib.Patches.Content`, `BaseLib.Utils`）
- `MegaCrit.Sts2.*` — Slay the Spire 2 游戏核心库
- `Godot` — Godot 游戏引擎
- `HarmonyLib` — 运行时补丁框架
- `Microsoft.CodeAnalysis` — Roslyn 源码生成器

---

## 目录结构总览

```
MinionLib/
├── Action/                    # 随从行动系统
│   ├── GameActions/           #   GameAction 实现
│   └── Patches/               #   Harmony 补丁
├── Commands/                  # 命令系统
├── Component/                 # 卡牌组件系统（核心模块）
│   ├── Core/                  #   核心基础设施
│   ├── Interfaces/            #   接口定义
│   ├── Partials/              #   自动生成的 partial 文件
│   ├── Patches/               #   补丁
│   └── Utils/                 #   工具组件
├── Example/                   # 示例代码
│   ├── Actions/               #   示例行动
│   ├── Cards/                 #   示例卡牌
│   ├── Components/            #   示例组件
│   ├── Minions/               #   示例随从
│   ├── Potions/               #   示例药水
│   └── Powers/                #   示例能力
├── Initialization/            # 初始化
├── Layout/                    # 随从布局系统
├── Minion/                    # 随从模型
│   └── Patches/               #   补丁
├── MinionLib.Generators/      # Roslyn 源码生成器
├── Powers/                    # 随从能力
│   └── Patches/               #   补丁
├── RightClick/                # 右键点击系统
│   ├── Easy/                  #   简单右键接口
│   └── Patches/               #   补丁
├── Targeting/                 # 自定义目标类型系统
│   ├── Pets/                  #   随从相关目标类型
│   └── Utilities/             #   目标类型组合工具
├── Utilities/                 # 工具类
└── MainFile.cs                # 模组入口
```

---

## 模块详解

### 1. Action（随从行动系统）

允许随从拥有可点击触发的行动能力（Action），支持目标选择、队列管理和多人同步。

| 文件 | 描述 |
|------|------|
| `Action/ActionModel.cs` | 抽象基类，定义随从可执行的行动模型，继承自 PowerModel，支持目标选择、回合结束自动移除等 |
| `Action/CreatureActionQueueService.cs` | 行动队列服务，将随从行动加入游戏动作队列，支持多人同步 |
| `Action/CreatureActionQueueThreshold.cs` | 行动队列阈值管理，防止同一行动被重复排队 |
| `Action/GameActions/ExecuteCreatureActionGameAction.cs` | 游戏动作：执行随从行动，处理目标验证和实际执行 |
| `Action/GameActions/NetExecuteCreatureActionGameAction.cs` | 网络同步版本，支持多人游戏的行动序列化 |
| `Action/Patches/ActionClickPatch.cs` | Harmony 补丁：处理随从点击事件，支持鼠标和手柄输入，启动目标选择 |
| `Action/Patches/ActionPowerIconClickPatch.cs` | Harmony 补丁：处理行动图标点击，触发对应随从行动 |

**关键流程：**
1. 玩家点击随从 → `ActionClickPatch.OnGuiInput()`
2. 检查条件（回合、队列状态、阈值）
3. 需要目标选择 → 启动 `NTargetManager` 目标选择
4. 目标确认 → `CreatureActionQueueService.TryEnqueue()`
5. 加入 GameAction 队列 → `ExecuteCreatureActionGameAction`
6. 执行 Action → `ActionModel.TryAct()`

---

### 2. Commands（命令系统）

| 文件 | 描述 |
|------|------|
| `Commands/MinionAnimCmd.cs` | 随从动画命令：重排位置、播放攻击撞击动画（拉后→冲刺→返回） |
| `Commands/MinionCmd.cs` | 随从召唤命令：添加随从到玩家宠物列表，设置位置，触发召唤回调 |
| `Commands/PetOrderSnapshotManager.cs` | 宠物顺序快照管理器：记录和维护宠物战斗顺序，支持获取有序宠物列表 |

**关键 API：**
```csharp
// 召唤随从
await MinionCmd.AddMinion<AmiyaMinion>(player, options);

// 重排随从动画
await MinionAnimCmd.Rearrange(animated: true);

// 获取有序随从列表
var orderedPets = PetOrderSnapshotManager.GetSnapshot(player);
```

---

### 3. Component（卡牌组件系统）

MinionLib 最复杂的模块。允许给卡牌动态附加组件，组件可以：
- 修改卡牌属性（伤害、格挡、能量消耗、目标类型等）
- 响应游戏事件（打出、抽牌、回合结束等）
- 自动序列化保存/读取

#### 核心文件

| 文件 | 描述 |
|------|------|
| `Component/CardComponent.cs` | 卡牌组件抽象基类，定义组件生命周期（Attach/Detach）、合并、序列化、本地化前缀/后缀 |
| `Component/ComponentsCardModel.cs` | 组件卡牌模型基类，管理组件列表，支持添加/移除/获取组件，重写卡牌属性 |
| `Component/ComponentExtensions.cs` | 组件扩展方法，提供 `SetWasJustUpgraded` 用于 DynamicVar |

#### Core 子目录

| 文件 | 描述 |
|------|------|
| `Component/Core/CardComponentRegistry.cs` | 组件注册表，通过组件 ID 创建实例（工厂模式） |
| `Component/Core/CardComponentStateSerializer.cs` | 组件状态序列化器，将组件列表序列化为 `int[]` blob |
| `Component/Core/ComponentDelegateAttribute.cs` | 特性标记：用于标记委托注册方法 |
| `Component/Core/ComponentDescriptionRawCache.cs` | 组件描述原始文本缓存，加速本地化 |
| `Component/Core/ComponentPhase.cs` | 组件阶段枚举（Init→Prime→Prefix→Core→Postfix→Final）和上下文 |
| `Component/Core/ComponentStateAttribute.cs` | 特性标记：标记需要序列化的组件状态属性 |
| `Component/Core/DelegateRegistry.cs` | 委托注册表，按名称和类型存储委托 |
| `Component/Core/LocArgAttribute.cs` | 本地化参数特性（LocArg/NotLocArg/NestedLocString） |
| `Component/Core/NoGeneratedSerializationAttribute.cs` | 禁用自动生成序列化的特性 |
| `Component/Core/SerializationUtils.cs` | 二进制序列化工具类 |

#### Interfaces 子目录

| 文件 | 描述 |
|------|------|
| `Component/Interfaces/ICardComponent.cs` | 卡牌组件接口 |
| `Component/Interfaces/IComponentsCardModel.cs` | 组件卡牌模型接口 |
| `Component/Interfaces/IGeneratedBinarySerializable.cs` | 二进制序列化接口 |

#### Partials 子目录（Auto-generated）

| 文件 | 描述 |
|------|------|
| `Component/Partials/CardComponent_Hooks.cs` | CardComponent 的所有游戏事件钩子（~100+ 事件） |
| `Component/Partials/CardComponent_Modifiers.cs` | CardComponent 的所有数值修改器 |
| `Component/Partials/ComponentsCardModel_Hooks.cs` | ComponentsCardModel 的事件分发逻辑 |
| `Component/Partials/ComponentsCardModel_Modifiers.cs` | ComponentsCardModel 的数值修改器聚合逻辑 |
| `Component/Partials/ComponentsCardModel_ModifiersC.cs` | 以 `C` 结尾的自定义修改器钩子 |
| `Component/Partials/ICardComponent_Hooks.cs` | ICardComponent 接口的事件钩子默认实现 |
| `Component/Partials/ICardComponent_Modifiers.cs` | ICardComponent 接口的修改器默认实现 |

#### Patches 子目录

| 文件 | 描述 |
|------|------|
| `Component/Patches/CardGlowColorPatch.cs` | 补丁：根据组件自定义卡牌发光颜色 |
| `Component/Patches/CardModelUpdateDynamicVarPreviewPatch.cs` | 补丁：更新卡牌动态变量预览时包含组件变量 |
| `Component/Patches/ComponentDescriptionRawCachePatch.cs` | 补丁：在卡牌描述中注入 `{CompPre}` 和 `{CompPost}` 标记 |

#### Utils 子目录

| 文件 | 描述 |
|------|------|
| `Component/Utils/AmountCardComponent.cs` | 带数量属性的组件基类，支持自动合并（加法/减法） |
| `Component/Utils/Timing.g.cs` | Timing 枚举，定义所有游戏事件时机 |
| `Component/Utils/TimingCardComponent.cs` | 基于时机的组件基类 |
| `Component/Utils/TimingCardComponent.g.cs` | TimingCardComponent 的事件分发实现 |

**使用示例：**
```csharp
// 定义组件
public sealed partial class DamageBlockComponent : CardComponent
{
    [ComponentState<DamageVar>(ValueProp.Move)]
    public partial int Damage { get; set; }

    [ComponentState<BlockVar>(ValueProp.Move)]
    public partial int Block { get; set; }

    public override async Task OnPlayPrefix(...) {
        await CreatureCmd.Damage(..., DynamicVars.Damage, ...);
        await CreatureCmd.GainBlock(..., DynamicVars.Block, ...);
    }
}

// 给卡牌添加组件
componentsCard.AddComponent(new DamageBlockComponent { Damage = 1, Block = 1 });
```

---

### 4. Example（示例代码）

演示 MinionLib 各种功能的使用方法。

#### Actions

| 文件 | 描述 |
|------|------|
| `Example/Actions/PetAttackPoint.cs` | 宠物攻击行动，对任意敌人造成伤害 |
| `Example/Actions/PetDefensePoint.cs` | 宠物防御行动，为随从或主人获得格挡 |

#### Cards

| 文件 | 描述 |
|------|------|
| `Example/Cards/AttackakaStrikeCard.cs` | 已注释掉的示例：随从绑定攻击卡 |
| `Example/Cards/AwaitCard.cs` | 等待卡牌（测试用） |
| `Example/Cards/Blank.cs` | 空白组件卡牌 |
| `Example/Cards/DefenseakaGuardCard.cs` | 已注释掉的示例：随从绑定防御卡 |
| `Example/Cards/GrantDeckDamageBlockComponentCard.cs` | 给牌库和手牌中的所有卡牌添加伤害+格挡组件 |
| `Example/Cards/GrantHealComponentCard.cs` | 选择手牌并添加治疗组件 |
| `Example/Cards/HealSelfComponentCard.cs` | 自带治疗组件的卡牌，升级时增强 |
| `Example/Cards/MinionAdvanceCard.cs` | 调整随从位置（前排/后排切换） |
| `Example/Cards/PetEmpowerCard.cs` | 强化随从（赋予力量和敏捷） |
| `Example/Cards/SummonAttackakaCard.cs` | 召唤攻击型随从 |
| `Example/Cards/SummonDefenseakaCard.cs` | 召唤防御型随从 |
| `Example/Cards/TestComponentsCard.cs` | 测试用组件卡牌 |

#### Components

| 文件 | 描述 |
|------|------|
| `Example/Components/DamageBlockComponent.cs` | 造成伤害并获得格挡的组件 |
| `Example/Components/HealOwnerComponent.cs` | 治疗主人的组件 |
| `Example/Components/TestComponent.cs` | 测试组件，演示 ComponentState 序列化 |

#### Minions

| 文件 | 描述 |
|------|------|
| `Example/Minions/AttackakaMinion.cs` | 示例攻击型随从：6 HP，召唤时赋予力量和攻击者能力 |
| `Example/Minions/DefenseakaMinion.cs` | 示例防御型随从：自定义动画状态机，召唤时赋予敏捷和守护者能力 |

#### Potions

| 文件 | 描述 |
|------|------|
| `Example/Potions/MinionStrengthPotion.cs` | 示例药水：为目标随从赋予力量 |

#### Powers

| 文件 | 描述 |
|------|------|
| `Example/Powers/AttackakaGiftPower.cs` | 示例能力：回合开始时生成攻击卡（已注释） |
| `Example/Powers/DefenseakaGiftPower.cs` | 示例能力：回合开始时生成防御卡（已注释） |
| `Example/Powers/PetAttackerPower.cs` | 宠物攻击者能力：回合开始时赋予攻击行动点 |
| `Example/Powers/PetDefenderPower.cs` | 宠物防御者能力：回合开始时赋予防御行动点 |

---

### 5. Initialization（初始化）

| 文件 | 描述 |
|------|------|
| `Initialization/MinionHookInitializer.cs` | 初始化器：订阅战斗事件（回合开始/结束、战斗设置/结束），自动重排随从并清理队列 |

---

### 6. Layout（随从布局系统）

| 文件 | 描述 |
|------|------|
| `Layout/DefaultMinionLayout.cs` | 默认随从布局：根据位置（Front/Back/Upper）计算网格坐标 |
| `Layout/IMinionLayout.cs` | 布局接口定义 |
| `Layout/MinionLayoutContext.cs` | 布局上下文：包含房间、所有随从、位置字典 |
| `Layout/MinionLayoutManager.cs` | 布局管理器：注册多个布局器，按优先级计算最终位置 |
| `Layout/NCreatureExtension.cs` | NCreature 扩展：判断是否为随从节点 |

**布局位置枚举：**
```csharp
public enum MinionPosition
{
    Front,      // 前排下方
    Back,       // 后排下方
    FrontUpper, // 前排上方
    BackUpper,  // 后排上方
    Upper       // 最上方（备用）
}
```

---

### 7. Minion（随从模型）

| 文件 | 描述 |
|------|------|
| `Minion/MinionModel.cs` | 随从模型基类：继承 MonsterModel，定义位置枚举、空闲状态机、召唤回调 |
| `Minion/Patches/MinionInteractablePatch.cs` | 补丁：确保本地玩家的随从保持可交互 |

**MinionModel 关键属性：**
```csharp
public abstract class MinionModel : MonsterModel
{
    public MinionPosition Position { get; internal set; }
    public virtual Task OnSummon(Player owner, Creature self, MinionSummonOptions options) => Task.CompletedTask;
}
```

---

### 8. MinionLib.Generators（Roslyn 源码生成器）

编译时自动生成代码，减少样板代码。

| 文件 | 描述 |
|------|------|
| `MinionLib.Generators/BinarySerializationGenerator.cs` | 为标记 `[ComponentState]` 的属性生成二进制序列化代码 |
| `MinionLib.Generators/CardComponentRegisterSourceGenerator.cs` | 为 ICardComponent 实现生成自动注册代码和 ComponentId |
| `MinionLib.Generators/ComponentStatePropertyGenerator.cs` | 为 `[ComponentState]` 标记的 partial 属性生成后备字段 |
| `MinionLib.Generators/DelegateRegisterSourceGenerator.cs` | 为 `[ComponentDelegate]` 标记的方法生成委托注册代码 |
| `MinionLib.Generators/DynamicVarSourceGenerator.cs` | 为 CardComponent 子类生成 SmartVars 和 SmartAddArgs 代码 |
| `MinionLib.Generators/EquatableArray.cs` | 可比较的数组结构（用于增量生成器） |
| `MinionLib.Generators/ImmutableArraySequenceComparer.cs` | ImmutableArray 序列比较器 |
| `MinionLib.Generators/IsExternalInit.cs` | .NET Standard 2.0 的 record 类型支持 |

**生成的文件后缀：**
- `.BinarySerialization.g.cs`
- `.ComponentRegister.g.cs`
- `.ComponentStateProperty.g.cs`
- `.DelegateRegister.g.cs`
- `.DynamicVars.g.cs`

---

### 9. Powers（随从能力）

| 文件 | 描述 |
|------|------|
| `Powers/MinionGuardianPower.cs` | 守护者能力：前排随从将未格挡伤害重定向到自己 |
| `Powers/Patches/MinionGuardianBlockToHpPatch.cs` | 补丁：守护者获得格挡时转换为最大生命值和回血 |
| `Powers/Patches/MinionGuardianOverkillPatch.cs` | 补丁：处理守护者的过量伤害链式传递 |
| `Powers/Patches/MinionGuardianOwnerDamageSuppressPatch.cs` | 补丁：抑制守护者重定向期间主人的临时伤害 |

**守护者机制：**
- 前排随从自动成为守护者
- 主人受到的伤害重定向到守护者
- 守护者获得格挡时 → 转换为最大生命值和回血
- 过量伤害链式传递给下一个守护者

---

### 10. RightClick（右键点击系统）

允许玩家右键点击手牌触发额外效果。

| 文件 | 描述 |
|------|------|
| `RightClick/Easy/EasyRightClickableCardHandler.cs` | 简单右键处理器：将右键事件加入动作队列 |
| `RightClick/Easy/EasyRightClickCardAction.cs` | 右键动作：执行卡牌右键逻辑，支持网络同步 |
| `RightClick/Easy/IEasyRightClickableCard.cs` | 简单右键接口：卡牌实现此接口以支持右键 |
| `RightClick/Easy/NetEasyRightClickCardAction.cs` | 网络同步结构：序列化右键动作 |
| `RightClick/IRightClickHandler.cs` | 右键处理器接口 |
| `RightClick/Patches/CardRightClickPatch.cs` | 补丁：为卡牌持有者添加右键事件监听 |
| `RightClick/RightClickContext.cs` | 右键上下文：包含玩家、模型和额外载荷 |
| `RightClick/RightClickDispatcher.cs` | 右键分发器：按优先级调用注册的处理器 |

**使用方式：**
```csharp
public class MyCard : CardModel, IEasyRightClickableCard
{
    public async Task OnRightClick(PlayerChoiceContext ctx, RightClickContext clickCtx) {
        // 右键效果
    }
}
```

---

### 11. Targeting（自定义目标类型系统）

扩展游戏的目标类型系统，支持随从相关的自定义目标。

| 文件 | 描述 |
|------|------|
| `Targeting/CustomTargetType.cs` | 自定义目标类型抽象基类 |
| `Targeting/CustomTargetTypeManager.cs` | 管理器：注册和查询自定义目标类型 |
| `Targeting/ICustomTargetType.cs` | 自定义目标类型接口 |
| `Targeting/MinionTargetTypes.cs` | 预定义的随从目标类型枚举 |
| `Targeting/Patches/CustomTargetTypeCardPatch.cs` | 补丁：让卡牌系统支持自定义目标类型 |
| `Targeting/Patches/CustomTargetTypePotionPatch.cs` | 补丁：让药水系统支持自定义目标类型 |

#### Pets 子目录（具体目标类型）

| 文件 | 描述 |
|------|------|
| `Targeting/Pets/AllCreaturesTargetType.cs` | 所有生物（多目标） |
| `Targeting/Pets/AllMinionsTargetType.cs` | 所有随从（多目标，仅限当前玩家） |
| `Targeting/Pets/AnyCreatureTargetType.cs` | 任意生物（单目标） |
| `Targeting/Pets/AnyMinionOrOwnerTargetType.cs` | 任意随从或主人 |
| `Targeting/Pets/AnyMinionTargetType.cs` | 任意随从 |
| `Targeting/Pets/ItSelfTargetType.cs` | 自身（仅对行动有效） |
| `Targeting/Pets/VoidTargetType.cs` | 空目标 |

#### Utilities 子目录（目标类型组合）

| 文件 | 描述 |
|------|------|
| `Targeting/Utilities/BuiltInTargetType.cs` | 内置目标类型包装 |
| `Targeting/Utilities/DifferenceTargetType.cs` | 差集目标类型 |
| `Targeting/Utilities/IntersectionTargetType.cs` | 交集目标类型 |
| `Targeting/Utilities/LambdaTargetType.cs` | Lambda 目标类型 |
| `Targeting/Utilities/SingleTargetTypesUnionManager.cs` | 单目标类型并集管理器 |
| `Targeting/Utilities/UnionTargetType.cs` | 并集目标类型 |

---

### 12. Utilities（工具类）

| 文件 | 描述 |
|------|------|
| `Utilities/PetsOrderAccessor.cs` | 宠物顺序访问器：安全地重排宠物列表，自动触发重排动画 |

**使用方式：**
```csharp
using (var accessor = new PetsOrderAccessor(player))
{
    if (accessor.Pets != null && accessor.Pets.Count >= 2)
    {
        (accessor.Pets[0], accessor.Pets[1]) = (accessor.Pets[1], accessor.Pets[0]);
    }
} // Dispose 时自动拍摄快照并触发布局动画
```

---

### 13. MainFile（入口点）

| 文件 | 描述 |
|------|------|
| `MainFile.cs` | 模组入口：初始化 Harmony 补丁、订阅战斗事件、定义全局 using 和调试日志 |

---

## Auto-Generated 文件汇总

以下文件包含 `// <auto-generated>` 标记或是 `.g.cs` 后缀：

| 文件 | 生成器 | 说明 |
|------|--------|------|
| `Component/Partials/CardComponent_Hooks.cs` | 源生成器 | 事件钩子 |
| `Component/Partials/CardComponent_Modifiers.cs` | 源生成器 | 数值修改器 |
| `Component/Partials/ComponentsCardModel_Hooks.cs` | 源生成器 | 事件分发 |
| `Component/Partials/ComponentsCardModel_Modifiers.cs` | 源生成器 | 修改器聚合 |
| `Component/Partials/ComponentsCardModel_ModifiersC.cs` | 源生成器 | 自定义修改器 |
| `Component/Partials/ICardComponent_Hooks.cs` | 源生成器 | 接口事件默认实现 |
| `Component/Partials/ICardComponent_Modifiers.cs` | 源生成器 | 接口修改器默认实现 |
| `Component/Utils/Timing.g.cs` | 源生成器 | Timing 枚举 |
| `Component/Utils/TimingCardComponent.g.cs` | 源生成器 | Timing 事件分发 |

**注意：** 实际编译时，`.g.cs` 后缀的文件由 Roslyn 源生成器在编译时动态生成，不会直接出现在源代码目录中。`Partials` 目录下的文件虽然包含 `auto-generated` 标记，但似乎是预生成并提交到仓库的。

---

## 依赖关系

```
MinionLib
├── BaseLib (抽象基类、工具、内容补丁)
│   ├── BaseLib.Abstracts (CustomCardModel, CustomPowerModel, ICustomModel)
│   ├── BaseLib.Patches.Content (CustomContentDictionary, CustomEnums)
│   └── BaseLib.Utils (Pool 特性等)
├── MegaCrit.Sts2.Core.* (游戏核心)
│   ├── Combat, Commands, Entities, GameActions, Models
│   ├── Localization, HoverTips, Multiplayer.Serialization
│   └── Nodes (UI 和场景节点)
├── Godot (引擎 API)
├── HarmonyLib (运行时补丁)
└── Microsoft.CodeAnalysis (Roslyn 源码生成器)
```

---

## 已移植到 Theresa 的文件

以下文件已从 MinionLib 移植到 Theresa 项目：

### Action 系统

| MinionLib 路径 | Theresa 路径 | 状态 |
|----------------|-------------|------|
| `Action/ActionModel.cs` | `TheresaCode/Minions/Action/ActionModel.cs` | ✅ 已有并完善 |
| `Action/CreatureActionQueueThreshold.cs` | `TheresaCode/Minions/Action/CreatureActionQueueThreshold.cs` | ✅ 已移植 |
| `Action/CreatureActionQueueService.cs` | `TheresaCode/Minions/Action/CreatureActionQueueService.cs` | ✅ 已移植 |
| `Action/GameActions/ExecuteCreatureActionGameAction.cs` | `TheresaCode/Minions/Action/GameActions/ExecuteCreatureActionGameAction.cs` | ✅ 已移植 |
| `Action/GameActions/NetExecuteCreatureActionGameAction.cs` | `TheresaCode/Minions/Action/GameActions/NetExecuteCreatureActionGameAction.cs` | ✅ 已移植 |
| `Action/Patches/ActionClickPatch.cs` | `TheresaCode/Minions/Action/Patches/ActionClickPatch.cs` | ✅ 已移植（完整版） |
| `Action/Patches/ActionPowerIconClickPatch.cs` | `TheresaCode/Minions/Action/Patches/ActionPowerIconClickPatch.cs` | ✅ 已移植 |

### 布局系统

| MinionLib 路径 | Theresa 路径 | 状态 |
|----------------|-------------|------|
| `Layout/DefaultMinionLayout.cs` | `TheresaCode/Minions/Layout/DefaultMinionLayout.cs` | ✅ 已有 |
| `Layout/IMinionLayout.cs` | `TheresaCode/Minions/Layout/IMinionLayout.cs` | ✅ 已有 |
| `Layout/MinionLayoutContext.cs` | `TheresaCode/Minions/Layout/MinionLayoutContext.cs` | ✅ 已有 |
| `Layout/MinionLayoutManager.cs` | `TheresaCode/Minions/Layout/MinionLayoutManager.cs` | ✅ 已有 |
| `Layout/NCreatureExtension.cs` | `TheresaCode/Minions/Layout/NCreatureExtension.cs` | ✅ 已有 |

### 命令系统

| MinionLib 路径 | Theresa 路径 | 状态 |
|----------------|-------------|------|
| `Commands/MinionAnimCmd.cs` | `TheresaCode/Minions/Commands/MinionAnimCmd.cs` | ✅ 已有 |
| `Commands/MinionCmd.cs` | `TheresaCode/Minions/Commands/MinionCmd.cs` | ✅ 已有 |
| `Commands/PetOrderSnapshotManager.cs` | `TheresaCode/Minions/Commands/PetOrderSnapshotManager.cs` | ✅ 已有 |

### 交互补丁

| MinionLib 路径 | Theresa 路径 | 状态 |
|----------------|-------------|------|
| `Minion/Patches/MinionInteractablePatch.cs` | `TheresaCode/Minions/Patches/MinionInteractablePatch.cs` | ✅ 已有 |
| `Minion/Patches/MinionAddCreaturePositionPatch.cs` | `TheresaCode/Minions/Patches/MinionAddCreaturePositionPatch.cs` | ✅ 已有 |

### 工具类

| MinionLib 路径 | Theresa 路径 | 状态 |
|----------------|-------------|------|
| `Utilities/PetsOrderAccessor.cs` | `TheresaCode/Minions/Utilities/PetsOrderAccessor.cs` | ✅ 已移植 |

### 随从模型

| MinionLib 路径 | Theresa 路径 | 状态 |
|----------------|-------------|------|
| `Minion/MinionModel.cs` | `TheresaCode/Minions/Models/MinionModel.cs` | ✅ 已有（自定义版本） |

---

## 未移植的文件

以下文件**尚未移植**，因为当前没有需求或复杂度太高：

### Component 系统（复杂度极高）

- `Component/*.cs` — 全部未移植
- `Component/Core/*.cs` — 全部未移植
- `Component/Interfaces/*.cs` — 全部未移植
- `Component/Partials/*.cs` — 全部未移植（auto-generated）
- `Component/Patches/*.cs` — 全部未移植
- `Component/Utils/*.cs` — 全部未移植

**原因：** 需要 Source Generator、BaseLib 深度集成，当前无卡牌组件需求。

### RightClick 系统

- `RightClick/*.cs` — 全部未移植
- `RightClick/Easy/*.cs` — 全部未移植
- `RightClick/Patches/*.cs` — 全部未移植

**原因：** 当前无卡牌右键点击需求。

### Example 示例

- `Example/**/*.cs` — 全部未移植

**原因：** 示例代码，不需要移植。

### Generators

- `MinionLib.Generators/*.cs` — 全部未移植

**原因：** Source Generator 是编译时工具，不需要在 Theresa 中复用。

### Powers（MinionLib 专属）

- `Powers/MinionGuardianPower.cs` — 未移植
- `Powers/Patches/*.cs` — 未移植

**原因：** 守护者机制是 MinionLib 的示例能力，Theresa 有自己的能力系统。

### Initialization

- `Initialization/MinionHookInitializer.cs` — 未移植

**原因：** Theresa 有自己的初始化逻辑（`MainFile.cs`）。

---

## 快速参考

### 随从召唤
```csharp
await MinionCmd.AddMinion<AmiyaMinion>(player, new MinionSummonOptions {
    Position = MinionPosition.Front
});
```

### 随从 Action 定义
```csharp
public sealed class MyAction : ActionModel
{
    public override TargetType TargetType => TargetType.None;
    public override bool DecrementAfterAct => true;

    protected override async Task OnAct(PlayerChoiceContext ctx, Creature? target)
    {
        // 行动逻辑
    }
}
```

### 随从 Action 触发流程
```
玩家点击随从
  → ActionClickPatch.OnGuiInput()
  → 检查 CanAct()
  → 需要目标？启动 NTargetManager
  → CreatureActionQueueService.TryEnqueue()
  → ExecuteCreatureActionGameAction
  → ActionModel.TryAct()
  → OnAct()
```
