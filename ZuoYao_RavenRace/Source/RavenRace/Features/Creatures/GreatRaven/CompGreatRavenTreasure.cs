using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.Creatures.GreatRaven
{
    public class CompProperties_GreatRavenTreasure : CompProperties
    {
        public CompProperties_GreatRavenTreasure()
        {
            this.compClass = typeof(CompGreatRavenTreasure);
        }
    }

    public class CompGreatRavenTreasure : ThingComp
    {
        // 记录上一次产出的时间，而不是下一次
        // 这样修改间隔设置时，倒计时会立即根据新的间隔重新计算
        private int lastTreasureTick = -1;

        public CompProperties_GreatRavenTreasure Props => (CompProperties_GreatRavenTreasure)props;
        public Pawn Pawn => (Pawn)parent;

        // 获取当前的间隔 Tick 数 (基于设置)
        private int IntervalTicks => Mathf.RoundToInt(RavenRaceMod.Settings.greatRavenSearchDays * 60000f);

        public override void PostExposeData()
        {
            base.PostExposeData();
            // 兼容旧存档：如果存的是 nextTreasureTick，读取后会被重置逻辑修正
            Scribe_Values.Look(ref lastTreasureTick, "lastTreasureTick", -1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                //以此刻作为起始点
                lastTreasureTick = Find.TickManager.TicksGame;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!Pawn.Spawned || Pawn.Dead) return;
            if (!RavenRaceMod.Settings.enableGreatRavenShiny) return;

            // 初始化检查
            if (lastTreasureTick < 0) lastTreasureTick = Find.TickManager.TicksGame;

            // 检查是否达到时间
            if (Find.TickManager.TicksGame - lastTreasureTick >= IntervalTicks)
            {
                TryFindShinyThing();
                // 重置为当前时间
                lastTreasureTick = Find.TickManager.TicksGame;
            }
        }

        public void TryFindShinyThing()
        {
            // 只有在已驯服且属于玩家派系时才触发
            if (Pawn.Faction != Faction.OfPlayer) return;

            ThingDef goldDef = ThingDefOf.Gold;
            Thing thingToDrop;

            // 30% 概率给纯金，70% 概率给金制品
            if (Rand.Chance(0.3f))
            {
                thingToDrop = ThingMaker.MakeThing(goldDef);
                thingToDrop.stackCount = Rand.Range(20, 50);
            }
            else
            {
                var validDefs = DefDatabase<ThingDef>.AllDefs.Where(d =>
                    d.MadeFromStuff &&
                    (d.IsWeapon || d.IsApparel || d.IsArt) &&
                    GenStuff.AllowedStuffsFor(d).Contains(goldDef)
                ).ToList();

                if (validDefs.Count > 0)
                {
                    ThingDef chosenDef = validDefs.RandomElement();
                    thingToDrop = ThingMaker.MakeThing(chosenDef, goldDef);

                    CompQuality qualityComp = thingToDrop.TryGetComp<CompQuality>();
                    if (qualityComp != null)
                    {
                        qualityComp.SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), ArtGenerationContext.Colony);
                    }
                }
                else
                {
                    thingToDrop = ThingMaker.MakeThing(goldDef);
                    thingToDrop.stackCount = Rand.Range(10, 30);
                }
            }

            if (GenPlace.TryPlaceThing(thingToDrop, Pawn.Position, Pawn.Map, ThingPlaceMode.Near))
            {
                Find.LetterStack.ReceiveLetter(
                    "Raven_LetterLabel_ShinyFound".Translate(),
                    "Raven_LetterText_ShinyFound".Translate(Pawn.LabelShort, thingToDrop.LabelCap),
                    LetterDefOf.PositiveEvent,
                    new LookTargets(Pawn, thingToDrop)
                );
                FleckMaker.ThrowMetaIcon(Pawn.Position, Pawn.Map, FleckDefOf.Heart);
            }
        }

        // [新增] 开发者模式按钮
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // 遍历基类的 Gizmo (如果有)
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            // 如果开启了开发者模式，显示强制完成按钮
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Force Shiny Found",
                    icon = TexCommand.DesirePower, // 使用原版通用的"DesirePower"图标作为Debug图标
                    action = () =>
                    {
                        TryFindShinyThing();
                        lastTreasureTick = Find.TickManager.TicksGame; // 重置冷却
                        Messages.Message("Dev: Forced shiny find.", MessageTypeDefOf.TaskCompletion);
                    }
                };
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!RavenRaceMod.Settings.enableGreatRavenShiny || Pawn.Faction != Faction.OfPlayer)
            {
                return null;
            }

            // 计算剩余 Tick
            int ticksElapsed = Find.TickManager.TicksGame - lastTreasureTick;
            int ticksRemaining = IntervalTicks - ticksElapsed;

            if (ticksRemaining > 0)
            {
                return "Raven_Inspect_ShinySearch".Translate() + ": " + ticksRemaining.ToStringTicksToPeriod();
            }
            else
            {
                return "Raven_Inspect_ShinySearch".Translate() + ": " + "Ready".Translate();
            }
        }
    }
}