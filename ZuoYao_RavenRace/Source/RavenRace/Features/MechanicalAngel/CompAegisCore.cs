using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace RavenRace.Features.MechanicalAngel
{
    public class CompProperties_AegisCore : CompProperties
    {
        public CompProperties_AegisCore()
        {
            this.compClass = typeof(CompAegisCore);
        }
    }

    /// <summary>
    /// 艾吉斯核心组件。
    /// 负责提供 UI Gizmo，允许玩家开启或关闭“主动夜袭榨取”功能。
    /// </summary>
    public class CompAegisCore : ThingComp
    {
        // 默认允许主动充能
        public bool allowLustCharge = true;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref allowLustCharge, "allowLustCharge", true);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Pawn pawn = this.parent as Pawn;
            if (pawn != null && pawn.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "允许主动榨取充能",
                    defaultDesc = "开启后，当艾吉斯的淫能过低时，她会自动寻路到被绑定为她'主人'的殖民者身边，通过强制性行为补充能量。关闭则她只能回充电站。",
                    icon = ContentFinder<Texture2D>.Get("UI/Icons/HeartIcon", true),
                    isActive = () => allowLustCharge,
                    toggleAction = () => allowLustCharge = !allowLustCharge
                };
            }
        }
    }
}