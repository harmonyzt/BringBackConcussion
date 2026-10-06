using EFT;
using SPT.Reflection.Patching;

namespace BringBackConcussion.Patches
{
    internal abstract class RollForAdrenalineValues : ModulePatch
    {
        public static int CalculateAdrenalineChance(Player player)
        {
            if (player == null || player.Skills?.StressResistance == null)
                return 10;

            int stressResLevel = player.Skills.StressResistance.Level;

            return stressResLevel switch
            {
                >= 51 => UnityEngine.Random.Range(68, 98), // up to 98%
                >= 31 => UnityEngine.Random.Range(37, 67), // up to 67%
                >= 11 => UnityEngine.Random.Range(12, 36), // up to 36%
                _ => UnityEngine.Random.Range(1, 11)    // up to 11%
            };
        }

        // Roll for adrenaline
        public static bool ShouldGetAdrenaline(Player player)
        {
            if (!Plugin.EnableAdrenaline.Value)
                return false;

            int adrenalineChance = CalculateAdrenalineChance(player);
            int roll = UnityEngine.Random.Range(0, 100);

            return roll < adrenalineChance;
        }
    }
}