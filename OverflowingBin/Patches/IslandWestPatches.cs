using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OverflowingBin.ItemBin;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace OverflowingBin.Patches;
internal class IslandWestPatches
{
    private static IMonitor? Monitor;
    private static IslandWestBinLogic? IslandWestBinLogic;

    internal static void Initialize(IMonitor monitor, Harmony harmony, ShippingBinTextures shippingBinTextures)
    {
        Monitor = monitor;

        harmony.Patch(
            original: AccessTools.Method(typeof(IslandWest), nameof(IslandWest.draw)),
            postfix: new HarmonyMethod(typeof(IslandWestPatches), nameof(Draw_Postfix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(IslandWest), "closeShippingBinLid"),
            prefix: new HarmonyMethod(typeof(IslandWestPatches), nameof(CloseShippingBinLid_Prefix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(IslandWest), nameof(IslandWest.showShipment)),
            transpiler: new HarmonyMethod(typeof(IslandWestPatches), nameof(ShowShipment_Transpiler))
        );
        Harmony.ReversePatch(
            original: AccessTools.Method(typeof(IslandWest), "openShippingBinLid"),
            standin: new HarmonyMethod(typeof(IslandWestBinLogic), nameof(IslandWestBinLogic.OpenShippingBinLid_RP))
        );
        Harmony.ReversePatch(
            original: AccessTools.Method(typeof(IslandWest), "isShippingBinLidOpen"),
            standin: new HarmonyMethod(typeof(IslandWestBinLogic), nameof(IslandWestBinLogic.IsShippingBinLidOpen_RP))
        );

        IslandWestBinLogic = new IslandWestBinLogic(shippingBinTextures);
    }

    private static void Draw_Postfix(IslandWest __instance,
        TemporaryAnimatedSprite? ___shippingBinLid,
        SpriteBatch b)
    {
        IslandWestBinLogic!.Draw_Postfix(__instance, ___shippingBinLid, b);
    }

    private static bool CloseShippingBinLid_Prefix(IslandWest __instance,
        TemporaryAnimatedSprite? ___shippingBinLid)
    {
        return IslandWestBinLogic!.CloseShippingBinLid_Prefix(__instance, ___shippingBinLid);
    }

    private static void PatchSpriteLayerDepths(CodeMatcher codeMatcher)
    {
        // Adjust the layer depth for the IslandWest TemporaryAnimatedSprites
        var i = 0;
        codeMatcher.Start()
        .MatchStartForward(new CodeMatch[] {
            new(OpCodes.Ldc_R4),
            new(OpCodes.Stfld, AccessTools.Field(typeof(TemporaryAnimatedSprite), nameof(TemporaryAnimatedSprite.layerDepth)))
        })
        .Repeat(cm =>
        {
            float layerDepthOffset = 0;
            if (i == 0)
            {
                // Full sprite, background layer
                // Move it back more.
                layerDepthOffset = 0.00011f; // Original: 0.00001003f
            }
            else if (i == 1)
            {
                // Bottom half of sprite, foreground layer
                // Move it forward more.
                layerDepthOffset = 0.001601f; //Original: 0.0003f
            }
            else if (i == 2)
            {
                // Item being shipped
                // Move it forward, but keep it behind the bottom half.
                layerDepthOffset = 0.0016f; // Original: 0.00022502f
            }
            i++;

            cm.RemoveInstruction();
            // (float)((this.shippingBinPosition.Y + 1) * 64) / 10000f + layerDepthOffset
            cm.InsertAndAdvance(new CodeInstruction[] {
                new(OpCodes.Ldarg_0), // Load reference to IslandWest
                new(OpCodes.Ldfld, AccessTools.Field(typeof(IslandWest), nameof(IslandWest.shippingBinPosition))),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(Point), nameof(Point.Y))),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Add),
                new(OpCodes.Ldc_I4, 64),
                new(OpCodes.Mul),
                new(OpCodes.Conv_R4),
                new(OpCodes.Ldc_R4, 10000f),
                new(OpCodes.Div),
                new(OpCodes.Ldc_R4, layerDepthOffset),
                new(OpCodes.Add)
            });
        });
        if (i != 3)
        {
            throw new InvalidOperationException(
                "Unable to patch layer depths for ShippingBin's TemporaryAnimatedSprite's"
            );
        }
    }

    private static void CaptureSpriteId(CodeMatcher codeMatcher)
    {
        // Store the id of the TemporaryAnimatedSprites to ShippingBinState.ShippmentSpriteId
        codeMatcher.Start()
        .MatchStartForward(new CodeMatch[] {
            new(inst => inst.IsLdloc()),
            new(OpCodes.Stfld, AccessTools.Field(typeof(TemporaryAnimatedSprite), nameof(TemporaryAnimatedSprite.extraInfoForEndBehavior))),
            new(OpCodes.Dup),
            new(OpCodes.Ldarg_0), // IslandWest is a GameLocation
            new(OpCodes.Ldftn, AccessTools.Method(typeof(GameLocation), nameof(GameLocation.removeTemporarySpritesWithID))),
        })
        .ThrowIfInvalid("Failed to find code for IslandWest's TemporaryAnimatedSprite id");

        var ldlocExtraInfo = codeMatcher.Instruction;
        codeMatcher.Insert(new CodeInstruction[] {
            new(OpCodes.Ldarg_0), // Load reference to the current ShippingBin
            new(OpCodes.Call, AccessTools.Method(typeof(ShippingBinState), nameof(ShippingBinState.GetState))),
            ldlocExtraInfo, // Load sprite id
            new(OpCodes.Call, AccessTools.PropertySetter(typeof(ShippingBinState), nameof(ShippingBinState.ShippmentSpriteId))),
        });
    }

    private static IEnumerable<CodeInstruction> ShowShipment_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codeMatcher = new CodeMatcher(instructions);
        PatchSpriteLayerDepths(codeMatcher);
        CaptureSpriteId(codeMatcher);
        return codeMatcher.Instructions();
    }
}