using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace RavenRace.Features.Storyteller.Incidents
{
    /// <summary>
    /// 自定义加入信件。
    /// 这里的 Pawn 暂时存储在信件中 (World)，只有玩家点击接受后才会生成到地图上。
    /// </summary>
    public class ChoiceLetter_RavenJoin : ChoiceLetter
    {
        public Pawn joiner;
        public Map map;

        public override bool CanShowInLetterStack => base.CanShowInLetterStack && joiner != null && !joiner.Destroyed;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly)
                {
                    yield return Option_Close;
                }
                else
                {
                    // 1. 接受选项
                    DiaOption optAccept = new DiaOption("AcceptButton".Translate());
                    optAccept.action = () =>
                    {
                        Accept();
                        Find.LetterStack.RemoveLetter(this);
                    };
                    optAccept.resolveTree = true;
                    if (map == null || !map.IsPlayerHome)
                    {
                        optAccept.Disable("CannotAcceptQuestNoMap".Translate());
                    }
                    yield return optAccept;

                    // 2. 拒绝选项
                    DiaOption optReject = new DiaOption("RejectLetter".Translate());
                    optReject.action = () =>
                    {
                        Reject();
                        Find.LetterStack.RemoveLetter(this);
                    };
                    optReject.resolveTree = true;
                    yield return optReject;

                    // 3. 推迟选项
                    yield return Option_Postpone;
                }
            }
        }

        private void Accept()
        {
            if (joiner == null || map == null) return;

            // 寻找地图边缘生成点
            IntVec3 loc;
            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out loc))
            {
                // 如果找不到路，就直接空投到中心
                loc = DropCellFinder.TradeDropSpot(map);
            }

            // 生成 Pawn 到地图
            GenSpawn.Spawn(joiner, loc, map);

            // 确保派系正确
            joiner.SetFaction(Faction.OfPlayer, null);

            // 发送消息
            Messages.Message("RavenRace_Message_RavenJoined".Translate(joiner.LabelShort), joiner, MessageTypeDefOf.PositiveEvent);
        }

        private void Reject()
        {
            if (joiner != null && !joiner.Spawned)
            {
                // 彻底销毁这个未出生的 Pawn
                joiner.Destroy(DestroyMode.Vanish);
                joiner = null;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // 必须深度保存 Pawn，因为它还没生成到地图上，否则存读档会丢失
            Scribe_Deep.Look(ref joiner, "joiner");
            Scribe_References.Look(ref map, "map");
        }
    }
}