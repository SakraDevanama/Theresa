using HarmonyLib;

namespace Theresa.TheresaCode.Dust;

/// <summary>
/// 微尘系统初始化器
/// </summary>
public static class DustInitializer
{
    public static void Initialize()
    {
        DustCombatHook.Initialize();
    }
}
