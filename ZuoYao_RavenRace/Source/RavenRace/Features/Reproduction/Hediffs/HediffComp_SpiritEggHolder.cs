using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Sound;
using System.Text;
using HarmonyLib;
// [关键] 命名空间已改为 Features.Reproduction，所以不需要额外的 using，因为 CompSpiritEgg 也在这个命名空间下
// 但为了保险，显式声明一下
using RavenRace.Features.Reproduction;

namespace RavenRace.Features.Reproduction // [Change] Namespace
{
    public class HediffCompProperties_SpiritEggHolder : HediffCompProperties
    {
        public HediffCompProperties_SpiritEggHolder()
        {
            this.compClass = typeof(HediffCompSpiritEggHolder);
        }
    }

    public class HediffCompSpiritEggHolder : HediffComp, IThingHolder
    {
        public ThingOwner innerContainer;
        private const int MaxCapacity = 5;

        public HediffCompSpiritEggHolder()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public int TryAcceptThing(Thing thing)
        {
            if (innerContainer == null || thing == null) return 0;
            int acceptedCount = 0;
            int countToAdd = thing.stackCount;
            for (int i = 0; i < countToAdd; i++)
            {
                if (innerContainer.Count >= MaxCapacity) break;
                Thing singleThing = ThingMaker.MakeThing(thing.def);
                if (singleThing == null) continue;

                if (thing.TryGetComp<CompSpiritEgg>() is CompSpiritEgg originalComp && singleThing.TryGetComp<CompSpiritEgg>() is CompSpiritEgg newComp)
                {
                    Pawn mother = originalComp.FindPawnByIdAllWorld(originalComp.motherId);
                    Pawn father = originalComp.FindPawnByIdAllWorld(originalComp.fatherId);
                    newComp.Initialize(mother, father, originalComp.geneSet);
                }
                if (innerContainer.TryAdd(singleThing, false))
                {
                    acceptedCount++;
                }
                else
                {
                    singleThing.Destroy();
                }
            }
            if (acceptedCount > 0)
            {
                parent.Severity = (float)innerContainer.Count;
                Pawn?.Drawer?.renderer?.SetAllGraphicsDirty();
            }
            return acceptedCount;
        }

        // ... (TryAcceptEgg, CompExposeData, CompPostTick 保持不变，注意使用了 CompSpiritEgg 类名) ...
        public bool TryAcceptEgg(Thing egg)
        {
            if (egg.stackCount > 1) egg.stackCount = 1;
            return TryAcceptThing(egg) > 0;
        }

        public override void CompExposeData()
        {
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (innerContainer == null) return;

            if (Mathf.Abs(parent.Severity - innerContainer.Count) > 0.01f)
            {
                parent.Severity = (float)innerContainer.Count;
                if (Pawn?.Drawer?.renderer != null) Pawn.Drawer.renderer.SetAllGraphicsDirty();
            }

            if (!Pawn.Dead && !Pawn.Suspended)
            {
                float warmthDays = (RavenRaceMod.Settings != null) ? RavenRaceMod.Settings.spiritEggWarmthDays : 3.0f;
                if (warmthDays <= 0) warmthDays = 3.0f;
                float dailyIncrease = 1f / warmthDays;
                float tickIncrease = dailyIncrease / 60000f;
                for (int i = 0; i < innerContainer.Count; i++)
                {
                    if (innerContainer[i].def == RavenDefOf.Raven_SpiritEgg && innerContainer[i].TryGetComp<CompSpiritEgg>() is CompSpiritEgg eggComp && eggComp.warmthProgress < 1.0f)
                    {
                        eggComp.warmthProgress += tickIncrease;
                        if (eggComp.warmthProgress > 1.0f) eggComp.warmthProgress = 1.0f;
                    }
                }
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                if (innerContainer == null || innerContainer.Count == 0) return null;
                StringBuilder sb = new StringBuilder();
                bool hasFertilizedEgg = false;
                for (int i = 0; i < innerContainer.Count; i++)
                {
                    if (innerContainer[i].def == RavenDefOf.Raven_SpiritEgg && innerContainer[i].TryGetComp<CompSpiritEgg>() is CompSpiritEgg egg)
                    {
                        if (!hasFertilizedEgg)
                        {
                            sb.AppendLine("灵卵温养中:");
                            hasFertilizedEgg = true;
                        }
                        sb.AppendLine($" - (受精卵) 温养度: {egg.warmthProgress:P0}");
                    }
                }
                return hasFertilizedEgg ? sb.ToString().TrimEnd() : null;
            }
        }

        public override string CompLabelInBracketsExtra => $"{innerContainer?.Count ?? 0}/{MaxCapacity}";
        public override bool CompShouldRemove => false;

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (innerContainer != null && innerContainer.Count > 0 && Pawn.MapHeld != null)
            {
                innerContainer.TryDropAll(Pawn.PositionHeld, Pawn.MapHeld, ThingPlaceMode.Near);
                if (Pawn?.Drawer?.renderer != null) Pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        public IThingHolder ParentHolder => this.Pawn;
        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn.Faction != Faction.OfPlayer && !Pawn.IsPrisonerOfColony) yield break;
            if (DebugSettings.godMode && innerContainer != null && innerContainer.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: 瞬间温养",
                    action = () =>
                    {
                        foreach (var eggThing in innerContainer)
                        {
                            var eggComp = eggThing.TryGetComp<CompSpiritEgg>();
                            if (eggComp != null)
                            {
                                eggComp.warmthProgress = 1.0f;
                            }
                        }
                        Messages.Message("所有体内灵卵已完美温养。", MessageTypeDefOf.TaskCompletion);
                    }
                };
            }
            if (innerContainer == null || innerContainer.Count == 0) yield break;
            Thing lastEgg = innerContainer[innerContainer.Count - 1];
            if (RavenRaceMod.Settings.enableEggProjectileMode)
            {
                yield return new Command_Target
                {
                    defaultLabel = "RavenRace_Gizmo_LaunchEgg".Translate(),
                    defaultDesc = "RavenRace_Gizmo_LaunchEggDesc".Translate(),
                    icon = lastEgg.def.uiIcon,
                    targetingParams = TargetingParameters.ForAttackAny(),
                    action = target => LaunchEgg(target)
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "RavenRace_Gizmo_RemoveEgg".Translate(),
                    defaultDesc = "RavenRace_Gizmo_RemoveEggDesc".Translate(),
                    icon = lastEgg.def.uiIcon,
                    action = () =>
                    {
                        if (!Pawn.Downed && Pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        {
                            Pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(RavenDefOf.Raven_Job_RemoveSpiritEgg, Pawn), JobTag.Misc);
                        }
                        else
                        {
                            EjectEgg(false);
                        }
                    }
                };
            }
        }

        private void LaunchEgg(LocalTargetInfo target)
        {
            if (innerContainer.Count == 0) return;
            Thing eggToLaunch = innerContainer.Take(innerContainer[innerContainer.Count - 1], 1);
            parent.Severity = (float)innerContainer.Count;
            if (Pawn?.Drawer?.renderer != null) Pawn.Drawer.renderer.SetAllGraphicsDirty();
            DoEjectEffect();
            SpiritEggProjectile projectile = (SpiritEggProjectile)GenSpawn.Spawn(ThingDef.Named("Raven_Projectile_SpiritEgg"), Pawn.Position, Pawn.Map);
            projectile.storedEgg = eggToLaunch;
            projectile.Launch(Pawn, Pawn.DrawPos, target, target, ProjectileHitFlags.All);
        }

        public void EjectEgg(bool dropAll = false)
        {
            if (innerContainer == null || innerContainer.Count == 0) return;
            DoEjectEffect();
            if (dropAll)
            {
                innerContainer.TryDropAll(Pawn.Position, Pawn.Map, ThingPlaceMode.Near);
            }
            else
            {
                innerContainer.TryDrop(innerContainer[innerContainer.Count - 1], Pawn.Position, Pawn.Map, ThingPlaceMode.Near, out _);
            }
            parent.Severity = (float)innerContainer.Count;
            if (Pawn?.Drawer?.renderer != null) Pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        private void DoEjectEffect()
        {
            FleckMaker.ThrowMetaIcon(Pawn.Position, Pawn.Map, FleckDefOf.Heart);
            (DefDatabase<SoundDef>.GetNamedSilentFail("Hive_Spawn") ?? SoundDefOf.Standard_Drop).PlayOneShot(Pawn);
        }
    }
}