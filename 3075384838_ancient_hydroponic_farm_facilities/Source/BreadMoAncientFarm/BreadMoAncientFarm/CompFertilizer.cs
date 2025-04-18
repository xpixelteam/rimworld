using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using PipeSystem;
using RimWorld;
using Verse;

namespace BreadMoAncientFarm
{
    public class CompFertilizer : CompResource
    {
        public CompProperties_Fertilizer Props => (CompProperties_Fertilizer)props;

        public Building_PlantGrower plantGrower => parent as Building_PlantGrower;

        private CompPowerTrader compPower;
        private int nextGrowTick;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compPower = parent.GetComp<CompPowerTrader>();
            if (plantGrower == null)
            {
                Log.Warning(parent.def.defName + " has CompFertilizer but is not a Building_PlantGrower");
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.CompInspectStringExtra());
            stringBuilder.AppendInNewLine("BM_ApproxNutrientUse".Translate() + (33000f / Props.ticksPerCycle * Props.FertilizerUsePerPlantPerCycle).ToString());
            return stringBuilder.ToString().Trim();
        }

        public override void CompTickLong()
        {
            Grow();
        }

        public override void CompTickRare()
        {
            Grow();
        }

        public override void CompTick()
        {
            Grow();
        }

        public void Grow()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick > nextGrowTick && plantGrower != null && (compPower == null || compPower.PowerOn))
            {
                foreach (Plant item in plantGrower.PlantsOnMe)
                {
                    if (item.GrowthRate > 0f && item.Growth > 0 && item.Growth < 1 && PipeNet.Stored >= Props.FertilizerUsePerPlantPerCycle)
                    {
                        float growthRate = 1f / (60000f * item.def.plant.growDays) * item.GrowthRate;
                        item.Growth += growthRate * Props.ticksPerCycle * BreadMoAFMod.settings.FertilizerEfficiency;
                        PipeNet.DrawAmongStorage(Props.FertilizerUsePerPlantPerCycle, PipeNet.storages);
                    }
                }
                nextGrowTick = Find.TickManager.TicksGame + Props.ticksPerCycle;
            }
        }
    }

    public class CompProperties_Fertilizer : CompProperties_Resource
    {
        public int ticksPerCycle = 2500;
        public float FertilizerUsePerPlantPerCycle = 0.0833333f;
        public CompProperties_Fertilizer()
        {
            compClass = typeof(CompFertilizer);
        }
    }
}
