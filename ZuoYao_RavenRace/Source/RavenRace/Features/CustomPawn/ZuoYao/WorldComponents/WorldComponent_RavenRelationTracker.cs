using System.Collections.Generic;
using Verse;
using RimWorld.Planet;

namespace RavenRace.Features.CustomPawn.ZuoYao
{
    /// <summary>
    /// 左爻功能核心组件：用于存储和管理特殊的人际关系数据（别天神）。
    /// </summary>
    public class WorldComponent_RavenRelationTracker : WorldComponent
    {
        // 存储自定义称呼
        // Key: "观察者ID_被观察者ID" (即：谁在看谁)
        private Dictionary<string, string> customLabels = new Dictionary<string, string>();

        // 存储锁定的好感度数值
        // Key: "SubjectID_OtherID" (Subject对Other的看法)
        private Dictionary<string, int> lockedOpinions = new Dictionary<string, int>();

        public WorldComponent_RavenRelationTracker(World world) : base(world) { }

        private string GetKey(Pawn subject, Pawn other)
        {
            if (subject == null || other == null) return null;
            return $"{subject.ThingID}_{other.ThingID}";
        }

        /// <summary>
        /// 设置别天神确立的关系数据
        /// </summary>
        public void SetRelationData(Pawn master, Pawn servant, string masterLabel, string servantLabel, int servantToMasterOpinion, int masterToServantOpinion)
        {
            // 1. 称呼设置
            // 当 奴仆 看 主人 时，显示 masterLabel (例如 "绝对主人")
            string keyS2M = GetKey(servant, master);
            if (!string.IsNullOrEmpty(masterLabel)) customLabels[keyS2M] = masterLabel;
            else customLabels.Remove(keyS2M);

            // 当 主人 看 奴仆 时，显示 servantLabel (例如 "忠诚奴仆")
            string keyM2S = GetKey(master, servant);
            if (!string.IsNullOrEmpty(servantLabel)) customLabels[keyM2S] = servantLabel;
            else customLabels.Remove(keyM2S);

            // 2. 好感度锁定
            lockedOpinions[keyS2M] = servantToMasterOpinion;
            lockedOpinions[keyM2S] = masterToServantOpinion;
        }

        /// <summary>
        /// 获取自定义关系标签（如果存在）
        /// </summary>
        public string GetCustomLabel(Pawn observer, Pawn target)
        {
            string key = GetKey(observer, target);
            if (key != null && customLabels.TryGetValue(key, out string label))
            {
                return label;
            }
            return null;
        }

        /// <summary>
        /// 获取锁定的好感度（如果存在）
        /// </summary>
        public int? GetLockedOpinion(Pawn subject, Pawn other)
        {
            string key = GetKey(subject, other);
            if (key != null && lockedOpinions.TryGetValue(key, out int val))
            {
                return val;
            }
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref customLabels, "customLabels", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref lockedOpinions, "lockedOpinions", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (customLabels == null) customLabels = new Dictionary<string, string>();
                if (lockedOpinions == null) lockedOpinions = new Dictionary<string, int>();
            }
        }
    }
}