using Verse;

namespace RavenRace
{
    /// <summary>
    /// 静态访问器，方便UI和其他类调用
    /// </summary>
    public static class FusangResourceManager
    {
        public static WorldComponent_Fusang Comp => Find.World.GetComponent<WorldComponent_Fusang>();

        public static int GetAmount(FusangResourceType type) => Comp?.GetResource(type) ?? 0;

        public static void Add(FusangResourceType type, int amount) => Comp?.ModifyResource(type, amount);

        public static bool TryConsume(FusangResourceType type, int amount)
        {
            if (GetAmount(type) >= amount)
            {
                Add(type, -amount);
                return true;
            }
            return false;
        }
    }
}