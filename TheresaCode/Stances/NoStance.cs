using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Stances;

#pragma warning disable STS001


public class NoStance : StancePower
#pragma warning restore STS001
{
    protected override bool IsVisibleInternal => false;
}