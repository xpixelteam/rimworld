
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BreadMoAncientFarm
{
    public class BreadMoAFMod : Mod
    {
        public static Setting settings;

        public static float FertilizerEfficiency => settings.FertilizerEfficiency;

        public BreadMoAFMod(ModContentPack content)
            : base(content)
        {
            settings = GetSettings<Setting>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.ColumnWidth = (inRect.width - 17f) / 2f;
            listing_Standard.Begin(inRect);
            Text.Font = GameFont.Small;
            listing_Standard.GapLine();
            listing_Standard.Label("BM_FertilizerEfficiency".Translate() + (1 + settings.FertilizerEfficiency).ToStringPercent());
            settings.FertilizerEfficiency = listing_Standard.Slider(settings.FertilizerEfficiency, 0, 5);
            listing_Standard.End();
        }

        public override string SettingsCategory()
        {
            return "BMAF_Setting".Translate();
        }
    }
    public class Setting : ModSettings
    {
        public float FertilizerEfficiency = 0.5f;



        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref FertilizerEfficiency, "FertilizerEfficiency");
        }
    }
}
