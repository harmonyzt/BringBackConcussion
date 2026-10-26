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
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.DoContusion));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ActiveHealthController __instance)
        {
            try
            {
                if (!Plugin.ExplosionConcussion.Value)
                    return true;

                // Only apply to local player
                if (_playerField == null)
                    _playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");

                if (_playerField?.GetValue(__instance) is not Player player || !player.IsYourPlayer)
                    return true;

                bool calledFromExplosion = false;

                // Find where DoContusion was called from
                // If it's shared BSG's explosion method, we skip it
                for (int i = 1; i < 16; i++)
                {
                    MethodBase frame = new StackFrame(i, false).GetMethod();
                    if (frame == null)
                        continue;

                    Type declaringType = frame.DeclaringType;

                    // Never block our own contusion calls
                    if (declaringType == typeof(ConcussionPatch) || declaringType == typeof(ExplosionProximityPatch))
                        return true;

                    if (declaringType == typeof(ExplosionSharedMethods))
                        calledFromExplosion = true;
                }

                if (calledFromExplosion)
                {
                    if (Plugin.VerboseLogging.Value)
                        Logger.LogInfo("[Bring Back Concussion] Suppressed vanilla explosion contusion");

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Plugin.LOGSource.LogError($"[Bring Back Concussion] VanillaExplosionConcussionPatch error: {e}");
                return true;
            }
        }
    }
}