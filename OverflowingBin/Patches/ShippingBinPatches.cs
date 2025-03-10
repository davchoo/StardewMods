using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using OverflowingBin.ItemBin;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;

namespace OverflowingBin.Patches;
internal class ShippingBinPatches
{
    private static IMonitor? Monitor;
    private static ShippingBinLogic? ShippingBinLogic;

    internal static void Initialize(IMonitor monitor, Harmony harmony, ShippingBinTextures shippingBinTextures)
    {
        Monitor = monitor;

        harmony.Patch(
            original: AccessTools.Method(typeof(ShippingBin), nameof(ShippingBin.draw)),
            postfix: new HarmonyMethod(typeof(ShippingBinPatches), nameof(Draw_Postfix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(ShippingBin), "closeShippingBinLid"),
            prefix: new HarmonyMethod(typeof(ShippingBinPatches), nameof(CloseShippingBinLid_Prefix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(ShippingBin), nameof(ShippingBin.showShipment)),
            transpiler: new HarmonyMethod(typeof(ShippingBinPatches), nameof(ShowShipment_Transpiler))
        );
        Harmony.ReversePatch(
            original: AccessTools.Method(typeof(ShippingBin), "openShippingBinLid"),
            standin: new HarmonyMethod(typeof(ShippingBinLogic), nameof(ShippingBinLogic.OpenShippingBinLid_RP))
        );
        Harmony.ReversePatch(
            original: AccessTools.Method(typeof(ShippingBin), "isShippingBinLidOpen"),
            standin: new HarmonyMethod(typeof(ShippingBinLogic), nameof(ShippingBinLogic.IsShippingBinLidOpen_RP))
        );

        ShippingBinLogic = new ShippingBinLogic(shippingBinTextures);
    }

    private static void Draw_Postfix(ShippingBin __instance,
        TemporaryAnimatedSprite? ___shippingBinLid,
        SpriteBatch b)
    {
        ShippingBinLogic!.Draw_Postfix(__instance, ___shippingBinLid, b);
    }

    private static bool CloseShippingBinLid_Prefix(ShippingBin __instance,
        TemporaryAnimatedSprite? ___shippingBinLid)
    {
        return ShippingBinLogic!.CloseShippingBinLid_Prefix(__instance, ___shippingBinLid);
    }

    private static void PatchSpriteLayerDepths(CodeMatcher codeMatcher)
    {
        // Adjust the layer depth for the ShippingBin TemporaryAnimatedSprites
        var i = 0;
        codeMatcher.Start()
        .MatchStartForward(new CodeMatch[] {
            new(inst => inst.LoadsConstant(10000f)),
            new(OpCodes.Div),
            new(OpCodes.Ldc_R4),
            new(OpCodes.Add),
            new(OpCodes.Stfld, AccessTools.Field(typeof(TemporaryAnimatedSprite), nameof(TemporaryAnimatedSprite.layerDepth)))
        })
        .Repeat(cm =>
        {
            cm.Advance(2); // Advance to ldc.r4 <layer depth offset>
            if (i == 0)
            {
                // Full sprite, background layer
                // Move it back more.
                cm.SetOperandAndAdvance(0.00011f); // Original: 0.0002f
            }
            else if (i == 1)
            {
                // Bottom half of sprite, foreground layer
                // Move it forward more.
                cm.SetOperandAndAdvance(0.001601f); //Original: 0.0003f
            }
            else if (i == 2)
            {
                // Item being shipped
                // Move it forward, but keep it behind the bottom half.
                cm.SetOperandAndAdvance(0.0016f); // Original: 0.000225
            }
            i++;
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
            new(inst => inst.IsLdloc()),
            new(OpCodes.Ldftn, AccessTools.Method(typeof(GameLocation), nameof(GameLocation.removeTemporarySpritesWithID))),
        })
        .ThrowIfInvalid("Failed to find code for ShippingBin's TemporaryAnimatedSprite id");

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