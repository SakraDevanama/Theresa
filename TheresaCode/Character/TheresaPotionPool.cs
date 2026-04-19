using BaseLib.Abstracts;
using Godot;

namespace Theresa.TheresaCode.Character;

public class TheresaPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => Theresa.CharacterId;
    public override Color LabOutlineColor => Theresa.Color;
}