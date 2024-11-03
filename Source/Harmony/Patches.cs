using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace DraftableAnimals
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("DraftableAnimals.Mod");
            Log.Message("Draftable Animals 1.5.4 saz ve ut");
            harmony.PatchAll();
        }
    }

  [HarmonyPatch(typeof(ITab_Pawn_Gear), "IsVisible", MethodType.Getter)]
  public static class IsVisible_Patch
  {
      public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
      {
          // Check if logging is enabled
          bool enableLogging = DraftableAnimalsMod.settings.EnableLogging;

          if (enableLogging)
          {
              Log.Message("[DraftableAnimals] Transpiler: Checking if Gear Tab visibility check is disabled: " 
                          + DraftableAnimalsMod.settings.disableGearTabVisibilityCheck);
          }

          // If the setting to disable the gear tab visibility check is enabled, return the original instructions
          if (DraftableAnimalsMod.settings.disableGearTabVisibilityCheck) 
          {
              if (enableLogging)
              {
                  Log.Message("[DraftableAnimals] Gear Tab visibility check is disabled. Returning original instructions.");
              }

              foreach (var instr in instructions)
              {
                  yield return instr;
              }

              yield break; // Do not apply any patching, just exit
          }

          // The normal patching process when the setting is not enabled
          var shouldSkip = AccessTools.Method(typeof(DraftableAnimalsUtility), nameof(DraftableAnimalsUtility.IsTrainedToAttack));
          var codes = instructions.ToList();
          var label = ilg.DefineLabel();

          bool patched = false;
          for (var i = 0; i < codes.Count; i++)
          {
              var instr = codes[i];
              yield return instr;

              if (!patched && codes[i].opcode == OpCodes.Stloc_0)
              {
                  patched = true;

                  if (enableLogging)
                  {
                      Log.Message("[DraftableAnimals] Patch applied after Stloc_0 opcode.");
                  }

                  codes[i + 1].labels.Add(label);
                  yield return new CodeInstruction(OpCodes.Ldloc_0);
                  yield return new CodeInstruction(OpCodes.Call, shouldSkip);
                  yield return new CodeInstruction(OpCodes.Brfalse_S, label);
                  yield return new CodeInstruction(OpCodes.Ldc_I4_0); // Return false if condition met
                  yield return new CodeInstruction(OpCodes.Ret); 
              }
          }

          if (!patched)
          {
              Log.Error("[DraftableAnimals] ITab_Pawn_Gear:IsVisible Transpiler failed to patch.");
          }
          else if (enableLogging)
          {
              Log.Message("[DraftableAnimals] Transpiler completed successfully.");
          }
      }
  }

    [HarmonyPatch(typeof(Pawn), "IsColonistPlayerControlled", MethodType.Getter)]
    public static class IsColonistPlayerControlled_Patch
    {
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (DraftableAnimalsUtility.IsTrainedToAttack(__instance))
            {
                if (__instance.Spawned && __instance.Faction == Faction.OfPlayer && __instance.MentalStateDef == null && __instance.Drafted)
                {
                    __result = true;
                }
            }
        }
    }

 [HarmonyPatch(typeof(FloatMenuMakerMap), "AddUndraftedOrders")]
     public static class AddUndraftedOrders_Patch
     {
         [HarmonyPriority(Priority.First)]
         public static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
         {
             // Check if the noUndraftedMenu flag is enabled and the pawn is undrafted
             if (DraftableAnimalsMod.settings.noUndraftedMenu && pawn.RaceProps.Animal && !pawn.Drafted)
             {
                 return false; // Skip adding undrafted orders
             }

             // If the pawn is trained to attack, skip adding undrafted orders
             if (DraftableAnimalsUtility.IsTrainedToAttack(pawn))
             {
                 return false;
             }

             return true; // Proceed with adding undrafted orders
         }
     }
     [HarmonyPatch(typeof(FloatMenuMakerMap), "CanTakeOrder")]
     public static class CanTakeOrder_Patch
     {
         public static void Postfix(Pawn pawn, ref bool __result)
         {
             // Only touch result if it's an animal
             if (pawn.RaceProps.Animal && DraftableAnimalsUtility.IsTrainedToAttack(pawn))
             {
                 __result = true;
             }
         }
     }

   [HarmonyPatch(typeof(FloatMenuMakerMap), "AddDraftedOrders")]
   public static class FloatMenuMakerMap_Patch
   {
       public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
       {
           var shouldSkip = AccessTools.Method(typeof(DraftableAnimalsUtility), nameof(DraftableAnimalsUtility.IsTrainedToAttack));
           var codes = instructions.ToList();
           FieldInfo pawnField = null;

           foreach (var nestedType in typeof(FloatMenuMakerMap).GetNestedTypes(AccessTools.all))
           {
               pawnField = AccessTools.Field(nestedType, "pawn");
               if (pawnField != null) break;
           }

           if (pawnField == null)
           {
               Log.Error("[DraftableAnimals] FloatMenuMakerMap:AddDraftedOrders - Field retrieval failed.");
               yield break;
           }

           var skillsField = AccessTools.Field(typeof(Pawn), "skills");
           var constructionDefField = AccessTools.Field(typeof(SkillDefOf), "Construction");

           if (skillsField == null || constructionDefField == null)
           {
               Log.Error("[DraftableAnimals] FloatMenuMakerMap:AddDraftedOrders - Field retrieval failed.");
               yield break;
           }

           bool patched = false;

           for (var i = 0; i < codes.Count; i++)
           {
               var instr = codes[i];

               if (!patched && codes[i].opcode == OpCodes.Ldloc_0 && codes[i + 1].LoadsField(pawnField)
                   && codes[i + 2].LoadsField(skillsField) && codes[i + 3].LoadsField(constructionDefField))
               {
                   patched = true;
                   yield return new CodeInstruction(OpCodes.Ldloc_0).MoveLabelsFrom(codes[i]);
                   yield return new CodeInstruction(OpCodes.Ldfld, pawnField);
                   yield return new CodeInstruction(OpCodes.Call, shouldSkip);
                   yield return new CodeInstruction(OpCodes.Brtrue_S, codes[i + 6].operand);
               }

               yield return instr;
           }

           if (!patched)
           {
               Log.Error("[DraftableAnimals] FloatMenuMakerMap:AddDraftedOrders Transpiler failed.");
           }
       }
   }

    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public class Pawn_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            if (__instance.Faction == Faction.OfPlayer && DraftableAnimalsUtility.IsTrainedToAttack(__instance))
            {
                Command_Toggle command_Toggle = new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Command_ColonistDraft,
                    isActive = (() => __instance.Drafted),
                    toggleAction = delegate
                    {
                        __instance.jobs.debugLog = true;
                        if (__instance.drafter is null)
                        {
                            if (__instance.RaceProps.Animal)
                            {
                                __instance.equipment = new Pawn_EquipmentTracker(__instance);
                            }
                            __instance.drafter = new Pawn_DraftController(__instance);
                            __instance.drafter.Drafted = true;
                        }
                        else
                        {
                            __instance.drafter.Drafted = !__instance.Drafted;
                        }
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Drafting, KnowledgeAmount.SpecificInteraction);
                        if (__instance.drafter.Drafted)
                        {
                            LessonAutoActivator.TeachOpportunity(ConceptDefOf.QueueOrders, OpportunityType.GoodToKnow);
                        }
                    },
                    defaultDesc = "CommandToggleDraftDesc".Translate(),
                    icon = TexCommand.Draft,
                    turnOnSound = SoundDefOf.DraftOn,
                    turnOffSound = SoundDefOf.DraftOff,
                    groupKey = 81729172,
                    defaultLabel = (__instance.Drafted ? "CommandUndraftLabel" : "CommandDraftLabel").Translate()
                };
                if (__instance.Downed)
                {
                    command_Toggle.Disable("IsIncapped".Translate(__instance.LabelShort, __instance));
                }
                if (!__instance.Drafted)
                {
                    command_Toggle.tutorTag = "Draft";
                }
                else
                {
                    command_Toggle.tutorTag = "Undraft";
                }
                yield return command_Toggle;
            }

            foreach (var g in __result)
            {
                if (!DraftableAnimalsUtility.IsTrainedToAttack(__instance) || g is not Command_Toggle command || command.defaultDesc != "CommandToggleDraftDesc".Translate())
                {
                    yield return g;
                }
            }
        }
    }
}