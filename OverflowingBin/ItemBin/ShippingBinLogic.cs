using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;

namespace OverflowingBin.ItemBin;
internal class ShippingBinLogic : ItemBinLogic<ShippingBin>
{
    private readonly ShippingBinTextures ShippingBinTextures;

    internal ShippingBinLogic(ShippingBinTextures shippingBinTextures)
    {
        ShippingBinTextures = shippingBinTextures;
    }

    protected override bool CanDraw(ShippingBin instance)
    {

        if (instance.isMoving)
        {
            return false;
        }
        if (instance.daysOfConstructionLeft.Value > 0)
        {
            return false;
        }
        return true;
    }

    protected override float GetAlpha(ShippingBin instance) => instance.alpha;

    protected override Color GetColor(ShippingBin instance) => instance.color;

    protected override GameLocation GetGameLocation(ShippingBin instance) =>
        instance.GetParentLocation();

    protected override Rectangle GetSourceRect(ShippingBin instance) => instance.getSourceRect();

    protected override Texture2D GetTexture() => ShippingBinTextures.ShippingBinFront!.Value;

    protected override Vector2 GetTilePosition(ShippingBin instance) =>
        new Vector2(instance.tileX.Value, instance.tileY.Value);

    protected override Vector2 GetTileSize(ShippingBin instance) =>
        new Vector2(instance.tilesWide.Value, instance.tilesHigh.Value);

    internal override bool IsShippingBinLidOpen(ShippingBin instance, bool requiredToBeFullyOpen)
    {
        return IsShippingBinLidOpen_RP(instance, requiredToBeFullyOpen);
    }

    internal override void OpenShippingBinLid(ShippingBin instance)
    {
        OpenShippingBinLid_RP(instance);
    }

    internal static bool IsShippingBinLidOpen_RP(ShippingBin instance, bool requiredToBeFullyOpen)
    {
        throw new NotImplementedException("Reverse patched in ShippingBinPatches");
    }

    internal static void OpenShippingBinLid_RP(ShippingBin instance)
    {
        throw new NotImplementedException("Reverse patched in ShippingBinPatches");
    }
}
