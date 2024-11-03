using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace DraftableAnimals
{
    public static class DraftableAnimalsUtility
    {

        public static bool IsAnAnimal(Pawn pawn)
        {
            return pawn?.RaceProps?.Animal ?? false;
        }

        public static bool IsTrainedToAttack(this Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Message("[DraftableAnimals] Pawn is null. Remove this message if always seen at game  start.");
                return false;
            }

            // Return false immediately if it's not an animal
            if (!pawn.RaceProps.Animal)
            {
                return false;
            }

            if (DraftableAnimalsMod.settings.requireTraining)
            {
                if (pawn.training == null)
                {
                  //  Log.Warning($"[DraftableAnimals] {pawn.LabelShort} has no training tracker.");
                    return false;
                }

                // Check if the animal is trained to attack
                return pawn.training.HasLearned(TrainableDefOf.Release);
            }
            
            // If training is not required, assume all animals are draftable
            return true;
        }

/*         public static bool IsTrainedToAttack(this Pawn pawn)
         {
            if(pawn == null)
            {
                return false;
            }
             // Return false immediately if it's not an animal
             if (!pawn.RaceProps.Animal)
             {
                 return false;
             }

             // Return based on the training requirement and if the animal is trained
             return DraftableAnimalsMod.settings.requireTraining
                 ? pawn.training?.HasLearned(TrainableDefOf.Release) ?? false
                 : true; // If training is not required, assume all animals are draftable
         }
*/
        // Stub for handling additional logic (e.g., IsInDropPod, IsInCaravan, etc.)
        public static bool IsInDropPodOrCaravan(Pawn pawn)
        {
            // Implement logic for checking if the pawn is in a drop pod or caravan
            return false;
        }

        public static bool IsDryad(Pawn pawn)
        {
            // Implement logic for checking if the pawn is a dryad
            return pawn.RaceProps?.Dryad ?? false;
        }


        public static void AddAnimalArrestOptions(ref List<FloatMenuOption> options, Vector3 clickPos, Pawn pawn)
        {
            foreach (LocalTargetInfo targetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), thingsOnly: true))
            {
                Pawn targetPawn = (Pawn)targetInfo.Thing;

                if (pawn.CanReach(targetPawn, PathEndMode.OnCell, Danger.Deadly))
                {
                    options.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Arrest".Translate(targetPawn.LabelCap, targetPawn), delegate
                    {
                        Building_Bed bed = RestUtility.FindBedFor(targetPawn, pawn, checkSocialProperness: false, ignoreOtherReservations: false, GuestStatus.Prisoner);
                        if (bed != null)
                        {
                            Job job = JobMaker.MakeJob(JobDefOf.Arrest, targetPawn, bed);
                            job.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                            if (targetPawn.Faction != null && targetPawn.Faction != Faction.OfPlayer && !targetPawn.Faction.Hidden && !targetPawn.Faction.HostileTo(Faction.OfPlayer))
                            {
                                Messages.Message("MessageCapturingWillAngerFaction".Translate(targetPawn.Named("PAWN")), targetPawn, MessageTypeDefOf.CautionInput, historical: false);
                            }
                        }
                        else
                        {
                            Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), targetPawn, MessageTypeDefOf.RejectInput, historical: false);
                        }
                    }, MenuOptionPriority.High, null, targetPawn), pawn, targetPawn));
                }
            }
        }

        public static void AddAnimalRescueOptions(ref List<FloatMenuOption> options, Vector3 clickPos, Pawn pawn)
        {
            foreach (LocalTargetInfo targetInfo in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
            {
                Pawn targetPawn = (Pawn)targetInfo.Thing;

                if (!targetPawn.InBed() && pawn.CanReserveAndReach(targetPawn, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
                {
                    options.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Rescue".Translate(targetPawn.LabelCap, targetPawn), delegate
                    {
                        Building_Bed bed = RestUtility.FindBedFor(targetPawn, pawn, checkSocialProperness: false);
                        if (bed != null)
                        {
                            Job job = JobMaker.MakeJob(JobDefOf.Rescue, targetPawn, bed);
                            job.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                        }
                        else
                        {
                            Messages.Message("CannotRescue".Translate() + ": " + "NoNonPrisonerBed".Translate(), targetPawn, MessageTypeDefOf.RejectInput, historical: false);
                        }
                    }, MenuOptionPriority.RescueOrCapture, null, targetPawn), pawn, targetPawn));
                }
            }
        }

    }
}