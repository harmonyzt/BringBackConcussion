using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using BringBackConcussion.Patches;

namespace BringBackConcussion
{
    [BepInPlugin("com.harmonyzt.BringBackConcussion", "BringBackConcussion", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LOGSource;
        internal static Plugin Instance;
        // Config
        internal static ConfigEntry<float> ConcussionStrength;
        internal static ConfigEntry<int> ConcussionDuration;
        internal static ConfigEntry<bool> TinnitusEffect;
        internal static ConfigEntry<bool> EnableHSSound;
        internal static ConfigEntry<bool> PlayDeathUISound;
        internal static ConfigEntry<bool> IgnoreTinnitusEquipmentChecks;
        internal static ConfigEntry<bool> EnablePanic;
        // Explosion proximity
        internal static ConfigEntry<bool> ExplosionConcussion;
        internal static ConfigEntry<float> ExplosionEffectRadius;
        internal static ConfigEntry<bool> ExplosionSoundCutoff;
        internal static ConfigEntry<float> ExplosionSoundFadeTime;
        // Adrenaline
        internal static ConfigEntry<bool> EnableAdrenaline;
        internal static ConfigEntry<int> AdrenalineDuration;
        // Misc
        internal static ConfigEntry<bool> MiscPickRandomSound;
        internal static ConfigEntry<bool> MiscGrenadeStun;
        // Frag grenades and headshots
        internal static ConfigEntry<bool> MiscGrenadeBlind;
        internal static ConfigEntry<bool> MiscMitigateGrenadeFlashTinnitus;
        internal static ConfigEntry<float> MiscBlindnessStrengthEffect;
        internal static ConfigEntry<bool> MiscHeadshotBlind;
        internal static ConfigEntry<float> MiscHeadshotStrengthEffect;
        
        private void Awake()
        {
            Instance = this;

            // Configuration - Main
            ConcussionStrength = Config.Bind(
                "General", "Concussion Strength", 0.75f, new ConfigDescription("Determines the strength of concussion effect", new AcceptableValueRange<float>(0.3f, 1.0f))
            );
            ConcussionDuration = Config.Bind(
                "General", "Concussion Duration", 5, new ConfigDescription("Determines how long the concussion lasts in seconds", new AcceptableValueRange<int>(1, 120))
            );
            TinnitusEffect = Config.Bind(
                "General", "Tinnitus Effect", false, new ConfigDescription("Enable/Disable tinnitus effect (tinnitus only occurs if no headset is equipped). To suppress tinnitus while flashed, keep Always Mitigate Tinnitus Effect checked")
            );
            EnablePanic = Config.Bind(
                "General", "Panic", true, new ConfigDescription("Enable/Disable the chance of your character to panic under certain scenarios")
            );
            IgnoreTinnitusEquipmentChecks = Config.Bind(
                "Misc", "Ignore Tinnitus Equipment Checks", false, new ConfigDescription("If enabled, tinnitus will play even if you have headset equipped. Overrides Tinnitus Effect setting when enabled.")
            );
            ExplosionConcussion = Config.Bind(
                "General", "Concussion From Nearby Explosions", true, new ConfigDescription("Apply concussion, tinnitus and panic mechanics when an explosion happens near you, even if no fragments hit you. Radius is limited by Explosion Effect Radius")
            );
            ExplosionEffectRadius = Config.Bind(
                "General", "Explosion Effect Radius", 7f, new ConfigDescription("How close (in meters) an explosion must be to trigger the mod's concussion/tinnitus/sound cutoff effects. Overrides the grenade's own default explosion radius (not the damage itself)", new AcceptableValueRange<float>(1f, 50f))
            );
            EnableAdrenaline = Config.Bind(
                "General", "Adrenaline Effect", true, new ConfigDescription("Enable/Disable chance-based adrenaline (painkiller) effect upon receiving a head hit. Chance scales with Stress Resistance skill level")
            );
            AdrenalineDuration = Config.Bind(
                "General", "Adrenaline Duration", 10, new ConfigDescription("Determines how long the adrenaline (painkiller) effect lasts in seconds", new AcceptableValueRange<int>(5, 60))
            );
            
            // Audio
            EnableHSSound = Config.Bind(
                "Audio", "Enable Death Headshot Sound", true, new ConfigDescription("Enable/Disable visor/helmet hit sound effect upon death from headshot")
            );
            PlayDeathUISound = Config.Bind(
                "Audio", "Enable Death UI Sound", true, new ConfigDescription("Enable/Disable death UI sound")
            );
            MiscPickRandomSound = Config.Bind(
                "Audio", "Use More Random Helmet Hit Sounds", true, new ConfigDescription("If disabled, will not use random range for sounds to pick and just use one sound")
            );
            ExplosionSoundCutoff = Config.Bind(
                "Audio", "Cut Off Sound On Explosion", true, new ConfigDescription("Cut off world sound when an explosion happens nearby. The closer the explosion, the deeper the cut; sound then fades back in. A concurrent tinnitus ring is preserved")
            );
            ExplosionSoundFadeTime = Config.Bind(
                "Audio", "Explosion Sound Fade In Time", 6f, new ConfigDescription("Determines how many seconds the sound takes to fade back in after an explosion cut off", new AcceptableValueRange<float>(1f, 15f))
            );
            // Misc
            MiscGrenadeStun = Config.Bind(
                "Misc", "Frag Grenades Always Concuss", true, new ConfigDescription("Enable/Disable concussion by frag grenades. If disabled, will only use default BSG's logic for concussion from frag grenades")
            );
            MiscGrenadeBlind = Config.Bind(
                "Misc", "Frag Grenades Blinds You", false, new ConfigDescription("Enable/Disable blindness by frag grenades")
            );
            MiscBlindnessStrengthEffect = Config.Bind(
                "Misc", "Frag Grenade Blindness Strength", 0.75f, new ConfigDescription("Enable/Disable strength of the blindness (very sensitive!)", new AcceptableValueRange<float>(0.1f, 2.0f))
            );
            MiscHeadshotBlind = Config.Bind(
                "Misc", "Headshots Blinds You", false, new ConfigDescription("Enable/Disable blindness by headshots")
            );
            MiscHeadshotStrengthEffect = Config.Bind(
                "Misc", "Headshot Blindness Strength", 0.85f, new ConfigDescription("Enable/Disable strength of the blindness upon receiving headshot (very sensitive!)", new AcceptableValueRange<float>(0.1f, 1.5f))
            );
            MiscMitigateGrenadeFlashTinnitus = Config.Bind(
                "Misc", "Always Mitigate Tinnitus Effect", true, new ConfigDescription("If enabled, tinnitus will not play while (or at all times) you are flashed and concussed at the same time")
            );
            
            // save the Logger to variable so we can use it elsewhere in the project
            LOGSource = Logger;
            
            // Enable patches
            new ConcussionPatch().Enable();
            new OnDiedPatch().Enable();
            new OnTinnitusPatch().Enable();
            new ExplosionProximityPatch().Enable();
            new VanillaExplosionConcussionPatch().Enable();

            Logger.LogInfo("Bring Back Concussion is loaded!");
        }
    }
}
