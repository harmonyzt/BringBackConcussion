using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using BringBackConcussion.Patches;

namespace BringBackConcussion
{
    [BepInPlugin("com.harmonyzt.BringBackConcussion", "BringBackConcussion", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        // Config
        internal static ConfigEntry<float> ConcussionStrength;
        internal static ConfigEntry<int> ConcussionDuration;
        internal static ConfigEntry<bool> TinnitusEffect;
        internal static ConfigEntry<bool> EnableHSSound;
        internal static ConfigEntry<bool> PlayDeathUISound;
        
        // BaseUnityPlugin inherits MonoBehaviour, so you can use base unity functions like Awake() and Update()
        private void Awake()
        {
            // Configuration
            ConcussionStrength = Config.Bind(
                "General", "Concussion Strength", 0.75f, new ConfigDescription("Determines the strength of concussion effect", new AcceptableValueRange<float>(0.5f, 1.0f))
                );
            ConcussionDuration = Config.Bind(
                "General", "Concussion Duration", 5, new ConfigDescription("Determines how long the concussion lasts in seconds", new AcceptableValueRange<int>(0, 120))
                );
            TinnitusEffect = Config.Bind(
                "General", "Tinnitus effect", false, new ConfigDescription("Enable/Disable tinnitus effect (tinnitus only occurs if no headset is equipped)")
                );
            EnableHSSound = Config.Bind(
                "Audio", "Enable Death Headshot Sound", true, new ConfigDescription("Enable/Disable visor/helmet hit sound effect upon death from headshot")
                );
            PlayDeathUISound = Config.Bind(
                "Audio", "Enable Death UI Sound", true, new ConfigDescription("Enable/Disable death UI sound")
            );
            
            // save the Logger to variable so we can use it elsewhere in the project
            LogSource = Logger;
            
            // Enable patches
            new ConcussionPatch().Enable();
            new OnDiedPatch().Enable();
            
            Logger.LogInfo("Bring Back Concussion is loaded!");
        }
    }
}
