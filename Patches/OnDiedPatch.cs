using System;
using System.Reflection;
using System.Threading.Tasks;
using Systems.Effects;
using EFT;
using EFT.HealthSystem;
using EFT.UI;
using EFT.Ballistics;
using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;
using Random = System.Random;

namespace BringBackConcussion.Patches
{
    public class OnDiedPatch : ModulePatch
    {
        // Due to whatever reason this method triggering two times we use timers for deaths
        private static bool _soundPlayed;
        private static DateTime _lastPlayTime = DateTime.MinValue;
        private static readonly TimeSpan CooldownTime = TimeSpan.FromSeconds(10);

        private static Effects _cachedEffectsInstance;

        private static readonly MaterialType[] HeadshotMaterials =
        {
            MaterialType.Helmet,
            MaterialType.GlassVisor,
            MaterialType.HelmetRicochet
        };

        private static readonly FieldInfo PlayerField = AccessTools.Field(typeof(ActiveHealthController), "Player");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.Kill));
        }

        [PatchPrefix]
        private static async void Prefix(ActiveHealthController __instance, EDamageType damageType)
        {
            try
            {
                // Allow AI to die normally
                if (PlayerField?.GetValue(__instance) is not Player player ||
                    player.IsAI ||
                    !player.IsYourPlayer)
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
                // BSGs headshots on death didn't track whether the helmet was equipped, so I am freeing myself from another 5 hour torture :kekw:
                if (
                    damageType is EDamageType.Bullet or EDamageType.Sniper &&
                    Plugin.EnableHSSound.Value &&
                    __instance.GetBodyPartHealth(EBodyPart.Head, true).Current < 1
                )
                {
                    // Get effects instance
                    var effectsInstance = GetEffectsInstance();
                    if (!effectsInstance)
                    {
                        Logger.LogError("[Bring Back Concussion] Effects instance not found!");
                    }
                    else
                    {
                        // Get the material field
                        int randomIndex = UnityEngine.Random.Range(0, HeadshotMaterials.Length);
                        MaterialType selectedMaterial = HeadshotMaterials[randomIndex];

                        if (!Plugin.MiscPickRandomSound.Value)
                        {
                            selectedMaterial = MaterialType.GlassVisor;
                        }

                        effectsInstance.EmitPlayerSoundOnly(
                            selectedMaterial,
                            player,
                            2.0f,
                            null
                        );
                        _lastPlayTime = DateTime.Now;

                        // Logger.LogInfo($"[Bring Back Concussion] Selected sound index: {randomIndex}, Name: {selectedSound.name}");
                    }
                }

                // Play UI sound
                if (Plugin.PlayDeathUISound.Value && !_soundPlayed)
                {
                    _soundPlayed = true;
                    _lastPlayTime = DateTime.Now;

                    // Simulate Live death UI sound (afair it plays with some kind of delay)
                    var random = new Random();
                    int delayMilliseconds = random.Next(1500, 4500);

                    await Task.Delay(delayMilliseconds);

                    if (Singleton<GUISounds>.Instantiated)
                    {
                        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.PlayerIsDead);
                    }
                    else if (Plugin.VerboseLogging.Value)
                    {
                        Logger.LogInfo("[Bring Back Concussion] GUISounds not ready, skipping death UI sound.");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"[Bring Back Concussion] OnDiedPatch error: {e.Message}");
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