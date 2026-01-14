using System;
using EFT;
using EFT.HealthSystem;
using EFT.UI;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.CameraControl;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using Random = System.Random;

namespace BringBackConcussion.Patches
{
    public class OnDiedPatch : ModulePatch
    {
        // Due to whatever reason this method triggering two times
        private static bool _soundPlayed = false;
        private static DateTime _lastPlayTime = DateTime.MinValue;
        private static readonly TimeSpan CooldownTime = TimeSpan.FromSeconds(10);
        private static ArmorHitSoundPlayer _cachedArmorHitSoundPlayer;
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.Kill));
        }

        [PatchPrefix]
        private static async void Prefix(ActiveHealthController __instance, EDamageType damageType)
        {
            try
            {
                FieldInfo playerField = AccessTools.Field(typeof(ActiveHealthController), "Player");
                    
                // Allow AI to die normally
                if (playerField?.GetValue(__instance) is not Player player || 
                    player.IsAI) 
                    return;
                
                // Check cooldown
                if (_soundPlayed && DateTime.Now - _lastPlayTime < CooldownTime)
                {
                    return;
                }
                
                // Reset the flag
                if (DateTime.Now - _lastPlayTime >= CooldownTime)
                {
                    _soundPlayed = false;
                }
            
                // Headshotted into death
                if (
                    damageType == EDamageType.Bullet && 
                    Plugin.EnableHSSound.Value &&
                    __instance.GetBodyPartHealth(EBodyPart.Head, true).Current < 1 
                    )
                {
                    var armorHitPlayer = GetArmorHitSoundPlayer();
                    var fpSoundsField = AccessTools.Field(typeof(ArmorHitSoundPlayer), "_fpSounds");
                    var fpSounds = fpSoundsField.GetValue(armorHitPlayer) as AudioClip[];
                    
                    // 4 sounds in array in total
                    int randomIndex = UnityEngine.Random.Range(0, fpSounds.Length);
                    AudioClip selectedSound = fpSounds[randomIndex];
                    
                    Singleton<BetterAudio>.Instance.PlayNonspatial(
                        fpSounds[randomIndex],
                        BetterAudio.AudioSourceGroupType.Impacts,
                        0.5f,  // sound position
                        2f,
                        null);
                
                    _lastPlayTime = DateTime.Now;
                    
                    //Logger.LogInfo($"[Bring Back Concussion] Selected sound index: {randomIndex}, Name: {selectedSound.name}");
                }
            
                // Play UI sound
                if (Plugin.PlayDeathUISound.Value && !_soundPlayed)
                {
                    _soundPlayed = true;
                    _lastPlayTime = DateTime.Now;
                
                    // Simulate Live death UI sound (afair it plays with some kind of delay)
                    var random = new Random();
                    int delayMilliseconds = random.Next(1000, 4000);

                    await Task.Delay(delayMilliseconds);
                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.PlayerIsDead);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"[Bring Back Concussion] OnDiedPatch error: {e.Message}");
            }
        }
        
        private static ArmorHitSoundPlayer GetArmorHitSoundPlayer()
        {
            if (_cachedArmorHitSoundPlayer != null)
            {
                return _cachedArmorHitSoundPlayer;
            }
            
            // ArmorHitSoundPlayer
            _cachedArmorHitSoundPlayer = GameObject.FindObjectOfType<ArmorHitSoundPlayer>();
            
            // CameraController
            if (_cachedArmorHitSoundPlayer == null)
            {
                var cameraController = Singleton<PlayerCameraController>.Instance;
                if (cameraController != null)
                {
                    _cachedArmorHitSoundPlayer = cameraController.GetComponent<ArmorHitSoundPlayer>();
                    if (_cachedArmorHitSoundPlayer == null)
                    {
                        _cachedArmorHitSoundPlayer = cameraController.GetComponentInChildren<ArmorHitSoundPlayer>();
                    }
                }
            }
            
            return _cachedArmorHitSoundPlayer;
        }
    }
}