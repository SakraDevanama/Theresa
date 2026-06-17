using System.Collections.Generic;
using System.Reflection;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Minions;
using Theresa.TheresaCode.Patches;
using Theresa.TheresaCode.Utils;

namespace Theresa;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Theresa"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static int MainThreadId { get; private set; }

    public static void Initialize()
    {
        MainThreadId = System.Environment.CurrentManagedThreadId;

        Harmony harmony = new(ModId);

        harmony.PatchAll(Assembly.GetExecutingAssembly());
        // 使得tscn可以加载自定义脚本
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(MainFile).Assembly);
        
        // 初始化微尘系统
        DustInitializer.Initialize();
        
        // 初始化 UnknownRelic Action 计数补丁
        UnknownRelicActionCounterPatch.Initialize();
        
        // 预加载随从资源（Spine场景 + 音效），避免召唤时同步加载导致卡顿
        MinionAssetPreloader.PreloadAll();
        
        // 注册 RemovedCardsTracker 的扩展保存，使被移除的卡牌在存档/读档时持久化
        ExtendedSaveHandlers<IRunState, SerializableRun>.RegisterSave(
            id: "Theresa_RemovedCards",
            getter: runState =>
            {
                var cards = RemovedCardsTracker.RemovedCards;
                if (cards.Count == 0) return null;
                return new List<SerializableCard>(cards);
            },
            setter: (runState, data) =>
            {
                RemovedCardsTracker.RestoreFromSave(data);
            },
            serializer: (list, writer) =>
            {
                if (list == null)
                {
                    writer.WriteBool(false);
                    return;
                }
                writer.WriteBool(true);
                writer.WriteInt(list.Count);
                foreach (var card in list)
                {
                    if (card == null)
                    {
                        writer.WriteBool(false);
                    }
                    else
                    {
                        writer.WriteBool(true);
                        card.Serialize(writer);
                    }
                }
            },
            deserializer: (reader) =>
            {
                bool exists = reader.ReadBool();
                if (!exists) return null;
                int count = reader.ReadInt();
                var list = new List<SerializableCard>(count);
                for (int i = 0; i < count; i++)
                {
                    bool hasCard = reader.ReadBool();
                    if (hasCard)
                    {
                        var card = new SerializableCard();
                        card.Deserialize(reader);
                        list.Add(card);
                    }
                }
                return list;
            }
        );
        
        Log.Debug("Mod initialized!");
    }
}
