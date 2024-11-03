using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DraftableAnimals
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    public static class FloatMenuMakerMap_ChoicesAtFor_Patch
    {
        public static void Postfix(ref List<FloatMenuOption> __result, Vector3 clickPos, Pawn pawn, bool suppressAutoTakeableGoto = false)
        {
            // Check if the selected pawn is an animal and is draftable based on training or settings
            if (!pawn.RaceProps.Humanlike && DraftableAnimalsUtility.IsTrainedToAttack(pawn))
            {
                // Log menu generation for animals if logging is enabled
                if (DraftableAnimalsMod.settings.EnableLogging)
                {
                    Log.Message($"[DraftableAnimals] Animal {pawn.LabelShort} selected. Generating menu options.");
                }

                // Iterate over potential arrest targets at the clicked position
                foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), thingsOnly: true))
                {
                    Pawn target = (Pawn)item.Thing;
                    if (DraftableAnimalsMod.settings.EnableLogging)
                    {
                        Log.Message($"[DraftableAnimals] Considering arrest for {target.LabelShort}");
                    }

                    // Check if the target can be arrested by the selected animal
                    if (pawn.CanReach(target, PathEndMode.OnCell, Danger.Deadly))
                    {
                        // Add arrest option
                        __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Arrest".Translate(target.LabelCap, target), delegate
                        {
                            // Immediately capture the variables
                            Pawn capturingPawn = pawn;
                            Pawn arrestTarget = target;

                            // Log the arrest action if logging is enabled
                            if (DraftableAnimalsMod.settings.EnableLogging)
                            {
                                Log.Message($"[DraftableAnimals] Arresting {arrestTarget.LabelShort} with {capturingPawn.LabelShort}");
                            }

                            // Set the prisoner status before bed assignment
                            if (!arrestTarget.IsPrisonerOfColony)
                            {
                                arrestTarget.guest.CapturedBy(Faction.OfPlayer, capturingPawn);
                            }

                            // Find a suitable bed for the prisoner
                            Building_Bed bed = RestUtility.FindBedFor(arrestTarget, capturingPawn, checkSocialProperness: false, ignoreOtherReservations: false, GuestStatus.Prisoner);
                            if (bed != null)
                            {
                                if (DraftableAnimalsMod.settings.EnableLogging)
                                {
                                    Log.Message($"[DraftableAnimals] Found bed {bed.LabelShort} for arresting {arrestTarget.LabelShort}");
                                }

                                // Create and assign the arrest job
                                Job job = JobMaker.MakeJob(JobDefOf.Arrest, arrestTarget, bed);
                                job.count = 1;
                                capturingPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                                // Notify the player if arresting will cause diplomatic issues
                                if (arrestTarget.Faction != null && arrestTarget.Faction != Faction.OfPlayer && !arrestTarget.Faction.Hidden && !arrestTarget.Faction.HostileTo(Faction.OfPlayer))
                                {
                                    Messages.Message("MessageCapturingWillAngerFaction".Translate(arrestTarget.Named("PAWN")), arrestTarget, MessageTypeDefOf.CautionInput, historical: false);
                                }
                            }
                            else
                            {
                                // If no suitable bed is found, notify the player
                                Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), arrestTarget, MessageTypeDefOf.RejectInput, historical: false);
                            }
                        }, MenuOptionPriority.High, null, target), pawn, target));
                    }
                }

                // Iterate over potential rescue/capture targets at the clicked position
                foreach (LocalTargetInfo item in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
                {
                    Pawn victim = (Pawn)item.Thing;
                    if (DraftableAnimalsMod.settings.EnableLogging)
                    {
                        Log.Message($"[DraftableAnimals] Considering rescue/capture for {victim.LabelShort}");
                    }

                    // Check if the victim can be captured or rescued by the selected animal
                    if (!victim.InBed() && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
                    {
                        // If the victim is hostile, use "Capture" instead of "Rescue"
                        string actionLabel = victim.HostileTo(pawn.Faction) ? "Capture".Translate(victim.LabelCap, victim) : "Rescue".Translate(victim.LabelCap, victim);
                        JobDef jobDef = victim.HostileTo(pawn.Faction) ? JobDefOf.Capture : JobDefOf.Rescue;

                        // Add capture/rescue option
                        __result.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(actionLabel, delegate
                        {
                            // Immediately capture the variables
                            Pawn capturingPawn = pawn;
                            Pawn targetVictim = victim;

                            // Log the capture/rescue action if logging is enabled
                            if (DraftableAnimalsMod.settings.EnableLogging)
                            {
                                Log.Message($"[DraftableAnimals] {actionLabel} for {targetVictim.LabelShort} by {capturingPawn.LabelShort}");
                            }

                            // Set the prisoner status before bed assignment if capturing
                            if (jobDef == JobDefOf.Capture && !targetVictim.IsPrisonerOfColony)
                            {
                                targetVictim.guest.CapturedBy(Faction.OfPlayer, capturingPawn);
                            }

                            // Find a suitable bed for the prisoner or rescue target
                            Building_Bed bed = RestUtility.FindBedFor(targetVictim, capturingPawn, checkSocialProperness: false);
                            if (bed != null)
                            {
                                if (DraftableAnimalsMod.settings.EnableLogging)
                                {
                                    Log.Message($"[DraftableAnimals] Found bed {bed.LabelShort} for {actionLabel} of {targetVictim.LabelShort}");
                                }

                                // Create and assign the capture/rescue job
                                Job job = JobMaker.MakeJob(jobDef, targetVictim, bed);
                                job.count = 1;
                                capturingPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                            }
                            else
                            {
                                // If no suitable bed is found, notify the player
                                string message = "Cannot" + actionLabel + ": " + (targetVictim.HostileTo(capturingPawn.Faction) ? "NoPrisonerBed".Translate() : "NoNonPrisonerBed".Translate());
                                Messages.Message(message, targetVictim, MessageTypeDefOf.RejectInput, historical: false);
                            }
                        }, MenuOptionPriority.RescueOrCapture, null, victim), pawn, victim));
                    }
                }
            }
        }
    }
}