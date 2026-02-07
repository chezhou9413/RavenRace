using System.Collections.Generic;
using Verse;
using RimWorld.Planet;

namespace RavenRace
{
    public class WorldComponent_RavenRelationTracker : WorldComponent
    {
        // 存储自定义称呼
        private Dictionary<string, string> customMasterLabels = new Dictionary<string, string>();
        private Dictionary<string, string> customServantLabels = new Dictionary<string, string>();

        // [新增] 存储锁定的好感度数值
        // Key: "SubjectID_OtherID", Value: Opinion
        private Dictionary<string, int> lockedOpinions = new Dictionary<string, int>();

        public WorldComponent_RavenRelationTracker(World world) : base(world) { }

        private string GetKey(Pawn subject, Pawn other)
        {
            if (subject == null || other == null) return null;
            return $"{subject.ThingID}_{other.ThingID}";
        }

        // 设置自定义称呼和数值
        public void SetRelationData(Pawn master, Pawn servant, string masterLbl, string servantLbl, int s2mOpinion, int m2sOpinion)
        {
            string keyM2S = GetKey(master, servant); // 主人看奴仆
            string keyS2M = GetKey(servant, master); // 奴仆看主人

            // 1. 保存称呼
            if (!string.IsNullOrEmpty(masterLbl)) customMasterLabels[keyS2M] = masterLbl;
            else customMasterLabels.Remove(keyS2M);

            if (!string.IsNullOrEmpty(servantLbl)) customServantLabels[keyM2S] = servantLbl;
            else customServantLabels.Remove(keyM2S);

            // 2. 保存好感度
            // Servant -> Master
            lockedOpinions[keyS2M] = s2mOpinion;
            // Master -> Servant
            lockedOpinions[keyM2S] = m2sOpinion;
        }

        public string GetMasterLabel(Pawn master, Pawn servant)
        {
            // 谁是观察者？如果是 servant 观察 master，我们需要 master 的 label
            // 这里 key 应该是 servant_master
            string key = GetKey(servant, master);
            if (key != null && customMasterLabels.TryGetValue(key, out string label)) return label;
            return null;
        }

        public string GetServantLabel(Pawn master, Pawn servant)
        {
            // master 观察 servant
            string key = GetKey(master, servant);
            if (key != null && customServantLabels.TryGetValue(key, out string label)) return label;
            return null;
        }

        // 获取锁定的好感度
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
            Scribe_Collections.Look(ref customMasterLabels, "customMasterLabels", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref customServantLabels, "customServantLabels", LookMode.Value, LookMode.Value);
            // [新增]
            Scribe_Collections.Look(ref lockedOpinions, "lockedOpinions", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (customMasterLabels == null) customMasterLabels = new Dictionary<string, string>();
                if (customServantLabels == null) customServantLabels = new Dictionary<string, string>();
                if (lockedOpinions == null) lockedOpinions = new Dictionary<string, int>();
            }
        }
    }
}