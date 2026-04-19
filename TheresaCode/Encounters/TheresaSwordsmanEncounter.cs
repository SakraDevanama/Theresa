using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Theresa.TheresaCode.Monsters;

namespace Theresa.TheresaCode.Encounters;

/// <summary>
/// 特雷西斯型怪物遭遇
/// </summary>
public class TheresaSwordsmanEncounter : CustomEncounterModel
{
    // 所有可能出现的怪物
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<TheresaSwordsmanMonster>()];

    // 这个遭遇在哪些层级出现
    public override bool IsValidForAct(ActModel act) => act.ActNumber() == 2; // 只在第二幕出现

    // 这个遭遇是否是弱怪池
    public override bool IsWeak => false;

    // 固定给予奖励
    public override bool ShouldGiveRewards => true;

    public TheresaSwordsmanEncounter() : base(RoomType.Monster)
    {
    }

    // 生成怪物列表
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() => [
        (ModelDb.Monster<TheresaSwordsmanMonster>().ToMutable(), null)
    ];
}
