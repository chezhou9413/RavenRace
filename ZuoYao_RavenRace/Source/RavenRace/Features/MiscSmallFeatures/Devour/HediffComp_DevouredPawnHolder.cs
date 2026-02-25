using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RavenRace.Features.MiscSmallFeatures.Devour
{
    public class HediffCompProperties_DevouredPawnHolder : HediffCompProperties
    {
        public HediffCompProperties_DevouredPawnHolder()
        {
            this.compClass = typeof(HediffComp_DevouredPawnHolder);
        }
    }

    /// <summary>
    /// 胎内牢笼。像休眠舱一样彻底锁死猎物的需求与时间，并提供“排泄”按钮。
    /// </summary>
    public class HediffComp_DevouredPawnHolder : HediffComp, IThingHolder
    {
        public ThingOwner innerContainer;
        private static readonly Texture2D EjectIcon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject", true);

        public HediffComp_DevouredPawnHolder()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public override void CompPostMake()
        {
            base.CompPostMake();
            // 冻结内部对象的时间流逝 (休眠舱核心逻辑)
            innerContainer.dontTickContents = true;
        }

        public override void CompExposeData()
        {
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (innerContainer == null)
                {
                    innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
                }
                innerContainer.dontTickContents = true;
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            EjectContents();
        }

        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (innerContainer != null && innerContainer.Count > 0 && innerContainer[0] is Pawn prey)
                {
                    return "塞入: " + prey.LabelShort;
                }
                return "空虚";
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn.Faction != Faction.OfPlayer && !Pawn.IsPrisonerOfColony) yield break;

            if (innerContainer != null && innerContainer.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "排泄猎物",
                    defaultDesc = "伴随着腔道的剧烈收缩与一阵淫靡的水声，将体内的猎物连同浓稠的黏液一起强制排出体外。猎物将陷入不可抗拒的绝顶与深度麻醉中。",
                    icon = EjectIcon,
                    action = () =>
                    {
                        EjectContents();
                    }
                };
            }
        }

        public void EjectContents()
        {
            if (innerContainer == null || innerContainer.Count == 0 || Pawn.MapHeld == null) return;

            foreach (Thing thing in innerContainer)
            {
                if (thing is Pawn victim)
                {
                    PawnComponentsUtility.AddComponentsForSpawn(victim);

                    // 强制绝顶麻醉
                    victim.health.AddHediff(HediffDefOf.Anesthetic);

                    // 1.5/1.6 的 GainFilth 不接受数字参数
                    victim.filth.GainFilth(ThingDefOf.Filth_Slime);

                    // 在地上喷洒黏液
                    FilthMaker.TryMakeFilth(Pawn.PositionHeld, Pawn.MapHeld, ThingDefOf.Filth_Slime, 3);

                    Messages.Message($"{victim.LabelShort} 被一股腥甜的淫水喷射了出来，浑身泥泞地陷入了绝顶的昏迷。", victim, MessageTypeDefOf.NeutralEvent);
                }
            }

            innerContainer.TryDropAll(Pawn.PositionHeld, Pawn.MapHeld, ThingPlaceMode.Near);

            FleckMaker.ThrowDustPuffThick(Pawn.PositionHeld.ToVector3Shifted(), Pawn.MapHeld, 1.5f, Color.white);

            // [核心修复] 替换为安全的单次触发音效：如果有虫巢生成声(极其黏糊的噗叽声)就用它，否则用默认掉落声
            (DefDatabase<SoundDef>.GetNamedSilentFail("Hive_Spawn") ?? SoundDefOf.Standard_Drop).PlayOneShot(Pawn);

            Pawn.health.RemoveHediff(this.parent);
        }

        // IThingHolder 接口实现
        public IThingHolder ParentHolder => this.Pawn;
        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        public ThingOwner GetDirectlyHeldThings() => innerContainer;
    }
}