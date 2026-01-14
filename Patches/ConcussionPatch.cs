using System;
using EFT;
using EFT.HealthSystem;
using SPT.Reflection.Patching;
using System.Reflection;
using Comfort.Common;
using EFT.Ballistics;
using Systems.Effects;

namespace BringBackConcussion.Patches
{
    internal class ConcussionPatch : ModulePatch
    {
        private static readonly MaterialType[] HeadshotMaterials = 
        {
            MaterialType.Helmet,
            MaterialType.GlassVisor
        };
        
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
                
                // Play crack sound effect upon head hit
                // Get effects instance
                var effectsInstance = GetEffectsInstance();
                if (effectsInstance == null)
                {
                    Logger.LogError("[Bring Back Concussion] Effects instance not found!");
                    return;
                }
                
                // Get the material field
                int randomIndex = UnityEngine.Random.Range(0, HeadshotMaterials.Length);
                MaterialType selectedMaterial = HeadshotMaterials[randomIndex];
                    
                effectsInstance.EmitPlayerSoundOnly(
                    selectedMaterial,
                    __instance,
                    1.0f,
                    null
                );
                
            }
            else if (bodyPartType == EBodyPart.Head)
            {
                Plugin.LogSource.LogInfo($"No concussion due higher damage. Damage taken at {bodyPartType}, damage: {damageInfo.Damage}, blocked by: {damageInfo.BlockedBy}.");
            }
        }
        private static Effects GetEffectsInstance()
        {
            try
            {
                var effectsInstance = Singleton<Effects>.Instance;
                
                if (effectsInstance != null)
                {
                    return effectsInstance;
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"[Bring Back Concussion] GetEffectsInstance error: {e.Message}");
            }
            
            return null;
        }
    }
}
