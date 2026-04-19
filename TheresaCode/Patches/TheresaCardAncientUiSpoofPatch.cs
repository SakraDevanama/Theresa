using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Theresa.TheresaCode.Cards;

namespace Theresa.TheresaCode.Patches;

// 补丁1：修改 CardModel.Rarity 属性的 getter
// 目标类：CardModel，目标方法：Rarity 属性的 Getter
[HarmonyPatch(typeof(CardModel), "Rarity", MethodType.Getter)]
public static class TheresaCardAncientUiSpoofPatch
{
    // 这个后置补丁方法会在 CardModel.Rarity 的原始 getter 执行后运行
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref CardRarity __result)
    {
        // 已禁用：不再欺骗 Ancient 稀有度
        // 所有 Theresa 卡牌现在都使用 AncientPortrait 显示卡图，但保持真实稀有度
        return;
        
        /* 原始代码（已禁用）：
        // 检查是否是 Theresa 卡牌
        if (!IsTheresaCard(__instance))
        {
            return;
        }

        // 检查当前调用栈是否来自允许的 UI 组件
        if (TheresaCardUtils.ShouldSpoofForUi())
        {
            // 欺骗稀有度为 Ancient
            __result = CardRarity.Ancient;
        }
        */
    }

    // 一个私有方法，用于判断 CardModel 是否是 TheresaCardModel 的实例
    private static bool IsTheresaCard(CardModel model)
    {
        // 使用 is 操作符进行类型检查
        return model is TheresaCardModel;
    }
}

// 补丁2：修改 NTinyCard.GetBannerColor 方法
// 这是为了修复在一些小尺寸卡牌图标（如牌组历史、商店预览）中，边框颜色不跟随稀有度变化的问题。
// 目标类：NTinyCard，目标方法：GetBannerColor
[HarmonyPatch(typeof(NTinyCard), "GetBannerColor")]
public static class TheresaTinyCardBannerColorPatch
{
    // 这个后置补丁方法会在 NTinyCard.GetBannerColor 的原始方法执行后运行
    [HarmonyPostfix]
    public static void Postfix(NTinyCard __instance, ref Color __result, CardRarity rarity)
    {
        // 已禁用：不再欺骗 Ancient 稀有度颜色
        // 所有 Theresa 卡牌现在都使用 AncientPortrait 显示卡图，但保持真实稀有度
        return;
        
        /* 原始代码（已禁用）：
        // 获取 NTinyCard 实例中的 CardModel
        CardModel? model = GetCardModelFromInstance(__instance);
        
        // 如果找不到模型，或者不是 Theresa 卡牌，直接返回
        if (model == null || !IsTheresaCard(model))
        {
            return;
        }

        // 检查当前调用栈是否来自允许的 UI 组件
        if (TheresaCardUtils.ShouldSpoofForUi())
        {
            // 欺骗为 Ancient 稀有度的颜色
            __result = new Color(0.5f, 0.0f, 0.5f, 1.0f); // 紫色，Ancient 稀有度的颜色
        }
        */
    }

    // 一个私有静态方法，用于通过反射从 NTinyCard 实例中获取 CardModel
    private static CardModel? GetCardModelFromInstance(NTinyCard instance)
    {
        // 定义可能存储卡牌模型的字段名列表
        // 这些是常见的命名约定，可以根据实际反编译结果调整
        string[] possibleFieldNames = { "model", "_model", "cardModel", "_cardModel", "Model" };

        Type instanceType = instance.GetType();
        foreach (string fieldName in possibleFieldNames)
        {
            // 尝试获取指定名称的字段
            FieldInfo? field = instanceType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType.IsAssignableTo(typeof(CardModel)))
            {
                // 如果找到了，并且它的类型是 CardModel 或其子类
                object? value = field.GetValue(instance);
                if (value is CardModel cardModel)
                {
                    return cardModel; // 返回找到的模型
                }
            }

            // 如果字段没找到，尝试查找属性
            PropertyInfo? property = instanceType.GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.PropertyType.IsAssignableTo(typeof(CardModel)))
            {
                // 如果找到了，并且它的类型是 CardModel 或其子类
                object? value = property.GetValue(instance);
                if (value is CardModel cardModel)
                {
                    return cardModel; // 返回找到的模型
                }
            }
        }

        // 如果遍历完所有可能的名字都没找到，返回 null
        return null;
    }

    // 一个辅助方法，用于判断 CardModel 是否是 TheresaCardModel 的实例
    private static bool IsTheresaCard(CardModel model)
    {
        return model is TheresaCardModel;
    }
}