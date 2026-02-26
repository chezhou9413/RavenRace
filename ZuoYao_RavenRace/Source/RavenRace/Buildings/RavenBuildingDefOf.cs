using RimWorld;
using Verse;

namespace RavenRace.Buildings
{
    /// <summary>
    /// 存储与“建筑”功能模块相关的Def定义的静态类。
    /// 使用[DefOf]属性，游戏启动时会自动为标记的字段赋值。
    /// </summary>
    [DefOf]
    public static class RavenBuildingDefOf
    {
        // 用于在催情香炉范围内时，给Pawn添加的心情
        public static ThoughtDef Raven_Thought_IncenseSmell;

        // 静态构造函数，确保DefOf在游戏加载时被正确初始化
        static RavenBuildingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RavenBuildingDefOf));
        }
    }
}