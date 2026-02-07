using Verse;
using RimWorld;

namespace RavenRace
{
    public class PlaceWorker_OnWall : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            // 阔剑地雷是朝前的，所以墙在它屁股后面
            // FacingCell 是朝向的向量，所以 loc - rot.FacingCell 就是背后的格子
            IntVec3 wallCell = loc - rot.FacingCell;

            if (!wallCell.InBounds(map))
            {
                return false;
            }

            Building edifice = wallCell.GetEdifice(map);
            if (edifice == null || edifice.def.graphicData == null)
            {
                return "MustPlaceOnWall".Translate();
            }

            // 检查是否是墙 (LinkFlags.Wall 是最靠谱的判断，或者检查 Passability)
            // 同时也允许放在自然岩石上
            if ((edifice.def.graphicData.linkFlags & LinkFlags.Wall) != 0 || edifice.def.building.isNaturalRock)
            {
                return true;
            }

            return "MustPlaceOnWall".Translate();
        }
    }
}