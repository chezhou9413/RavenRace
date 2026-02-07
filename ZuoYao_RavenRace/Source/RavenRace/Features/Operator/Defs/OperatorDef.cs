using System.Collections.Generic;
using Verse;

// [错误修复 2] 命名空间修正，与XML中的完整类名标签保持绝对一致
namespace RavenRace.Features.Operator.Defs
{
    public class OperatorDef : Def
    {
        public List<FavorabilityLevelData> favorabilityLevels;
    }

    public class FavorabilityLevelData
    {
        public int level;
        public string expressionsPath;
    }
}