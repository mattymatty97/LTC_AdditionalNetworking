using HarmonyLib;

namespace AdditionalNetworking.Patches.Debug;

[HarmonyPatch]
internal class DebugPatches
{
    /*
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.AimGun), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> ReduceDelay(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        for (var i = 0; i < codes.Count; i++)
        {
            var curr = codes[i];
            if (curr.LoadsConstant(1.3f))
                curr.operand = 0.1f;
        }

        return codes;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
    private static void Always1Shell(ref int ___shellsLoaded)
    {
        ___shellsLoaded = 1;
    }*/
}