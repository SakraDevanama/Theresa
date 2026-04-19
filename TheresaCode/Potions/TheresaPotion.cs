using BaseLib.Abstracts;
using BaseLib.Utils;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Potions;

[Pool(typeof(TheresaPotionPool))]
public abstract class TheresaPotion : CustomPotionModel;