using Verse;

namespace RavenRace.Features.DegradationCharm.Comps
{
    // 这个组件的属性类，在XML中引用
    public class CompProperties_ApplyCharm : CompProperties
    {
        public CompProperties_ApplyCharm() => this.compClass = typeof(CompApplyCharm);
    }

    // 一个空的组件，仅用作物品的标记，让我们的代码可以识别出“堕落符咒”
    public class CompApplyCharm : ThingComp { }
}