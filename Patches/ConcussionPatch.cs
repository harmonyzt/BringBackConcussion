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
            MaterialType.GlassVisor,
            MaterialType.HelmetRicochet
        };

        private static Effects _cachedEffectsInstance;
        
        protected override MethodBase GetTargetMethod() => typeof(Player).GetMethod("ApplyDamageInfo");
        
        [PatchPrefix]
        public static void PatchPrefix(ref EBodyPart bodyPartType, ref DamageInfoStruct damageInfo, ref Player __instance)
        {
            // Not us - do nothing
            if (__instance == null || !__instance.IsYourPlayer || __instance.IsAI) return;
            
            // Init
            ActiveHealthController activeHealthController = __instance.ActiveHealthController;
            
            if (bodyPartType == EBodyPart.Head && damageInfo is { DamageType: EDamageType.Bullet} && (!string.IsNullOrEmpty(damageInfo.BlockedBy) || damageInfo.Damage < 10))
            {
                // Plugin.LogSource.LogWarning($"Took damage at {bodyPartType}, damage: {damageInfo.Damage}, blocked by: {damageInfo.BlockedBy}.");
                
                float concussionStrength = Plugin.ConcussionStrength.Value;
                float concussionDuration = Plugin.ConcussionDuration.Value;
                
                activeHealthController.DoContusion(concussionDuration, concussionStrength);
                
                if (Plugin.TinnitusEffect.Value)
                {
                    activeHealthController.DoStun(1, 0);
                } else if (Plugin.MiscHeadshotBlind.Value)
                {
                    activeHealthController.DoStun(1, Plugin.MiscHeadshotStrengthEffect.Value);
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
                
                if (!Plugin.MiscPickRandomSound.Value)
                {
                    selectedMaterial = MaterialType.GlassVisor;
                }
                
                effectsInstance.EmitPlayerSoundOnly(
                    selectedMaterial,
                    __instance,
                    2.0f,
                    null
                );
            }
            // Grenade Explosion
            else if (damageInfo is { DamageType: EDamageType.GrenadeFragment })
            {
                
                // Plugin.LogSource.LogWarning($"Grenade hit! Trying to apply blindness...");
                
                // Apply blindness
                if (Plugin.MiscGrenadeBlind.Value)
                {
                    activeHealthController.DoStun(1.0f, Plugin.MiscBlindnessStrengthEffect.Value);
                }
                
                // Apply concussion
                if (Plugin.MiscGrenadeStun.Value)
                {
                    float concussionStrength = Plugin.ConcussionStrength.Value;
                    float concussionDuration = Plugin.ConcussionDuration.Value;

                    activeHealthController.DoContusion(concussionDuration, concussionStrength);
                }
            }
        }
        
        private static Effects GetEffectsInstance()
        {
            try
            {
                if (_cachedEffectsInstance != null)
                {
                    return _cachedEffectsInstance;
                }

                _cachedEffectsInstance = Singleton<Effects>.Instance;
                return _cachedEffectsInstance;
            }
            catch (Exception e)
            {
                Logger.LogError($"[Bring Back Concussion] GetEffectsInstance error: {e.Message}");
            }
            
            return null;
        }
    }
}
