using HarmonyLib;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace DraftableAnimals
{
    [HarmonyPatch(typeof(JobDriver_TakeToBed), "MakeNewToils")]
    public static class JobDriver_TakeToBed_MakeNewToils_Patch
    {
        static void Prefix(JobDriver_TakeToBed __instance)
        {
            if (DraftableAnimalsMod.settings.EnableLogging)
            {
                Pawn takee = (Pawn)AccessTools.Property(typeof(JobDriver_TakeToBed), "Takee").GetValue(__instance);
                Building_Bed dropBed = (Building_Bed)AccessTools.Property(typeof(JobDriver_TakeToBed), "DropBed").GetValue(__instance);

                Log.Message($"[DraftableAnimals] Job started for {__instance.pawn.LabelShort} - Taking {takee.LabelShort} to bed {dropBed.LabelShort}");
            }
        }

        static IEnumerable<Toil> Postfix(IEnumerable<Toil> toils, JobDriver_TakeToBed __instance)
        {
            Pawn takee = (Pawn)AccessTools.Property(typeof(JobDriver_TakeToBed), "Takee").GetValue(__instance);
            Building_Bed dropBed = (Building_Bed)AccessTools.Property(typeof(JobDriver_TakeToBed), "DropBed").GetValue(__instance);

            foreach (var toil in toils)
            {
                // Check bed compatibility before each toil
                if (toil == Toils_Bed.ClaimBedIfNonMedical(TargetIndex.B, TargetIndex.A))
                {
                    if (DraftableAnimalsMod.settings.EnableLogging)
                    {
                        Log.Message($"[DraftableAnimals] {takee.LabelShort} is attempting to claim bed {dropBed.LabelShort} - Medical: {dropBed.Medical}, ForPrisoners: {dropBed.ForPrisoners}, IsPrisoner: {takee.IsPrisoner}");
                    }

                    if (!CanClaimBed(takee, dropBed))
                    {
                        Log.Error($"[DraftableAnimals] {takee.LabelShort} cannot claim the bed {dropBed.LabelShort}. Failing the job.");
                    }
                }

                yield return toil;

                if (DraftableAnimalsMod.settings.EnableLogging)
                {
                    // Log after each toil is executed
                    Log.Message($"[DraftableAnimals] Executing toil: {toil.GetType().Name} for {__instance.pawn.LabelShort} targeting {takee.LabelShort}");
                }
            }
        }

        private static bool CanClaimBed(Pawn takee, Building_Bed bed)
        {
            bool canClaim = !(bed.Medical || (bed.ForPrisoners != takee.IsPrisoner));

            if (DraftableAnimalsMod.settings.EnableLogging && !canClaim)
            {
                Log.Message($"[DraftableAnimals] {takee.LabelShort} cannot claim bed {bed.LabelShort} - Medical: {bed.Medical}, ForPrisoners: {bed.ForPrisoners}, IsPrisoner: {takee.IsPrisoner}");
            }

            return canClaim;
        }
    }
}