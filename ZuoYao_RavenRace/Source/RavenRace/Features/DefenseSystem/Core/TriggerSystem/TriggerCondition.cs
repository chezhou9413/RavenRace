namespace RavenRace
{
    public enum TriggerCondition
    {
        StepOn,     // 踩踏触发
        Proximity,  // 接近触发
        Damage,     // 自身受损触发
        WallDamage, // [新增] 附着墙体受损触发
        Timer,      // 定时触发
        Manual      // 手动触发
    }
}