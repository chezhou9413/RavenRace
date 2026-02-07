using System.Collections.Generic;
using Verse;

namespace RavenRace
{
    public static class AltarGeometryUtility
    {
        // 核心为3x3, 中心点为 (0,0)。
        // 这里的坐标是相对于中心点的偏移量 (Rot4.North)

        // 内环 (8个): 紧贴3x3核心外一圈
        public static readonly List<IntVec3> InfuserOffsets = new List<IntVec3>
        {
            new IntVec3(0, 0, 3),   // N
            new IntVec3(2, 0, 2),   // NE
            new IntVec3(3, 0, 0),   // E
            new IntVec3(2, 0, -2),  // SE
            new IntVec3(0, 0, -3),  // S
            new IntVec3(-2, 0, -2), // SW
            new IntVec3(-3, 0, 0),  // W
            new IntVec3(-2, 0, 2)   // NW
        };

        // 外环 (12个): 半径约为 5-6
        public static readonly List<IntVec3> PylonOffsets = new List<IntVec3>
        {
            new IntVec3(0, 0, 6),
            new IntVec3(3, 0, 5), new IntVec3(5, 0, 3),
            new IntVec3(6, 0, 0),
            new IntVec3(5, 0, -3), new IntVec3(3, 0, -5),
            new IntVec3(0, 0, -6),
            new IntVec3(-3, 0, -5), new IntVec3(-5, 0, -3),
            new IntVec3(-6, 0, 0),
            new IntVec3(-5, 0, 3), new IntVec3(-3, 0, 5)
        };

        // 注入仪 (4个): 位于内环和外环之间的角落
        public static readonly List<IntVec3> InjectorOffsets = new List<IntVec3>
        {
            new IntVec3(4, 0, 4),   // NE
            new IntVec3(4, 0, -4),  // SE
            new IntVec3(-4, 0, -4), // SW
            new IntVec3(-4, 0, 4)   // NW
        };

        // 根据主建筑旋转调整偏移
        public static IntVec3 GetRotatedOffset(IntVec3 offset, Rot4 rotation)
        {
            return offset.RotatedBy(rotation);
        }
    }
}