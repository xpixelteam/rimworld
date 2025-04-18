using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace AncientMining
{
    public class CompQuestScanner : CompScanner
    {
        private QuestScriptTexPathPair targetQuest;

        public new CompProperties_QuestScanner Props => props as CompProperties_QuestScanner;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref targetQuest, "targetQuest");
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            SetDefaultTargetMineral();
        }

        private void SetDefaultTargetMineral()
        {
            targetQuest = null;
        }

        protected override void DoFind(Pawn worker)
        {
            QuestScriptDef Quest;
            if (targetQuest != null && Rand.Chance(Props.getSelectedChance(worker)))
            {
                Quest = targetQuest.quest;
            }
            else
            {
                Quest = Props.quests.RandomElement().quest;
            }
            Slate slate = new Slate();
            slate.Set("map", parent.Map);
            slate.Set("targetQuest", targetQuest);
            slate.Set("worker", worker);
            if (Quest.CanRun(slate))
            {
                Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(Quest, slate);
                Find.LetterStack.ReceiveLetter(quest.name, quest.description, LetterDefOf.PositiveEvent, null, null, quest);
            }
        }

        protected override bool TickDoesFind(float scanSpeed)
        {
            if (targetQuest != null)
            {
                scanSpeed *= Props.selectedSearchSpeed;
            }
            return base.TickDoesFind(scanSpeed);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item2 in base.CompGetGizmosExtra())
            {
                yield return item2;
            }
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            Command_Action command_Action = new Command_Action();
            string s = Props.unselectedString;
            string t = Props.unselectedTexPath;
            if (targetQuest != null)
            {
                s = targetQuest.questTypeName;
                t = targetQuest.texPath;
            }

            command_Action.defaultLabel = "CommandSelectMineralToScanFor".Translate() + ": " + s;
            command_Action.defaultDesc = "CommandSelectMineralToScanForDesc".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get(t, reportFailure: false);
            command_Action.action = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();

                FloatMenuOption none = new FloatMenuOption(Props.unselectedString, delegate
                {
                    foreach (object selectedObject in Find.Selector.SelectedObjects)
                    {
                        if (selectedObject is Thing thing)
                        {
                            CompQuestScanner comp = thing.TryGetComp<CompQuestScanner>();
                            if (comp != null)
                            {
                                comp.targetQuest = null;
                            }
                        }
                    }
                }, MenuOptionPriority.Default, null, null, 29f, (Rect rect) => Icon(rect, t));
                list.Add(none);

                foreach (QuestScriptTexPathPair item3 in Props.quests)
                {
                    FloatMenuOption item = new FloatMenuOption(item3.questTypeName, delegate
                    {
                        foreach (object selectedObject in Find.Selector.SelectedObjects)
                        {
                            if (selectedObject is Thing thing)
                            {
                                CompQuestScanner comp = thing.TryGetComp<CompQuestScanner>();
                                if (comp != null)
                                {
                                    comp.targetQuest = item3;
                                }
                            }
                        }
                    }, MenuOptionPriority.Default, null, null, 29f, (Rect rect) => Icon(rect, item3.texPath));
                    list.Add(item);
                }
                Find.WindowStack.Add(new FloatMenu(list));
            };
            yield return command_Action;
        }

        public bool Icon(Rect rect, string t)
        {
            Widgets.DrawTextureFitted(rect, ContentFinder<Texture2D>.Get(t, reportFailure: false), 1);

            return false;
        }
    }

    public class CompProperties_QuestScanner : CompProperties_Scanner
    {
        public SkillDef skill;

        public SimpleCurve selectedChance = new SimpleCurve()
        {
            new CurvePoint(0,0.1f),
            new CurvePoint(10,0.5f),
            new CurvePoint(20,0.5f),
        };

        public float selectedSearchSpeed = 0.7f;

        [MustTranslate]
        public string unselectedString = "";

        [NoTranslate]
        public string unselectedTexPath = "";

        public List<QuestScriptTexPathPair> quests = new List<QuestScriptTexPathPair>();

        public CompProperties_QuestScanner()
        {
            compClass = typeof(CompQuestScanner);
        }

        public float getSelectedChance(Pawn p)
        {
            return selectedChance.Evaluate(p.skills.GetSkill(skill).Level);
        }
    }

    public class QuestScriptTexPathPair : IExposable
    {
        public QuestScriptDef quest;
        [NoTranslate]
        public string texPath;

        [MustTranslate]
        public string questTypeName;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref quest, "quest");
            Scribe_Values.Look(ref texPath, "texPath");
            Scribe_Values.Look(ref questTypeName, "questTypeName");
        }
    }
}
