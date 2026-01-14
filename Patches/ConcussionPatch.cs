using EFT;
using EFT.HealthSystem;
using SPT.Reflection.Patching;
using System.Reflection;

namespace BringBackConcussion.Patches
{
    internal class ConcussionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(Player).GetMethod("ApplyDamageInfo");
        
        [PatchPrefix]
        public static void PatchPrefix(ref EBodyPart bodyPartType, ref DamageInfoStruct damageInfo, ref Player __instance)
        {
            // Not us - do nothing
            if (__instance == null || !__instance.IsYourPlayer || __instance.IsAI) return;
            
            if (bodyPartType == EBodyPart.Head && (!string.IsNullOrEmpty(damageInfo.BlockedBy) || damageInfo.Damage < 10))
            {
                //Plugin.LogSource.LogWarning($"Took damage at {bodyPartType}, damage: {damageInfo.Damage}, blocked by: {damageInfo.BlockedBy}.");
                
                float concussionStrength = Plugin.ConcussionStrength.Value;
                float concussionDuration = Plugin.ConcussionDuration.Value;
                
                ActiveHealthController activeHealthController = __instance.ActiveHealthController;
                activeHealthController.DoContusion(concussionDuration, concussionStrength);
                
                if (Plugin.TinnitusEffect.Value)
                {
                    activeHealthController.DoStun(1, 0);
                }
            }
            else if (bodyPartType == EBodyPart.Head)
            {
                Plugin.LogSource.LogInfo($"No concussion due higher damage. Damage taken at {bodyPartType}, damage: {damageInfo.Damage}, blocked by: {damageInfo.BlockedBy}.");
            }
        }
    }
}
