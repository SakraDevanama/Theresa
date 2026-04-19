# DustManager/NDustRing 悬停预览与卡框效果实现记录

**日期**: 2026-04-16  
**任务**: 让 `DustManager` 的微尘卡牌拥有和 Java 原版视频一样的环绕+悬停展开效果

---

## 1. 需求分析

用户提供了游戏视频，展示了以下核心效果：
- **平时**: 微尘卡牌以小图形式环绕角色飞行
- **悬停时**: 卡牌展开变大，显示完整的卡牌信息（卡框、费用、名称、描述）
- **Tooltip**: 鼠标悬停角色时显示 "微尘上限" 提示

用户强调 **环绕运动逻辑已经是完美的**，不需要修改，重点在：
1. 悬停展开预览
2. 给环绕卡牌加上卡框（像 Java 版用完整 `AbstractCard` 一样）
3. 显示 Tooltip

---

## 2. 实现过程

### 2.1 初始问题

`NDustRing` 之前使用 `Sprite2D` 显示卡牌肖像图，只能看到中间图片，没有卡框、费用、文字等信息。悬停时也缺少展开效果。

### 2.2 方案选择

经过讨论，决定：
- **用 `NCard` 完全替代 `Sprite2D`** 来做环绕和预览
- 保留用户原有的完美环绕坐标计算
- 同一组 `NCard` 在"环绕模式"和"预览模式"之间平滑过渡

### 2.3 关键修改

#### `TheresaCode/Dust/Nodes/NDustRing.cs`

完全重写，核心逻辑：
- 维护 `List<NCard>` 列表，从 `NodePool` 获取/归还
- 使用前后两个 `Node2D` 容器（`DustBackContainer` / `DustFrontContainer`）控制渲染深度
- `_Process` 中根据 `_parentCreature.IsFocused` 切换模式：
  - **环绕模式**: 复刻原有椭圆轨道计算，`Scale = CircleDrawScale * 2f`（后调整为 1.6 倍）
  - **预览模式**: 卡牌排列成网格，显示在屏幕中央偏左上方（玩家头顶附近）
- 使用 `Lerp` 实现平滑过渡

#### `TheresaCode/Dust/Patches/DustHoverTipPatch.cs`

新增 Harmony Patch：
- Patch `Creature.HoverTips` getter
- 当玩家有微尘时，在提示列表最前面插入 "微尘上限" 提示

#### 本地化文件

`Theresa/localization/zhs/static_hover_tips.json` 和 `eng` 版本：
- 新增 `THERESA-DUSTLIMIT.title` / `THERESA-DUSTLIMIT.description`

### 2.4 遇到的问题与修复

| 问题 | 原因 | 修复 |
|------|------|------|
| 编译错误 (`MouseFilterEnum` / `ToLocal`) | `Control` 与 `Node2D` API 差异 | 将预览容器改为 `Node2D` |
| 预览卡牌显示 "Broken Card" / "If you can read this, there is a bug" | 只调用了 `NCard.Reload()`，没调 `UpdateVisuals()` | 在设置 `Model` 后补调 `UpdateVisuals(PileType.None, CardPreviewMode.Normal)` |
| 环绕位置偏移 | 使用了不同的坐标计算和 `PivotOffset` | 完全恢复用户原有的坐标逻辑，不设置 `PivotOffset`，`Position` 直接对齐 |
| 卡牌太小 | `CircleDrawScale = 0.10f` 对 `NCard` 来说偏小 | 在环绕缩放后乘以 `2f`（最终用户调整为 `1.6f`） |

---

## 3. 最终效果

- 平时：3张（或更多）微尘卡牌带完整卡框环绕角色飞行，有前后深度和倾斜角度
- 悬停：卡牌平滑移动到玩家头顶左上方，放大到 0.7 倍显示完整信息
- Tooltip：正确显示 "微尘上限 你当前拥有X个微尘上限"

---

## 4. 相关文件

- `TheresaCode/Dust/Nodes/NDustRing.cs`
- `TheresaCode/Dust/Patches/DustHoverTipPatch.cs`
- `Theresa/localization/zhs/static_hover_tips.json`
- `Theresa/localization/eng/static_hover_tips.json`

---

## 5. 备注

- 环绕直径可通过调整 `NDustRing.cs` 中 `width = 110f * 1.1f + cardWidth` 的 `110f` 基数来改变
- 卡牌大小由 `CircleDrawScale` 控制，当前用户本地已调整为 1.6 倍
