using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using RavenRace.Features.Operator.Rewards;
using RavenRace.Features.Operator.Defs;

namespace RavenRace.Features.Operator
{
    /// <summary>
    /// 全局接线员管理器，负责追踪左爻的状态并提供数据。
    /// </summary>
    [StaticConstructorOnStartup]
    public class WorldComponent_OperatorManager : WorldComponent
    {
        // --- 核心数据 ---
        public int zuoYaoFavorability = 0;
        public const int MaxFavorability = 1000;
        public const int MinFavorability = -1000;
        public int activeExpressionLevel = 0;

        public HashSet<string> unlockedRewardDefs = new HashSet<string>();
        public HashSet<string> collectedUnderwearDefs = new HashSet<string>();

        // --- 静态缓存 ---
        private static Dictionary<int, List<Texture2D>> expressionCache = new Dictionary<int, List<Texture2D>>();
        private static OperatorDef operatorDef;
        private static Texture2D fallbackTexture;
        private static bool isInitialized = false;

        public WorldComponent_OperatorManager(World world) : base(world) { }

        // [新增] 用于临时存储赠礼后的对话
        public static string PostGiftMessage = null;

        private static void EnsureInitialized()
        {
            if (isInitialized) return;
            operatorDef = DefDatabase<OperatorDef>.GetNamed("Operator_ZuoYao");
            fallbackTexture = ContentFinder<Texture2D>.Get("UI/Fusang/LeaderPortrait");
            if (operatorDef?.favorabilityLevels != null)
            {
                foreach (var levelData in operatorDef.favorabilityLevels)
                {
                    var textures = ContentFinder<Texture2D>.GetAllInFolder(levelData.expressionsPath).ToList();
                    expressionCache[levelData.level] = textures;
                }
            }
            isInitialized = true;
        }

        public static void ChangeFavorability(int amount)
        {
            var manager = Find.World.GetComponent<WorldComponent_OperatorManager>();
            if (manager != null)
            {
                int oldFavor = manager.zuoYaoFavorability;
                manager.zuoYaoFavorability = Mathf.Clamp(manager.zuoYaoFavorability + amount, MinFavorability, MaxFavorability);
                // [核心修复] 调用奖励检查逻辑
                manager.CheckForNewRewards(oldFavor, manager.zuoYaoFavorability);
            }
        }

        /// <summary>
        /// 检查好感度变化是否跨过了某个奖励的阈值。
        /// </summary>
        private void CheckForNewRewards(int oldFavor, int newFavor)
        {
            foreach (var rewardDef in DefDatabase<RewardDef>.AllDefs)
            {
                // [核心修复] 只有当旧好感度低于阈值，且新好感度高于等于阈值时，才解锁
                if (oldFavor < rewardDef.requiredFavorability && newFavor >= rewardDef.requiredFavorability)
                {
                    // 确保不会重复解锁
                    if (unlockedRewardDefs.Add(rewardDef.defName))
                    {
                        Messages.Message($"与左爻的关系达到了新的阶段：【{rewardDef.label}】。现在可以在通讯台领取一份特殊的回礼。", MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
        }

        public int GetCurrentFavorabilityLevel()
        {
            if (zuoYaoFavorability >= 750) return 4; // [修复] 使用 >= 确保满值时等级正确
            if (zuoYaoFavorability >= 500) return 3;
            if (zuoYaoFavorability >= 250) return 2;
            if (zuoYaoFavorability > 0) return 1;
            return 0;
        }

        public Texture2D GetOperatorPortrait()
        {
            EnsureInitialized();
            int currentMaxLevel = GetCurrentFavorabilityLevel();
            if (activeExpressionLevel > currentMaxLevel)
            {
                activeExpressionLevel = currentMaxLevel;
            }
            if (expressionCache.TryGetValue(activeExpressionLevel, out var textures) && textures.Any())
            {
                return textures.RandomElement();
            }
            return fallbackTexture;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref activeExpressionLevel, "zuoYaoActiveExpressionLevel", 0);
            Scribe_Values.Look(ref zuoYaoFavorability, "zuoYaoFavorability", 0);
            Scribe_Collections.Look(ref unlockedRewardDefs, "unlockedRewardDefs", LookMode.Value);
            Scribe_Collections.Look(ref collectedUnderwearDefs, "collectedUnderwearDefs", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (unlockedRewardDefs == null) unlockedRewardDefs = new HashSet<string>();
                if (collectedUnderwearDefs == null) collectedUnderwearDefs = new HashSet<string>();
            }
        }
    }
}