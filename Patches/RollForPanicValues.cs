using System;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using GPUInstancer;
using HarmonyLib;
using Systems.Effects;

namespace BringBackConcussion.Patches
{
    internal class RollForPanicValues : ModulePatch
    {

        public static int PanicRollChance = 5;

        protected override MethodBase GetTargetMethod() 
        { 
            return AccessTools.Method(typeof(GameWorld), "OnGameStarted");
        }
        
        [PatchPrefix]
        public void PatchPrefix(GameWorld gameWorld,Player __instance)
        {
            var stressResLevel = __instance.Skills.StressResistance.Level;
            PanicRollChance = RollPanicChance(stressResLevel);
        }
        
        private static int RollPanicChance(int stressResLevel)
        {
            if (stressResLevel > 50)
            {
                // We force elite stress resistance players to only have a solid 5%
                return 5;
            }

            var randomInt = UnityEngine.Random.Range(1, 30);
            var preRollValue = stressResLevel - randomInt;
            var finalRollValue = preRollValue < 0 ? randomInt : preRollValue - randomInt;
            
            return finalRollValue < 0 ? 1 : finalRollValue;
        }
    }
}