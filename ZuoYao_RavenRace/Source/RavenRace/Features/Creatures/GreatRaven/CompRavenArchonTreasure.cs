using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Sounds; // 新增：引用音效DefOf
using Verse.Sound; // 【修复】添加音效命名空间

namespace RavenRace.Features.Creatures.GreatRaven
{
    public class CompProperties_RavenArchonTreasure : CompProperties
    {
        public CompProperties_RavenArchonTreasure()
        {
            this.compClass = typeof(CompRavenArchonTreasure);
        }
    }

    public class CompRavenArchonTreasure : ThingComp
    {
        private int lastTreasureTick = -1;

        // [修复] 移除静态初始化，改为缓存字段，并在 GetGizmos 中按需加载
        private static Texture2D iconAbsorbInt;
        private static Texture2D IconAbsorb
        {
            get
            {
                if (iconAbsorbInt == null)
                {
                    iconAbsorbInt = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/Raven_Ritual_ArchonTreasure", true);
                }
                return iconAbsorbInt;
            }
        }

        public CompProperties_RavenArchonTreasure Props => (CompProperties_RavenArchonTreasure)props;
        public Pawn Pawn => (Pawn)parent;

        // 获取当前的间隔 Tick 数
        private int IntervalTicks => Mathf.RoundToInt(RavenRaceMod.Settings.greatRavenSearchDays * 60000f);

        // 黄金精神 100% 对应的价值上限 (50万)
        private const float MaxGoldenValue = 500000f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastTreasureTick, "lastTreasureTick", -1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                lastTreasureTick = Find.TickManager.TicksGame;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!Pawn.Spawned || Pawn.Dead) return;
            if (!RavenRaceMod.Settings.enableGreatRavenShiny) return;

            if (lastTreasureTick < 0) lastTreasureTick = Find.TickManager.TicksGame;

            if (Find.TickManager.TicksGame - lastTreasureTick >= IntervalTicks)
            {
                TryFindShinyThing();
                lastTreasureTick = Find.TickManager.TicksGame;
            }
        }

        // --- 寻找亮闪闪逻辑 ---
        public void TryFindShinyThing()
        {
            if (Pawn.Faction != Faction.OfPlayer) return;

            var s = RavenRaceMod.Settings;
            Thing thingToDrop = null;

            // 独立的概率判定
            if (Rand.Chance(s.greatRavenCubeChance))
            {
                thingToDrop = ThingMaker.MakeThing(RavenDefOf.Raven_Item_HojoCube);
                thingToDrop.stackCount = 1;
            }
            else if (Rand.Chance(s.greatRavenItemChance))
            {
                ThingDef goldDef = ThingDefOf.Gold;
                var validDefs = DefDatabase<ThingDef>.AllDefs.Where(d =>
                    d.MadeFromStuff &&
                    (d.IsWeapon || d.IsApparel || d.IsArt) &&
                    GenStuff.AllowedStuffsFor(d).Contains(goldDef)
                ).ToList();

                if (validDefs.Count > 0)
                {
                    ThingDef chosenDef = validDefs.RandomElement();
                    thingToDrop = ThingMaker.MakeThing(chosenDef, goldDef);
                    if (thingToDrop.TryGetComp<CompQuality>() is CompQuality q)
                    {
                        q.SetQuality(QualityUtility.GenerateQualityRandomEqualChance(), ArtGenerationContext.Colony);
                    }
                }
            }
            else if (Rand.Chance(s.greatRavenGoldChance))
            {
                thingToDrop = ThingMaker.MakeThing(ThingDefOf.Gold);
                thingToDrop.stackCount = 20;
            }

            if (thingToDrop != null)
            {
                if (GenPlace.TryPlaceThing(thingToDrop, Pawn.Position, Pawn.Map, ThingPlaceMode.Near))
                {
                    // 【音效修改】播放找到宝物的音效
                    RavenSoundDefOf.RavenMeme_ArchonTreasure?.PlayOneShot(SoundInfo.InMap(new TargetInfo(Pawn)));

                    Find.LetterStack.ReceiveLetter(
                        "Raven_LetterLabel_ShinyFound".Translate(),
                        "Raven_LetterText_ShinyFound".Translate(Pawn.LabelShort, thingToDrop.LabelCap),
                        LetterDefOf.PositiveEvent,
                        new LookTargets(Pawn, thingToDrop)
                    );
                    FleckMaker.ThrowMetaIcon(Pawn.Position, Pawn.Map, FleckDefOf.Heart);
                }
            }
            else
            {
                // 没找到东西时的信封通知
                Find.LetterStack.ReceiveLetter(
                    "Raven_LetterLabel_ShinyNothing".Translate(),
                    "Raven_LetterText_ShinyNothing".Translate(Pawn.LabelShort),
                    LetterDefOf.NeutralEvent,
                    new LookTargets(Pawn)
                );
            }
        }

        // --- 黄金精神技能逻辑 (手动瞄准版) ---
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            if (Pawn.Faction == Faction.OfPlayer)
            {
                Hediff goldenSpirit = Pawn.health.hediffSet.GetFirstHediffOfDef(RavenDefOf.Raven_Hediff_GoldenSpirit);
                float severity = goldenSpirit?.Severity ?? 0f;
                bool isFull = severity >= 1.0f;

                Command_Action absorbCmd = new Command_Action
                {
                    defaultLabel = "吸收亮闪闪",
                    defaultDesc = "命令大统领吞噬指定的黄金或金制物品，将其价值转化为“黄金精神”。\n\n当前进度: " + severity.ToStringPercent("F4"),
                    // [修复] 此处访问静态属性，会在主线程按需加载贴图，解决多线程红字
                    icon = IconAbsorb,
                    action = () =>
                    {
                        Find.Targeter.BeginTargeting(GetTargetingParameters(), (LocalTargetInfo target) =>
                        {
                            AbsorbTarget(target.Thing);
                        }, Pawn, null, IconAbsorb);
                    }
                };

                if (isFull)
                {
                    absorbCmd.Disable("黄金精神已达满盈状态");
                }

                yield return absorbCmd;
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Force Shiny Found",
                    icon = TexCommand.DesirePower,
                    action = () =>
                    {
                        TryFindShinyThing();
                        lastTreasureTick = Find.TickManager.TicksGame;
                    }
                };
            }
        }

        // 定义可被选中的目标参数
        private TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = false,
                canTargetBuildings = false,
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = (TargetInfo t) =>
                {
                    if (t.Thing == null || !t.Thing.Spawned) return false;
                    return IsValidGoldItem(t.Thing);
                }
            };
        }

        // 验证物品是否为黄金或金制品
        private bool IsValidGoldItem(Thing t)
        {
            if (t.def.category != ThingCategory.Item) return false;

            // 1. 是黄金本身
            if (t.def == ThingDefOf.Gold) return true;

            // 2. 是由黄金制成的 (Stuff)
            if (t.Stuff == ThingDefOf.Gold) return true;

            // 3. 是齁金魔方
            if (t.def == RavenDefOf.Raven_Item_HojoCube) return true;

            return false;
        }

        // 执行吸收逻辑
        private void AbsorbTarget(Thing t)
        {
            if (t == null || t.Destroyed || !IsValidGoldItem(t))
            {
                Messages.Message("目标无效：必须是黄金或金制品。", Pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            float value = t.MarketValue * t.stackCount;
            int count = t.stackCount;
            string label = t.Label;

            if (value <= 0) return;

            float severityGain = value / MaxGoldenValue;

            HealthUtility.AdjustSeverity(Pawn, RavenDefOf.Raven_Hediff_GoldenSpirit, severityGain);

            FleckMaker.ThrowLightningGlow(Pawn.TrueCenter(), Pawn.Map, 3f);
            FleckMaker.ThrowMicroSparks(Pawn.DrawPos, Pawn.Map);
            MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, "齁噢噢噢❤", Color.yellow);

            t.Destroy(DestroyMode.Vanish);

            Messages.Message($"{Pawn.LabelShort} 吞噬了 {label} (价值 {value:F0})，黄金精神大幅增强！", Pawn, MessageTypeDefOf.PositiveEvent);
        }

        public override string CompInspectStringExtra()
        {
            if (!RavenRaceMod.Settings.enableGreatRavenShiny || Pawn.Faction != Faction.OfPlayer) return null;

            int ticksElapsed = Find.TickManager.TicksGame - lastTreasureTick;
            int ticksRemaining = IntervalTicks - ticksElapsed;
            string status = ticksRemaining > 0 ? ticksRemaining.ToStringTicksToPeriod() : "Ready".Translate();

            return "Raven_Inspect_ShinySearch".Translate() + ": " + status;
        }
    }
}