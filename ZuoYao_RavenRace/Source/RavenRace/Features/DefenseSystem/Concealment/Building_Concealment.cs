using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using Verse.Sound;

namespace RavenRace.Features.DefenseSystem.Concealment
{
    [StaticConstructorOnStartup]
    public class Building_Concealment : Building_TurretGun, IThingHolder
    {
        // 存储
        protected ThingOwner innerContainer;

        // 状态
        public int lastAttackTick = -99999;
        public const int RevealDuration = 600; // 10秒

        // 缓存
        private Thing defaultInvisibleGun;

        // 图标
        private static readonly Texture2D ExitIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);
        private static readonly Texture2D SwapIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);

        public Building_Concealment()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public Pawn Occupant => innerContainer.Count > 0 ? innerContainer[0] as Pawn : null;
        public bool HasOccupant => innerContainer.Count > 0;
        public bool IsRevealed => Find.TickManager.TicksGame < lastAttackTick + RevealDuration;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (this.gun != null && defaultInvisibleGun == null)
            {
                defaultInvisibleGun = this.gun;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref lastAttackTick, "lastAttackTick", -99999);
        }

        // ============================================================
        // 核心逻辑
        // ============================================================

        protected override void Tick()
        {
            innerContainer.DoTick();

            if (HasOccupant)
            {
                CheckOccupantStatus();
                // 只有有人且有电时才执行炮塔逻辑
                var pc = this.GetComp<CompPowerTrader>();
                if (pc == null || pc.PowerOn)
                {
                    base.Tick();
                }
                else
                {
                    // 没电/没人时重置冷却，防止自动开火
                    burstCooldownTicksLeft = 0;
                }
            }
            else
            {
                base.Tick();
            }
        }

        private void CheckOccupantStatus()
        {
            Pawn p = Occupant;
            if (p == null) return;
            if (p.Downed || p.Dead || p.InMentalState)
            {
                EjectOccupant();
            }
            else if (p.needs?.food != null && p.needs.food.Starving)
            {
                Messages.Message($"{p.LabelShort} 因为极度饥饿离开了掩体。", this, MessageTypeDefOf.NeutralEvent);
                EjectOccupant();
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        // ============================================================
        // 进出与武器 (Enter / Exit / Weapon)
        // ============================================================

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            if (HasOccupant)
            {
                yield return new FloatMenuOption("Raven_Concealment_Occupied".Translate(), null);
                yield break;
            }

            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotReach".Translate(), null);
                yield break;
            }

            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Raven_EnterConcealment".Translate(), delegate ()
            {
                Job job = JobMaker.MakeJob(DefenseDefOf.Raven_Job_EnterConcealment, this);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }), selPawn, this);
        }

        public bool TryAcceptPawn(Pawn p)
        {
            if (HasOccupant) return false;
            if (innerContainer.TryAddOrTransfer(p, true))
            {
                SwapGunToPawnWeapon(p);
                SoundDefOf.Standard_Pickup.PlayOneShot(this);
                return true;
            }
            return false;
        }

        public void EjectOccupant()
        {
            if (!HasOccupant) return;
            RestoreDefaultGun();
            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
            SoundDefOf.Standard_Drop.PlayOneShot(this);
        }

        private void SwapGunToPawnWeapon(Pawn p, Thing specificWeapon = null)
        {
            Thing targetWeapon = specificWeapon ?? p.equipment?.Primary;

            if (targetWeapon == null || !targetWeapon.def.IsRangedWeapon)
            {
                Messages.Message($"{p.LabelShort} 没有可用的远程武器。", this, MessageTypeDefOf.RejectInput);
                return;
            }

            // 1. 先生成新枪
            Thing newGun = ThingMaker.MakeThing(targetWeapon.def);

            // [核心修复] 使用 ArtGenerationContext.Outsider 防止生成 Tale 导致的空引用报错
            if (newGun.TryGetComp<CompQuality>() is CompQuality q1 && targetWeapon.TryGetComp<CompQuality>() is CompQuality q2)
            {
                q1.SetQuality(q2.Quality, ArtGenerationContext.Outsider);
            }

            // 2. 销毁旧枪
            if (this.gun != null && !this.gun.Destroyed)
            {
                this.gun.Destroy();
            }

            // 3. 赋值新枪
            this.gun = newGun;

            // 4. 刷新 Verb
            var method = AccessTools.Method(typeof(Building_TurretGun), "UpdateGunVerbs");
            method?.Invoke(this, null);
        }

        private void RestoreDefaultGun()
        {
            if (this.gun != null && !this.gun.Destroyed) this.gun.Destroy();
            this.MakeGun();
        }

        protected override void BeginBurst()
        {
            base.BeginBurst();
            // 注意：这里不再设置 lastAttackTick，因为具体的暴露逻辑移交给了 Harmony Patch
            // 这里只负责加经验和记录
            Pawn p = Occupant;
            if (p != null && !p.Dead && p.skills != null)
            {
                p.skills.Learn(SkillDefOf.Shooting, 10f);
                p.records.Increment(RecordDefOf.ShotsFired);
            }
        }

        // ============================================================
        // 安全性
        // ============================================================

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            EjectOccupant();
            base.Destroy(mode);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            EjectOccupant();
            base.DeSpawn(mode);
        }

        // [核心修复] 允许强制攻击
        protected override bool CanSetForcedTarget
        {
            get
            {
                return HasOccupant && Faction == Faction.OfPlayer;
            }
        }

        // [核心修复] 防止 DrawExtraSelectionOverlays 报错
        public override void DrawExtraSelectionOverlays()
        {
            if (this.gun == null || this.gun.Destroyed || this.AttackVerb == null)
            {
                return;
            }
            try
            {
                base.DrawExtraSelectionOverlays();
            }
            catch (Exception) { }
        }

        // ============================================================
        // UI & Gizmos
        // ============================================================

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            if (HasOccupant)
            {
                Pawn p = Occupant;

                if (AttackVerb != null)
                {
                    var attackCmd = new Command_VerbTarget
                    {
                        defaultLabel = "CommandSetForceAttackTarget".Translate(),
                        defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                        verb = AttackVerb,
                        hotKey = KeyBindingDefOf.Misc4,
                        drawRadius = false
                    };
                    yield return attackCmd;
                }

                yield return new Command_Action
                {
                    defaultLabel = "Raven_Concealment_SwapWeapon".Translate(),
                    defaultDesc = "Raven_Concealment_SwapWeaponDesc".Translate(),
                    icon = SwapIcon,
                    action = () =>
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        if (p.equipment?.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
                        {
                            options.Add(new FloatMenuOption($"装备: {p.equipment.Primary.LabelCap}", () => SwapGunToPawnWeapon(p, p.equipment.Primary)));
                        }

                        if (p.inventory != null && p.inventory.innerContainer != null)
                        {
                            foreach (var item in p.inventory.innerContainer)
                            {
                                if (item.def.IsRangedWeapon)
                                {
                                    options.Add(new FloatMenuOption($"库存: {item.LabelCap}", () => SwapGunToPawnWeapon(p, item)));
                                }
                            }
                        }

                        if (options.Count == 0) options.Add(new FloatMenuOption("无可用远程武器", null));
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "Raven_ExitConcealment".Translate(),
                    defaultDesc = "Raven_ExitConcealmentDesc".Translate(),
                    icon = ExitIcon,
                    action = () => EjectOccupant()
                };
            }
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();

            string baseStr = base.GetInspectString();
            if (!baseStr.NullOrEmpty())
            {
                sb.Append(baseStr);
            }

            if (HasOccupant)
            {
                if (sb.Length > 0) sb.AppendLine();

                sb.Append("Occupant".Translate() + ": " + Occupant.LabelShort);

                if (this.gun != null)
                {
                    sb.AppendLine();
                    sb.Append($"武器: {this.gun.LabelCap}");
                    if (AttackVerb != null)
                    {
                        sb.Append($" (射程: {AttackVerb.verbProps.range})");
                    }
                }

                sb.AppendLine();
                if (IsRevealed)
                    sb.Append("状态: " + "Raven_Concealment_Revealed".Translate());
                else
                    sb.Append("状态: " + "Raven_Concealment_Hidden".Translate());
            }
            else
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("Raven_Concealment_Vacant".Translate());
            }

            return sb.ToString().Trim();
        }
    }
}