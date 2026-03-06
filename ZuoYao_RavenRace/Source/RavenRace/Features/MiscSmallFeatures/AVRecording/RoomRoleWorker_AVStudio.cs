using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.AVRecording
{
    /// <summary>
    /// AV摄影房的房间判定逻辑。
    /// 【需求更新】：取消床铺限制。只要房间内存在“AV摄影机”，这个房间就会被强制定性为 AV摄影房。
    /// 分数极高，确保能覆盖掉普通的“卧室”或“兵营”判定。
    /// </summary>
    public class RoomRoleWorker_AVStudio : RoomRoleWorker
    {
        public override float GetScore(Room room)
        {
            // 遍历房间内所有的建筑和物品
            List<Thing> things = room.ContainedAndAdjacentThings;
            for (int i = 0; i < things.Count; i++)
            {
                // 只要发现有摄影机，立刻返回极高分（1,000,000）
                if (things[i] is Building_AVCamera)
                {
                    return 1000000f;
                }
            }

            return 0f;
        }

        public override float GetScoreDeltaIfBuildingPlaced(Room room, ThingDef buildingDef)
        {
            // 如果已经是AV摄影房了，再放东西也不加分了
            if (room.Role != null && room.Role.Worker is RoomRoleWorker_AVStudio)
            {
                return 0f;
            }

            // 预判玩家即将放置的蓝图，如果放的是摄影机，那就给高分
            if (buildingDef.thingClass == typeof(Building_AVCamera))
            {
                return 1000000f;
            }

            return 0f;
        }
    }
}