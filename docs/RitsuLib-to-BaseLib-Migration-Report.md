# RitsuLib 移除与 BaseLib 接管 — 工作总结报告

> 日期：2026-04-18
> 目标：将 `Theresa_BaseLib` 版本中的 `STS2-RitsuLib` 依赖完全移除，所有功能由 `BaseLib` 接管。
> 状态：**已完成，编译通过（0 错误）**

---

## 一、背景说明

项目维护两个版本：
- `Theresa_RitsuLib`：基于 RitsuLib 框架的版本（保留不动）
- `Theresa_BaseLib`：基于 BaseLib 框架的版本（本次修改目标）

本次工作仅针对 `Theresa_BaseLib` 目录，将其从"RitsuLib + BaseLib 双依赖"精简为"仅 BaseLib 单依赖"。

---

## 二、移除的依赖

| 项目 | 修改内容 |
|------|----------|
| `Theresa.csproj` | 移除 `STS2.RitsuLib` NuGet 包引用；移除构建后自动复制 RitsuLib 到 mods 目录的 Target |
| `Theresa.json` | `dependencies` 从 `["BaseLib", "STS2-RitsuLib", "MinionLib"]` 改为 `["BaseLib", "MinionLib"]` |
| `packages/Theresa.csproj` | 同步移除旧配置中的 RitsuLib 引用和复制逻辑 |

---

## 三、核心功能迁移详情

### 3.1 附魔注册（`SilkThreadEnchantment`）

**原方案（RitsuLib）：**
```csharp
RitsuLibFramework.GetContentRegistry(ModId).RegisterEnchantment<SilkThreadEnchantment>();
```

**新方案（BaseLib）：**
- 将 `SilkThreadEnchantment` 的基类从 `ModEnchantmentTemplate`（RitsuLib）改为 `CustomEnchantmentModel`（BaseLib）
- `CustomEnchantmentModel` 继承自游戏原生的 `EnchantmentModel` 并实现了 `ICustomModel` 接口
- BaseLib 的 `PrefixIdPatch` 会自动为 `ICustomModel` 添加前缀，使其被 `ModelDb` 正确识别
- **无需手动注册**，类型在实例化时自动进入 `ModelDb`

**修改文件：**
- `TheresaCode/Enchantments/SilkThreadEnchantment.cs`

---

### 3.2 卡牌消耗事件监听（`ApoptosisCountdownEffect` / `FollowTheHistory`）

**原方案（RitsuLib）：**
```csharp
_exhaustSubscription = RitsuLibFramework.SubscribeLifecycle<CardExhaustedEvent>(OnCardExhausted);
_combatEndSubscription = RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(_ => OnCombatEnded());
```

**新方案（BaseLib / 原生 Hook）：**
- 直接重写 `PowerModel` 基类提供的虚方法：
  - `AfterCardExhausted(PlayerChoiceContext, CardModel, bool)` — 监听卡牌消耗
  - `AfterCombatEnd(CombatRoom)` — 监听战斗结束
- 优势：无需手动管理 `IDisposable` 订阅句柄，生命周期由游戏 Hook 系统自动管理

**修改文件：**
- `TheresaCode/Cards/ApoptosisCountdownEffect.cs`
- `TheresaCode/Cards/FollowTheHistory.cs`

---

### 3.3 卡牌级消耗监听（`NightBeforeWar`）

**原方案（RitsuLib）：**
```csharp
_exhaustSubscription = RitsuLibFramework.SubscribeLifecycle<CardExhaustedEvent>(OnCardExhausted);
```

**新方案（Harmony 补丁）：**
- 新增 Harmony Postfix 补丁，拦截 `CardCmd.Exhaust()` 方法
- 卡牌实例通过 `IsAwaitingExhaust` 布尔字段标记自身是否等待消耗事件
- 当卡牌被消耗时，补丁检查该字段并调用 `OnExhausted()` 方法

**新增文件：**
- `TheresaCode/Patches/NightBeforeWarExhaustPatch.cs`

**修改文件：**
- `TheresaCode/Cards/NightBeforeWar.cs`

---

### 3.4 计算型动态变量（`ModCardVars.Computed`）

**原方案（RitsuLib）：**
```csharp
ModCardVars.Computed("Hits", 0m, CalcHits)
```

**新方案（自定义 `DynamicVar` 子类）：**
- 创建继承自 `DynamicVar` 的自定义变量类
- 重写 `UpdateCardPreview()` 方法 — 控制卡牌预览时显示的数值
- 重写 `GetBaseValueForIConvertible()` 方法 — 控制运行时实际读取的数值
- 这种方式与游戏原生的 `CalculatedVar`/`CalculatedDamageVar` 机制一致

**具体实现：**

| 卡牌 | 原变量 | 新自定义类 | 说明 |
|------|--------|-----------|------|
| `CivilightEterna` | `ModCardVars.Computed("ReplayCount", ...)` | 普通 `DynamicVar` | 升级逻辑改为 `UpgradeValueBy(1)` |
| `EternalDust` | `ModCardVars.Computed("Hits", ...)` | `MantraHitsVar` | 根据 `MantraPower` 层数实时计算命中次数 |
| `UnforgivableSin` | `ModCardVars.Computed("TotalDamage", ...)` | `ExhaustBasedDamageVar` | 根据消耗堆牌数计算总伤害 |

**修改文件：**
- `TheresaCode/Cards/CivilightEterna.cs`
- `TheresaCode/Cards/EternalDust.cs`
- `TheresaCode/Cards/UnforgivableSin.cs`

---

### 3.5 初始化入口（`MainFile.cs`）

**移除的内容：**
- `using STS2RitsuLib;`
- `using STS2RitsuLib.Interop;`
- `using Theresa.TheresaCode.Enchantments;`
- `RitsuLibFramework.EnsureGodotScriptsRegistered()` 调用
- `ModTypeDiscoveryHub.RegisterModAssembly()` 调用
- `RitsuLibFramework.GetContentRegistry().RegisterEnchantment()` 调用

**保留的内容：**
- `Harmony.PatchAll()` — 自动加载所有 Harmony 补丁
- `ScriptManagerBridge.LookupScriptsInAssembly()` — 使 `.tscn` 场景能加载自定义 C# 脚本
- `DustInitializer.Initialize()` — 微尘系统初始化

---

## 四、新增文件清单

| 文件路径 | 说明 |
|----------|------|
| `TheresaCode/Patches/NightBeforeWarExhaustPatch.cs` | Harmony Postfix 补丁，替代 RitsuLib 的 `CardExhaustedEvent` 订阅 |

---

## 五、修改文件清单

| 文件路径 | 修改类型 |
|----------|----------|
| `Theresa.csproj` | 移除 RitsuLib 包引用和复制逻辑 |
| `Theresa.json` | 更新依赖列表 |
| `packages/Theresa.csproj` | 同步移除旧配置中的 RitsuLib |
| `MainFile.cs` | 移除所有 RitsuLib 初始化和注册代码 |
| `TheresaCode/Enchantments/SilkThreadEnchantment.cs` | 基类改为 `CustomEnchantmentModel` |
| `TheresaCode/Cards/ApoptosisCountdownEffect.cs` | 用 `AfterCardExhausted()` / `AfterCombatEnd()` 替代事件订阅 |
| `TheresaCode/Cards/FollowTheHistory.cs` | 用 `AfterCardExhausted()` 替代事件订阅 |
| `TheresaCode/Cards/NightBeforeWar.cs` | 用实例标记字段 + 补丁替代事件订阅 |
| `TheresaCode/Cards/CivilightEterna.cs` | 移除 `ModCardVars.Computed`，改用普通 `DynamicVar` |
| `TheresaCode/Cards/EternalDust.cs` | 新增 `MantraHitsVar` 替代 `ModCardVars.Computed` |
| `TheresaCode/Cards/UnforgivableSin.cs` | 新增 `ExhaustBasedDamageVar` 替代 `ModCardVars.Computed` |
| `TheresaCode/Cards/ShatteredFinale.cs` | 移除 `using STS2RitsuLib.Scaffolding.Content;` |
| `TheresaCode/Cards/StoryTeller.cs` | 移除 `using STS2RitsuLib.Cards.DynamicVars;` |

---

## 六、编译验证

```
> dotnet build Theresa.csproj
  Theresa -> ...\Theresa_BaseLib\.godot\mono\temp\bin\Debug\Theresa.dll
  Copying .dll and manifest to mods folder.
  Copying BaseLib to mods folder.
  Copying MinionLib to mods folder.

  已成功生成。
  15 个警告，0 个错误
```

所有警告均为项目原有警告（null 引用检查、未使用本地函数等），与本次迁移无关。

---

## 七、设计差异说明

| 特性 | RitsuLib | BaseLib（新方案） |
|------|----------|-------------------|
| 事件订阅 | 全局事件总线 `SubscribeLifecycle<TEvent>()` | 重写 `AbstractModel` 虚方法 / Harmony 补丁 |
| 附魔注册 | 手动 `RegisterEnchantment<T>()` | 继承 `CustomEnchantmentModel`，自动注册 |
| 计算变量 | `ModCardVars.Computed()` | 自定义 `DynamicVar` 子类 |
| 脚本注册 | `EnsureGodotScriptsRegistered()` | `ScriptManagerBridge.LookupScriptsInAssembly()` |
| 类型发现 | `ModTypeDiscoveryHub.RegisterModAssembly()` | 无需额外操作，`ModelDb.Init()` 自动扫描 |

---

## 八、注意事项

1. **两个版本并行维护**：`Theresa_RitsuLib` 目录未做任何修改，保持原样。
2. **BaseLib 无全局事件总线**：BaseLib 的设计是利用游戏原生的 `Hook` 系统和 `AbstractModel` 虚方法。如果未来需要监听 RitsuLib 特有的事件（如 `CardMovedBetweenPilesEvent`、`GameReadyEvent`），需要评估是否可通过原生 Hook 覆盖，或是否需要额外的 Harmony 补丁。
3. **动态变量预览**：自定义 `DynamicVar` 子类的 `UpdateCardPreview()` 方法决定了卡牌悬停时显示的数值，确保该方法逻辑正确。