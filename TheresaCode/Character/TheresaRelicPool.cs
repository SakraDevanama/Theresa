using BaseLib.Abstracts;
using Godot;

namespace Theresa.TheresaCode.Character;

public class TheresaRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => Theresa.CharacterId;
    public override Color LabOutlineColor => Theresa.Color;
}