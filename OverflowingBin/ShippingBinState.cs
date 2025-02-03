using System.Runtime.CompilerServices;
using StardewValley.Buildings;

namespace OverflowingBin;
internal class ShippingBinState
{
    private static readonly ConditionalWeakTable<ShippingBin, ShippingBinState> StateTable = new();

    public bool CanCloseLid = true;

    public bool HasShippmentSpriteId = false;
    private int _shippmentSpriteId = 0;
    public int ShippmentSpriteId
    {
        get => _shippmentSpriteId;
        set
        {
            _shippmentSpriteId = value;
            HasShippmentSpriteId = true;
        }
    }

    public static ShippingBinState GetState(ShippingBin instance)
    {
        return StateTable.GetOrCreateValue(instance);
    }
}