using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;
using System.Reflection.Emit;

namespace AnalogMovement;
internal class FarmerMovementPatches
{
    private const float Deadband = 0.05f;

    private static IMonitor Monitor;

    internal static void Initialize(IMonitor monitor, Harmony harmony)
    {
        Monitor = monitor;

        var updateControlInputDelegate = AccessTools.Method("StardewValley.Game1+<>c__DisplayClass952_0:<UpdateControlInput>b__0");
        updateControlInputDelegate ??= AccessTools.Method("StardewValley.Game1+<>c__DisplayClass978_0:<UpdateControlInput>b__0");
        harmony.Patch(
            original: updateControlInputDelegate!,
            transpiler: new HarmonyMethod(typeof(FarmerMovementPatches), nameof(UpdateControlInputDelegate_Transpiler))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), "MovePositionImpl"),
            prefix: new HarmonyMethod(typeof(FarmerMovementPatches), nameof(MovePositionImpl_Prefix)) { priority = Priority.Low }
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.nextPosition)),
            prefix: new HarmonyMethod(typeof(FarmerMovementPatches), nameof(NextPosition_Prefix)) { priority = Priority.Low }
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.nextPositionHalf)),
            prefix: new HarmonyMethod(typeof(FarmerMovementPatches), nameof(NextPositionHalf_Prefix)) { priority = Priority.Low }
        );
    }

    /// <summary>Patch the default deadband of 0.2 to the value in the Deadband constant</summary>
    internal static IEnumerable<CodeInstruction> UpdateControlInputDelegate_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codeMatcher = new CodeMatcher(instructions);
        codeMatcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Conv_R8),
                new CodeMatch(instruction => instruction.LoadsConstant(0.2) || instruction.LoadsConstant(-0.2))
            )
            .Repeat(cm =>
            {
                Monitor.Log($"Found Load Double Constant at {cm.Pos} with operand {cm.Operand}", LogLevel.Trace);
                var newOperand = Math.CopySign(Deadband, (double)cm.Operand);
                cm.SetOperandAndAdvance(newOperand);
                Monitor.Log($"Patched to {newOperand}", LogLevel.Trace);
            });
        return codeMatcher.Instructions();
    }

    /// <returns>True if the gamepad is enabled and the left is moved out of the Deadzone</returns>
    internal static bool UsingLeftThumbStick()
    {
        if (!Game1.options.gamepadControls)
        {
            return false;
        }
        GamePadState state = Game1.input.GetGamePadState();
        return Math.Abs(state.ThumbSticks.Left.X) > Deadband || Math.Abs(state.ThumbSticks.Left.Y) > Deadband;
    }

    /// <summary>
    /// Adjust movementSpeedX and movementSpeedY to match the angle of the left thumbstick
    /// </summary>
    internal static void ApplyThumbstickAdjustment(ref float movementSpeedX, ref float movementSpeedY)
    {
        if (!UsingLeftThumbStick())
        {
            return;
        }
        GamePadState state = Game1.input.GetGamePadState();
        float x = state.ThumbSticks.Left.X;
        float y = state.ThumbSticks.Left.Y;
        // Apply deadband
        if (Math.Abs(x) < Deadband)
        {
            x = 0;
        }
        if (Math.Abs(y) < Deadband)
        {
            y = 0;
        }
        // Convert thumbstick input into a square and restrict to [-1, 1]
        float maxComp = Math.Max(Math.Abs(x), Math.Abs(y));
        if (maxComp > 0.0)
        {
            x /= maxComp;
            y /= maxComp;
        }
        movementSpeedX *= Math.Abs(x);
        movementSpeedY *= Math.Abs(y);
    }

    internal static void MovePositionImpl_Prefix(Farmer __instance, int direction, ref float movementSpeedX, ref float movementSpeedY)
    {
        if (__instance != Game1.player)
        {
            return;
        }
        ApplyThumbstickAdjustment(ref movementSpeedX, ref movementSpeedY);
    }

    internal static bool NextPosition_Prefix(Farmer __instance, int direction, ref Rectangle __result)
    {
        if (__instance != Game1.player)
        {
            return true;
        }
        Rectangle nextPosition = __instance.GetBoundingBox();

        float movementSpeedX = __instance.getMovementSpeed();
        float movementSpeedY = movementSpeedX;
        ApplyThumbstickAdjustment(ref movementSpeedX, ref movementSpeedY);

        switch (direction)
        {
            case Direction.Up:
                nextPosition.Y -= (int)Math.Ceiling(movementSpeedY);
                break;
            case Direction.Right:
                nextPosition.X += (int)Math.Ceiling(movementSpeedX);
                break;
            case Direction.Down:
                nextPosition.Y += (int)Math.Ceiling(movementSpeedY);
                break;
            case Direction.Left:
                nextPosition.X -= (int)Math.Ceiling(movementSpeedX);
                break;
        }
        __result = nextPosition;
        return false;
    }

    internal static bool NextPositionHalf_Prefix(Farmer __instance, int direction, ref Rectangle __result)
    {
        if (__instance != Game1.player)
        {
            return true;
        }
        Rectangle nextPosition = __instance.GetBoundingBox();
        float movementSpeedX = __instance.getMovementSpeed() / 2.0f;
        float movementSpeedY = movementSpeedX;
        ApplyThumbstickAdjustment(ref movementSpeedX, ref movementSpeedY);

        switch (direction)
        {
            case Direction.Up:
                nextPosition.Y -= (int)Math.Ceiling(movementSpeedY);
                break;
            case Direction.Right:
                nextPosition.X += (int)Math.Ceiling(movementSpeedX);
                break;
            case Direction.Down:
                nextPosition.Y += (int)Math.Ceiling(movementSpeedY);
                break;
            case Direction.Left:
                nextPosition.X -= (int)Math.Ceiling(movementSpeedX);
                break;
        }
        __result = nextPosition;
        return false;
    }

    internal class Direction {
        internal const int Up = 0;
        internal const int Right = 1;
        internal const int Down = 2;
        internal const int Left = 3;
    }
}