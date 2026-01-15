using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using BringBackConcussion.Patches;

namespace BringBackConcussion
{
    [BepInPlugin("com.harmonyzt.BringBackConcussion", "BringBackConcussion", "1.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        // Config
        internal static ConfigEntry<float> ConcussionStrength;
        internal static ConfigEntry<int> ConcussionDuration;
        internal static ConfigEntry<bool> TinnitusEffect;
        internal static ConfigEntry<bool> EnableHSSound;
        internal static ConfigEntry<bool> PlayDeathUISound;
        // Misc
        internal static ConfigEntry<bool> MiscPickRandomSound;
        internal static ConfigEntry<bool> MiscGrenadeStun;
        // Frag grenades and headshots
        internal static ConfigEntry<bool> MiscGrenadeBlind;
        internal static ConfigEntry<bool> MiscMitigateGrenadeFlashTinnitus;
        internal static ConfigEntry<float> MiscBlindnessStrengthEffect;
        internal static ConfigEntry<bool> MiscHeadshotBlind;
        internal static ConfigEntry<float> MiscHeadshotStrengthEffect;
        
        // BaseUnityPlugin inherits MonoBehaviour, so you can use base unity functions like Awake() and Update()
        private void Awake()
        {
            // Configuration - Main
            ConcussionStrength = Config.Bind(
                "General", "Concussion Strength", 0.75f, new ConfigDescription("Determines the strength of concussion effect", new AcceptableValueRange<float>(0.3f, 1.0f))
            );
            ConcussionDuration = Config.Bind(
                "General", "Concussion Duration", 5, new ConfigDescription("Determines how long the concussion lasts in seconds", new AcceptableValueRange<int>(1, 120))
            );
            TinnitusEffect = Config.Bind(
                "General", "Tinnitus Effect", false, new ConfigDescription("Enable/Disable tinnitus effect (tinnitus only occurs if no headset is equipped). To completely disable tinnitus, make sure you have Always Mitigate Tinnitus Effect checked")
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
            // Misc
            MiscGrenadeStun = Config.Bind(
                "Misc", "Frag Grenades Always Concuss", true, new ConfigDescription("Enable/Disable concussion by frag grenades. If disabled, will use default BSG logic for concussion from frag grenades")
            );
            MiscGrenadeBlind = Config.Bind(
                "Misc", "Frag Grenades Blinds You", false, new ConfigDescription("Enable/Disable blindness by frag grenades")
            );
            MiscBlindnessStrengthEffect = Config.Bind(
                "Misc", "Frag Grenade Blindness Strength", 0.75f, new ConfigDescription("Enable/Disable strength of the blindness", new AcceptableValueRange<float>(0.1f, 2.0f))
            );
            MiscHeadshotBlind = Config.Bind(
                "Misc", "Headshots Blinds You", false, new ConfigDescription("Enable/Disable blindness by headshots")
            );
            MiscHeadshotStrengthEffect = Config.Bind(
                "Misc", "Headshot Blindness Strength", 0.45f, new ConfigDescription("Enable/Disable strength of the blindness upon receiving headshot (very sensitive!)", new AcceptableValueRange<float>(0.1f, 1.5f))
            );
            MiscMitigateGrenadeFlashTinnitus = Config.Bind(
                "Misc", "Always Mitigate Tinnitus Effect", true, new ConfigDescription("Enable/Disable mitigation of tinnitus effect playing at all costs if you get flashed and contused at the same time")
            );
            
            // save the Logger to variable so we can use it elsewhere in the project
            LogSource = Logger;
            
            // Enable patches
            new ConcussionPatch().Enable();
            new OnDiedPatch().Enable();
            new OnTinnitusPatch().Enable();
            
            Logger.LogInfo("Bring Back Concussion is loaded!");
        }
    }
}
