using Verse;

namespace RavenRace.Features.Espionage
{
    /// <summary>
    /// 间谍的当前工作状态
    /// </summary>
    public enum SpyState
    {
        Idle,           // 待命 (在基地)
        Traveling,      // 前往目标派系途中 (暂定，如果瞬达可不使用)
        Infiltrating,   // 驻扎渗透中 (提供被动情报)
        OnMission,      // 执行特定任务中
        Cooldown,       // 任务后冷却/潜逃
        Captured,       // 被捕
        Dead            // 已确认死亡
    }

    /// <summary>
    /// 间谍的来源类型
    /// </summary>
    public enum SpySourceType
    {
        Colonist,       // 我方殖民者 (实体Pawn)
        FusangAgent,    // 扶桑特工 (虚拟数据)
        Turncoat        // 策反的内线 (目标派系的Pawn)
    }

    /// <summary>
    /// 官员在权力结构中的层级
    /// </summary>
    public enum OfficialRank
    {
        Leader = 0,         // 领袖 (最高)
        HighCouncil = 1,    // 核心层 (决策者)
        MiddleManager = 2,  // 中层干部 (执行者)
        KeyFigure = 3,      // 关键人物 (底层/替补)
        None = 99           // 无/未定义
    }

    /// <summary>
    /// 对派系的控制状态
    /// </summary>
    public enum FactionControlStatus
    {
        Independent,    // 独立 (默认)
        Infiltrated,    // 渗透 (掌握部分情报)
        Influenced,     // 影响 (有内线)
        Puppet,         // 傀儡 (完全控制)
        Annexed         // 吞并 (并入我方)
    }
}