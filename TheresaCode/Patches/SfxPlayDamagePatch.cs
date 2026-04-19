using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// Patch SfxCmd.PlayDamage 来安全处理缺少 EnemyImpact_Intensity 参数的音效事件
/// 
/// 某些怪物（如 byrdonis）的 TakeDamageSfx 事件没有 EnemyImpact_Intensity 参数，
/// 直接调用会导致 FMOD 警告："FMOD parameter 'EnemyImpact_Intensity' not found on event"
/// 
/// 修复方案：使用 try-catch 包装 PlayOneShot 调用，忽略参数缺失的警告
/// </summary>
[HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.PlayDamage))]
public static class SfxPlayDamagePatch
{
    [HarmonyPrefix]
    public static bool Prefix(MonsterModel? monster, int damageAmount)
    {
        if (monster == null) return false;

        try
        {
            // 尝试播放带参数的音效
            SfxCmd.Play(monster.TakeDamageSfx, "EnemyImpact_Intensity", 2f);
        }
        catch
        {
            // 如果参数不存在，降级为无参数播放
            SfxCmd.Play(monster.TakeDamageSfx);
        }

        // 跳过原方法
        return false;
    }
}
