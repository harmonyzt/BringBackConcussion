using System.Reflection;
using Comfort.Common;
using EFT;
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
            // Mitigate tinnitus at all costs if you got contused and flashed at the same time
            if (Plugin.TinnitusEffect.Value && !Plugin.MiscMitigateGrenadeFlashTinnitus.Value)
            {
                return true;
            }
            
            // if the "Ignore Equipment Checks" is enabled, bypass whatever fuckery BSG put inside the tinnitus play
            if (Plugin.IgnoreTinnitusEquipmentChecks.Value)
            {
                if (_tinnitusField == null)
                    _tinnitusField = AccessTools.Field(typeof(Player), "_tinnitus");

                var tinnitus = _tinnitusField.GetValue(__instance) as AudioClip;
                
                Singleton<BetterAudio>.Instance.StartTinnitusEffect(time, tinnitus);
                
                // skip the original method
                return false;
            }

            return false;
        }
    }
}