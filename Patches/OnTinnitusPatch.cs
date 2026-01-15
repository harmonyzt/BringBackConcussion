using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace BringBackConcussion.Patches
{
    public class OnTinnitusPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.method_88));
        }

        [PatchPrefix]
        private static bool Prefix()
        {
            if (Plugin.TinnitusEffect.Value && !Plugin.MiscMitigateGrenadeFlashTinnitus.Value)
            {
                return true;
            }

            return false;
        }
    }
}