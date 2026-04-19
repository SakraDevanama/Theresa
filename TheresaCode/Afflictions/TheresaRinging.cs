using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Afflictions;

/// <summary>
/// 特雷西斯版耳鸣 affliction（复刻原版 Ringing）
/// </summary>
public sealed class TheresaRinging : AfflictionModel
{
    public override bool HasExtraCardText => true;
}
