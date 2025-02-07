using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;

namespace OverflowingBin.ItemBin;
internal class IslandWestBinLogic : ItemBinLogic<IslandWest>
{
    private readonly ShippingBinTextures ShippingBinTextures;

    internal IslandWestBinLogic(ShippingBinTextures shippingBinTextures)
    {
        ShippingBinTextures = shippingBinTextures;
    }

    protected override bool CanDraw(IslandWest instance) => instance.farmhouseRestored.Value;

    protected override float GetAlpha(IslandWest instance) => 1.0f;

    protected override Color GetColor(IslandWest instance) => Color.White;

    protected override GameLocation GetGameLocation(IslandWest instance) => instance;

    protected override Rectangle GetSourceRect(IslandWest instance) => new Rectangle(0, 0, 32, 32);

    protected override Texture2D GetTexture() => ShippingBinTextures.IslandWestBinFront!.Value;

    protected override Vector2 GetTilePosition(IslandWest instance)
    {
        var point = instance.shippingBinPosition;
        return new Vector2(point.X, point.Y);
    }

    protected override Vector2 GetTileSize(IslandWest instance) => new Vector2(2, 1);

    internal override bool IsShippingBinLidOpen(IslandWest instance, bool requiredToBeFullyOpen)
    {
        return IsShippingBinLidOpen_RP(instance, requiredToBeFullyOpen);
    }

    internal override void OpenShippingBinLid(IslandWest instance)
    {
        OpenShippingBinLid_RP(instance);
    }

    internal static bool IsShippingBinLidOpen_RP(IslandWest instance, bool requiredToBeFullyOpen)
    {
        throw new NotImplementedException("Reverse patched in IslandWestPatches");
    }

    internal static void OpenShippingBinLid_RP(IslandWest instance)
    {
        throw new NotImplementedException("Reverse patched in IslandWestPatches");
    }
}
