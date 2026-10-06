using EFT;
using SPT.Reflection.Patching;

namespace BringBackConcussion.Patches
{
    internal abstract class RollForPanicValues : ModulePatch
    {
        // Calculate panic chance
        public static int CalculatePanicChance(Player player)
        {
            if (player == null || player.Skills?.StressResistance == null)
                return 25;

            int stressResLevel = player.Skills.StressResistance.Level;

            return stressResLevel switch
            {
                >= 51 => UnityEngine.Random.Range(1, 3),
                >= 31 => UnityEngine.Random.Range(5, 21),
                >= 11 => UnityEngine.Random.Range(20, 51),
                _ => UnityEngine.Random.Range(20, 81)
            };
        }
        
        // Roll for panic
        public static bool ShouldPanic(Player player)
        {
            if (!Plugin.EnablePanic.Value)
                return false;

            int panicChance = CalculatePanicChance(player);
            int roll = UnityEngine.Random.Range(0, 100);

            return roll < panicChance;
        }
    }
}