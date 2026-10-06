using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BringBackConcussion.Patches
{
    public class OnTinnitusPatch : ModulePatch
    {
        private static FieldInfo _tinnitusField;
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.TryStartContusion));
        }
        
        [PatchPrefix]
        private static bool Prefix(Player __instance, ref float time)
        {
            // Tinnitus is disabled
            if (!Plugin.TinnitusEffect.Value)
            {
                return false;
            }

            // Mitigate tinnitus at all costs if you got contused and flashed at the same time
            if (Plugin.MiscMitigateGrenadeFlashTinnitus.Value && IsFlashed(__instance))
            {
                return false;
            }

            // If "Ignore Equipment Checks" is enabled, bypass whatever fuckery BSG put inside the tinnitus play
            if (Plugin.IgnoreTinnitusEquipmentChecks.Value)
            {
                if (_tinnitusField == null)
                    _tinnitusField = AccessTools.Field(typeof(Player), "_tinnitus");

                var tinnitus = _tinnitusField.GetValue(__instance) as AudioClip;

                Singleton<BetterAudio>.Instance.StartTinnitusEffect(time, tinnitus);

                // skip the original method
                return false;
            }

            // Let the original method fire normally
            return true;
        }

        // Check whether the player is currently flashed
        internal static bool IsFlashed(Player player)
        {
            ActiveHealthController healthController = player.ActiveHealthController;
            if (healthController == null)
                return false;

            ActiveHealthController.Stun stun = healthController.FindActiveEffect<ActiveHealthController.Stun>(EBodyPart.Head);
            return stun != null && stun.Strength > 0f;
        }
    }
}