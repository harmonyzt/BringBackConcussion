using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BringBackConcussion.Patches
{
    /// <summary>
    /// Proximity check for explosions
    /// Fires for every grenade/mine/artillery explosion (ExplosionSharedMethods.Explosion)
    /// Runs a custom logic for concussion, and panic, etc
    /// </summary>
    internal class ExplosionProximityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExplosionSharedMethods), nameof(ExplosionSharedMethods.Explosion));
        }

        [PatchPostfix]
        public static void PatchPostfix(IExplosiveItem grenadeItem, Vector3 explosionPosition)
        {
            try
            {
                if (grenadeItem == null)
                    return;

                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                Player player = gameWorld != null ? gameWorld.MainPlayer : null;

                // Not us - do nothing
                if (player == null || !player.IsYourPlayer || player.IsAI)
                    return;

                ActiveHealthController activeHealthController = player.ActiveHealthController;
                if (activeHealthController == null || !activeHealthController.IsAlive)
                    return;

                // Our configured radius of affection
                float radius = Plugin.ExplosionEffectRadius.Value;

                float distance = Vector3.Distance(player.Position, explosionPosition);
                if (distance > radius)
                    return;

                // 0 = at the edge of the radius
                // 1 = at the epicenter (gg)
                float closeness = Mathf.Clamp01(1f - distance / radius);

                // Cut off sound based on proximity
                if (Plugin.ExplosionSoundCutoff.Value)
                {
                    ExplosionSoundCutoff.Trigger(closeness);
                }

                // Apply concussion mechanics (See Down Below)
                if (Plugin.ExplosionConcussion.Value)
                {
                    ApplyConcussionMechanics(player, activeHealthController);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"[Bring Back Concussion] ExplosionProximityPatch error: {e}");
            }
        }

        private static void ApplyConcussionMechanics(Player player, ActiveHealthController activeHealthController)
        {
            // Applies the concussion effect - tinnitus is started from it
            // Because whatever the fuck BSG is doing with this, and I am tired it just makes 0 sense why'd you start tinnitus just from DoContusion
            activeHealthController.DoContusion(Plugin.ConcussionDuration.Value, Plugin.ConcussionStrength.Value);

            // Play the tinnitus ring directly because fuck ugly BSG code
            TryStartTinnitus(player, Plugin.ConcussionDuration.Value);

            // Flash blindness from the explosion
            if (Plugin.MiscGrenadeBlind.Value)
            {
                activeHealthController.DoStun(1.0f, Plugin.MiscBlindnessStrengthEffect.Value);
            }
            else if (Plugin.TinnitusEffect.Value)
            {
                activeHealthController.DoStun(1, 0);
            }

            // Roll for panic based on player's stress resistance
            if (RollForPanicValues.ShouldPanic(player))
            {
                activeHealthController.AddEffect<ActiveHealthController.PanicEffect>(EBodyPart.Head, delayTime: 0f, workTime: null);
                activeHealthController.AddEffect<ActiveHealthController.MisfireEffect>(EBodyPart.Head, delayTime: 0f, workTime: null);
                player.StartCoroutine(ConcussionPatch.RemovePanicEffectsAfterDelay(activeHealthController));
            }
        }

        private static FieldInfo _tinnitusClipField;

        // Start the tinnitus ring directly through BetterAudio
        private static void TryStartTinnitus(Player player, float time)
        {
            try
            {
                if (!Plugin.TinnitusEffect.Value)
                    return;

                // Mitigate tinnitus only while actually flashed and concussed alltogether
                if (Plugin.MiscMitigateGrenadeFlashTinnitus.Value && OnTinnitusPatch.IsFlashed(player))
                    return;

                // Respect the headset here
                if (!Plugin.IgnoreTinnitusEquipmentChecks.Value
                    && player.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem is Headphones)
                    return;

                if (!Singleton<BetterAudio>.Instantiated)
                    return;

                if (_tinnitusClipField == null)
                    _tinnitusClipField = AccessTools.Field(typeof(Player), "_tinnitus");

                AudioClip clip = _tinnitusClipField?.GetValue(player) as AudioClip;
                Singleton<BetterAudio>.Instance.StartTinnitusEffect(time, clip);
            }
            catch (Exception e)
            {
                Logger.LogError($"[Bring Back Concussion] TryStartTinnitus error: {e}");
            }
        }
    }
}