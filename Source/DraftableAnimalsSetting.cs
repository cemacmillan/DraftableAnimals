using UnityEngine;
using Verse;

namespace DraftableAnimals
{
    public class DraftableAnimalsSettings : ModSettings
    {
        public bool requireTraining = true;
        public bool EnableLogging = false;
        public bool noUndraftedMenu = false;
        public bool disableGearTabVisibilityCheck = true; // New setting

        // Mod detection
        public bool CatsAreCatsActive = ModsConfig.IsActive("cem.catsarecats");
        public bool LifeWithAnimals = ModsConfig.IsActive("CnjFdhqn.lifewithanimals");

        public override void ExposeData()
        {
            Scribe_Values.Look(ref requireTraining, "requireTraining", true);
            Scribe_Values.Look(ref EnableLogging, "enableLogging", false);
            Scribe_Values.Look(ref noUndraftedMenu, "noUndraftedMenu", false);
            Scribe_Values.Look(ref disableGearTabVisibilityCheck, "disableGearTabVisibilityCheck", true); // Expose new setting
            base.ExposeData();
        }
    }
   

    public class DraftableAnimalsMod : Mod
    {
        public static DraftableAnimalsSettings settings;

        // Corrected the constructor name to match the class name
        public DraftableAnimalsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<DraftableAnimalsSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Existing setting
            listingStandard.CheckboxLabeled("Require Animal Training", ref settings.requireTraining, "Should animals require training to be draftable?");

            // New setting for enabling logging
            listingStandard.CheckboxLabeled("Enable Debug Logging", ref settings.EnableLogging, "Enable or disable debug logging for Draftable Animals.");

            // New setting for undrafted menu
            listingStandard.CheckboxLabeled("Disable Undrafted Menu", ref settings.noUndraftedMenu, "Disable undrafted menu options for draftable animals.");

            // New setting for disabling the Gear tab visibility check
            listingStandard.CheckboxLabeled("Disable Gear Tab Visibility Check", ref settings.disableGearTabVisibilityCheck, "Disable the visibility check for the Gear tab on animals.");

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "Draftable Animals";
    }
}