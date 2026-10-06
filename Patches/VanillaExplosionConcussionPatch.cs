using System;
using System.Diagnostics;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace BringBackConcussion.Patches
{
    /// <summary>
    /// Blocks BSG's own explosion contusion due our custom proximity shit
    /// </summary>
    internal class VanillaExplosionConcussionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.DoContusion));
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            try
            {
                if (!Plugin.ExplosionConcussion.Value)
                    return true;

                // Find where DoContusion was called from
                // If it's shared BSG's method, we skip (unless feature is disabled above)
                for (int i = 1; i < 12; i++)
                {
                    MethodBase frame = new StackFrame(i, false).GetMethod();
                    if (frame == null)
                        continue;

                    if (frame.DeclaringType == typeof(ExplosionSharedMethods))
                        return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Plugin.LOGSource.LogError($"[Bring Back Concussion] VanillaExplosionContusionPatch error: {e}");
                return true;
            }
        }
    }
}