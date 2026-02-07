using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.Espionage
{
    /// <summary>
    /// 记录一名间谍的详细数据。
    /// 间谍可以是殖民者，也可以是虚拟特工。
    /// </summary>
    public class SpyData : IExposable, ILoadReferenceable
    {
        // --- 唯一标识 ---
        private int uniqueID;

        // --- 来源 ---
        public SpySourceType sourceType;
        public Pawn colonistRef;        // 如果是殖民者，引用之
        public string agentName;        // 如果是特工，存储名字

        // --- 当前状态 ---
        public SpyState state = SpyState.Idle;
        public Faction targetFaction;   // 当前渗透的目标派系
        public float exposure = 0f;     // 暴露值 (0-100)

        // [新增] 当前执行的任务引用
        public ActiveMission currentMission;


        // --- 属性 (0-100) ---
        // 基础值 + 成长值
        public float statInfiltration;  // 潜伏
        public float statOperation;     // 行动
        public float statNetwork;       // 网络
        public float statAdaptation;    // 应变

        // --- 经验与等级 ---
        public int level = 1;
        public float xp = 0f;

        public SpyData() { }

        public SpyData(int id)
        {
            this.uniqueID = id;
        }

        /// <summary>
        /// 根据殖民者初始化间谍数据
        /// </summary>
        public void InitializeFromPawn(Pawn p)
        {
            this.sourceType = SpySourceType.Colonist;
            this.colonistRef = p;
            this.agentName = p.Name.ToStringShort;
            RecalculateStats();
        }

        /// <summary>
        /// 重新计算属性 (基于技能)
        /// 公式参考设计文档：
        /// 潜伏 = (社交x0.4 + 智识x0.3 + 艺术x0.3) * 5
        /// 行动 = (格斗x0.3 + 射击x0.3 + 医疗x0.2 + 建造x0.2) * 5
        /// 网络 = (社交x0.5 + 智识x0.3 + 交易x0.2) * 5
        /// 应变 = (格斗x0.3 + 智识x0.3 + 医疗x0.2 + 社交x0.2) * 5
        /// </summary>
        public void RecalculateStats()
        {
            if (colonistRef == null) return;

            var skills = colonistRef.skills;
            float social = skills.GetSkill(SkillDefOf.Social).Level;
            float intel = skills.GetSkill(SkillDefOf.Intellectual).Level;
            float art = skills.GetSkill(SkillDefOf.Artistic).Level;
            float melee = skills.GetSkill(SkillDefOf.Melee).Level;
            float shooting = skills.GetSkill(SkillDefOf.Shooting).Level;
            float medicine = skills.GetSkill(SkillDefOf.Medicine).Level;
            float construct = skills.GetSkill(SkillDefOf.Construction).Level;

            // 基础属性计算
            statInfiltration = (social * 0.4f + intel * 0.3f + art * 0.3f) * 5f;
            statOperation = (melee * 0.3f + shooting * 0.3f + medicine * 0.2f + construct * 0.2f) * 5f;
            statNetwork = (social * 0.5f + intel * 0.3f + social * 0.2f) * 5f; // 交易算在Social里或者单独处理
            statAdaptation = (melee * 0.3f + intel * 0.3f + medicine * 0.2f + social * 0.2f) * 5f;

            // 加上等级加成 (每级+3)
            float levelBonus = (level - 1) * 3f;
            statInfiltration += levelBonus;
            statOperation += levelBonus;
            statNetwork += levelBonus;
            statAdaptation += levelBonus;

            // 限制范围 0-100
            ClampStats();
        }

        private void ClampStats()
        {
            statInfiltration = Mathf.Clamp(statInfiltration, 0, 100);
            statOperation = Mathf.Clamp(statOperation, 0, 100);
            statNetwork = Mathf.Clamp(statNetwork, 0, 100);
            statAdaptation = Mathf.Clamp(statAdaptation, 0, 100);
        }

        public string Label => colonistRef != null ? colonistRef.LabelShort : agentName;

        public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueID, "uniqueID");
            Scribe_Values.Look(ref sourceType, "sourceType");
            Scribe_References.Look(ref colonistRef, "colonistRef");
            Scribe_Values.Look(ref agentName, "agentName");

            Scribe_Values.Look(ref state, "state");
            Scribe_References.Look(ref targetFaction, "targetFaction");
            Scribe_Values.Look(ref exposure, "exposure");


            Scribe_References.Look(ref currentMission, "currentMission");

            Scribe_Values.Look(ref statInfiltration, "statInfiltration");
            Scribe_Values.Look(ref statOperation, "statOperation");
            Scribe_Values.Look(ref statNetwork, "statNetwork");
            Scribe_Values.Look(ref statAdaptation, "statAdaptation");

            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref xp, "xp", 0f);
        }

        public string GetUniqueLoadID()
        {
            return "Raven_Spy_" + uniqueID;
        }
    }
}