using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;

namespace Theresa.TheresaCode.Dust;

/// <summary>
/// 微尘战斗钩子 - 订阅战斗事件。
/// </summary>
public static class DustCombatHook
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        CombatManager.Instance.TurnEnded += OnTurnEnded;
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    public static void Deinitialize()
    {
        if (!_initialized) return;
        _initialized = false;

        CombatManager.Instance.TurnEnded -= OnTurnEnded;
        CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
    }

    private static void OnTurnEnded(CombatState combatState)
    {
        if (combatState.CurrentSide == CombatSide.Enemy)
        {
            foreach (var player in combatState.Players)
            {
                DustManager.AtTurnEnd(player);
            }
        }
    }

    private static void OnCombatSetUp(CombatState combatState)
    {
        foreach (var player in combatState.Players)
        {
            DustManager.PreBattle(player);
        }
    }

    private static void OnCombatEnded(CombatRoom combatRoom)
    {
        foreach (var player in combatRoom.CombatState.Players)
        {
            DustManager.PostBattle(player);
        }
    }
}
