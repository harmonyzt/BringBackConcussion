using System;
using System.Collections;
using EFT;
using EFT.HealthSystem;
using EFT.Ballistics;
using SPT.Reflection.Patching;
using System.Reflection;
using Comfort.Common;
using HarmonyLib;
using Systems.Effects;
using UnityEngine;

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

        protected override MethodBase GetTargetMethod() 
        { 
            return AccessTools.Method(typeof(Player), "ApplyDamageInfo");
        }
        
        [PatchPrefix]
        public static void PatchPrefix(
            EBodyPart bodyPartType, 
            ref DamageInfo damageInfo, 
            Player __instance)
        {
            // Not us - do nothing
            if (__instance == null || !__instance.IsYourPlayer || __instance.IsAI) 
                return;
            
            // Init
            ActiveHealthController activeHealthController = __instance.ActiveHealthController;

            if (activeHealthController == null) 
                return;
            
            if (bodyPartType == EBodyPart.Head && damageInfo is { DamageType: EDamageType.GrenadeFragment } or { DamageType: EDamageType.Bullet })
            {
                // Plugin.LogSource.LogWarning($"Took damage at {bodyPartType}, damage: {damageInfo.Damage}, blocked by: {damageInfo.BlockedBy}.");
                
                float concussionStrength = Plugin.ConcussionStrength.Value;
                float concussionDuration = Plugin.ConcussionDuration.Value;
                
                activeHealthController.DoContusion(concussionDuration, concussionStrength);
                
                if (Plugin.TinnitusEffect.Value && !Plugin.MiscHeadshotBlind.Value)
                {
                    activeHealthController.DoStun(1, 0);
                } 
                else if (Plugin.MiscHeadshotBlind.Value)
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
                
                // Roll for panic based on player's stress resistance
                if (RollForPanicValues.ShouldPanic(__instance))
                {
                    activeHealthController.AddEffect<ActiveHealthController.PanicEffect>(EBodyPart.Head, delayTime: 0f, workTime: null);
                    activeHealthController.AddEffect<ActiveHealthController.MisfireEffect>(EBodyPart.Head, delayTime: 0f, workTime: null);
                    __instance.StartCoroutine(RemovePanicEffectsAfterDelay(activeHealthController));
                }
            }
            // Grenade Explosion
            else if (damageInfo is { DamageType: EDamageType.GrenadeFragment } or {DamageType: EDamageType.Explosion} or {DamageType: EDamageType.Artillery})
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
                
                // Roll for panic based on stress resistance
                if (RollForPanicValues.ShouldPanic(__instance))
                {
                    activeHealthController.AddEffect<ActiveHealthController.PanicEffect>(EBodyPart.Head, delayTime: 0f, workTime: null);
                    activeHealthController.AddEffect<ActiveHealthController.MisfireEffect>(EBodyPart.Head, delayTime: 0f, workTime: null);
                    __instance.StartCoroutine(RemovePanicEffectsAfterDelay(activeHealthController));
                }
            }
        }

        private static IEnumerator RemovePanicEffectsAfterDelay(ActiveHealthController healthController)
        {
            float delay = UnityEngine.Random.Range(5f, 8f);
            yield return new WaitForSeconds(delay);

            healthController.RemoveEffect<ActiveHealthController.PanicEffect>(EBodyPart.Head);
            healthController.RemoveEffect<ActiveHealthController.MisfireEffect>(EBodyPart.Head);

            //Plugin.LOGSource.LogInfo($"[ConcussionPatch] Removed panic effects after {delay:F1}s");
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