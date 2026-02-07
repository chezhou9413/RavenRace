using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Sound;

namespace RavenRace.Features.UniqueWeapons.SpiritBeads
{
    public class CompProperties_SpiritBeads : CompProperties
    {
        public HediffDef insertedHediff; // 插入后的纳刀 Buff
        public int insertTicks = 120;    // 塞入所需时间

        public CompProperties_SpiritBeads()
        {
            this.compClass = typeof(CompSpiritBeads);
        }
    }

    // [新增] 必须添加此特性
    [StaticConstructorOnStartup]
    public class CompSpiritBeads : ThingComp
    {
        public CompProperties_SpiritBeads Props => (CompProperties_SpiritBeads)props;

        private bool isInserted = false;
        private bool autoInsert = true;

        // 缓存贴图，避免每帧加载
        private static Texture2D iconBeads;

        public bool IsInserted => isInserted;

        public Pawn Holder
        {
            get
            {
                if (parent.ParentHolder is Pawn_EquipmentTracker tracker) return tracker.pawn;
                if (parent.ParentHolder is Pawn pawn) return pawn;
                return null;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isInserted, "isInserted", false);
            Scribe_Values.Look(ref autoInsert, "autoInsert", true);
        }

        public override void CompTick()
        {
            base.CompTick();

            // 每 30 ticks (0.5秒) 检查一次，保证反应灵敏又不卡顿
            if (!parent.IsHashIntervalTick(30)) return;

            Pawn pawn = Holder;
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            // 自动装填逻辑
            // 条件：开启自动 + 未插入 + 是主武器
            if (autoInsert && !isInserted && parent == pawn.equipment?.Primary)
            {
                if (ShouldAutoInsert(pawn))
                {
                    StartInsertJob(pawn);
                }
            }
        }

        /// <summary>
        /// 判断当前是否适合进行自动装填
        /// 核心逻辑：像霰弹枪一样，只要闲下来就塞，但绝不打断走位和攻击
        /// </summary>
        private bool ShouldAutoInsert(Pawn p)
        {
            // 1. 失去意识或精神崩溃 -> 不塞
            if (p.Downed || p.InMentalState || p.Drafted) return false;

            // 2. 正在移动 -> 绝对不塞
            // 这是最重要的判断，保证玩家右键移动时不会因为塞珠子而停顿
            if (p.pather != null && p.pather.Moving) return false;

            // 3. 身体僵直 (攻击后摇、晕眩) -> 不塞
            if (p.stances.FullBodyBusy) return false;

            // 4. 任务状态检查
            if (p.CurJob != null)
            {
                // 正在攻击 -> 不塞 (防止断刀)
                if (p.CurJob.def == JobDefOf.AttackMelee || p.CurJob.def == JobDefOf.AttackStatic) return false;

                // 正在执行塞入 -> 不塞 (防止重复)
                if (p.CurJob.def == SpiritBeadsDefOf.Raven_Job_InsertBeads) return false;

                // 正在进食、救援等关键短任务 -> 建议不打断，防止卡死循环
                if (p.CurJob.def == JobDefOf.Ingest || p.CurJob.def == JobDefOf.Rescue) return false;
            }

            // 结论：只要站着不动（无论是征召站立 Wait_Combat，还是闲逛 Wait_Wander，还是工作间隙），就强制塞入
            return true;
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (isInserted)
            {
                SetInserted(pawn, false);
                SoundDef sound = DefDatabase<SoundDef>.GetNamedSilentFail("Hive_Spawn");
                sound?.PlayOneShot(pawn);
                Messages.Message("RavenRace_Msg_BeadsPopOut".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent);
            }
        }

        public void SetInserted(Pawn pawn, bool inserted)
        {
            this.isInserted = inserted;
            if (pawn == null) return;

            if (inserted)
            {
                if (Props.insertedHediff != null) pawn.health.AddHediff(Props.insertedHediff);
            }
            else
            {
                if (Props.insertedHediff != null)
                {
                    List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                    for (int i = hediffs.Count - 1; i >= 0; i--)
                    {
                        if (hediffs[i].def == Props.insertedHediff)
                        {
                            pawn.health.RemoveHediff(hediffs[i]);
                        }
                    }
                }
            }
        }

        public void StartInsertJob(Pawn pawn)
        {
            if (pawn.CurJobDef == SpiritBeadsDefOf.Raven_Job_InsertBeads) return;

            Job job = JobMaker.MakeJob(SpiritBeadsDefOf.Raven_Job_InsertBeads, pawn, parent);
            // 使用 InterruptForced 强制打断当前的 Wait/Idle 状态
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        // 地上选中时不显示
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        // 给 Patch 调用的 Gizmo 生成器
        public IEnumerable<Gizmo> GetEquippedGizmos(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer) yield break;

            if (iconBeads == null)
            {
                iconBeads = ContentFinder<Texture2D>.Get("UI/Commands/Raven_InsertBeads", true);
            }

            // 1. 自动模式开关
            // 使用原版 Command_Toggle，它会自动处理勾选框，我们只需要提供图标
            yield return new Command_Toggle
            {
                defaultLabel = "RavenRace_AutoInsert".Translate(),
                defaultDesc = "RavenRace_AutoInsertDesc".Translate(),
                icon = iconBeads, // 使用您提供的珠子图标
                isActive = () => autoInsert,
                toggleAction = () => autoInsert = !autoInsert
            };

            // 2. 手动塞入按钮
            // 仅当未插入时显示
            if (!isInserted)
            {
                bool isInserting = pawn.CurJobDef == SpiritBeadsDefOf.Raven_Job_InsertBeads;

                Command_Action insertCmd = new Command_Action
                {
                    defaultLabel = "RavenRace_InsertBeads".Translate(),
                    defaultDesc = "RavenRace_InsertBeadsDesc".Translate(),
                    icon = iconBeads, // 同样使用珠子图标
                    action = () => StartInsertJob(pawn)
                };

                if (isInserting)
                {
                    insertCmd.Disable("执行中...");
                }

                yield return insertCmd;
            }

            // 3. 大招 Gizmo (当设置开启且已插入时显示)
            if (RavenRaceMod.Settings.enableGrandClimax && isInserted)
            {
                AbilityDef grandClimaxDef = DefDatabase<AbilityDef>.GetNamed("Raven_Ability_GrandClimax");
                yield return new Command_Action
                {
                    defaultLabel = grandClimaxDef.LabelCap,
                    defaultDesc = grandClimaxDef.description,
                    icon = grandClimaxDef.uiIcon,
                    action = () =>
                    {
                        Ability ability = new Ability(pawn, grandClimaxDef);
                        Job job = ability.GetJob(new LocalTargetInfo(pawn), new LocalTargetInfo(pawn));
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                };
            }
        }
    }
}