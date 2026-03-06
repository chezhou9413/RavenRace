using System;
using System.Reflection;
using Verse;
using HarmonyLib;

namespace RavenRace.Features.Hypnosis
{
    /// <summary>
    /// 处理与 RimTalk 的软兼容反射逻辑。
    /// 确保即使没有 RimTalk，Mod 也能正常运行。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class RimTalkCompat
    {
        private static bool isRimTalkActive = false;
        private static MethodInfo cacheGetMethod;
        private static MethodInfo addTalkRequestMethod;
        private static Type talkTypeEnum;

        static RimTalkCompat()
        {
            try
            {
                if (ModsConfig.IsActive("cj.rimtalk"))
                {
                    // 1. 获取 RimTalk.Source.Data.Cache 类型
                    Type cacheType = AccessTools.TypeByName("RimTalk.Source.Data.Cache");
                    if (cacheType != null)
                    {
                        // 2. 获取 Cache.Get(Pawn) 静态方法
                        cacheGetMethod = AccessTools.Method(cacheType, "Get", new Type[] { typeof(Pawn) });
                    }

                    // 3. 获取 RimTalk.Data.PawnState 类型
                    Type pawnStateType = AccessTools.TypeByName("RimTalk.Data.PawnState");
                    if (pawnStateType != null)
                    {
                        // 4. 获取 AddTalkRequest 方法
                        // 签名推测: public void AddTalkRequest(string text, Pawn target, TalkType type)
                        // 注意：TalkType 是一个枚举，我们需要获取这个枚举类型
                        talkTypeEnum = AccessTools.TypeByName("RimTalk.Data.TalkType");

                        if (talkTypeEnum != null)
                        {
                            addTalkRequestMethod = AccessTools.Method(pawnStateType, "AddTalkRequest",
                                new Type[] { typeof(string), typeof(Pawn), talkTypeEnum });
                        }
                    }

                    if (cacheGetMethod != null && addTalkRequestMethod != null)
                    {
                        isRimTalkActive = true;
                        Log.Message("[RavenRace] RimTalk integration initialized successfully for Hypnosis App.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RavenRace] Failed to initialize RimTalk compatibility: {ex}");
                isRimTalkActive = false;
            }
        }

        /// <summary>
        /// 尝试让目标 Pawn 说出一句话（通过 RimTalk）。
        /// </summary>
        /// <param name="speaker">说话者</param>
        /// <param name="text">内容</param>
        public static void TryAddTalkRequest(Pawn speaker, string text)
        {
            if (!isRimTalkActive || speaker == null || speaker.Dead) return;

            try
            {
                // 1. 调用 Cache.Get(speaker) 获取 PawnState 实例
                object pawnState = cacheGetMethod.Invoke(null, new object[] { speaker });

                if (pawnState != null)
                {
                    // 2. 构造 TalkType.Other 枚举值 (通常 Other 是通用类型)
                    // 假设 TalkType 枚举里有 "Other"
                    object talkTypeVal = Enum.Parse(talkTypeEnum, "Other");

                    // 3. 调用 AddTalkRequest(text, null, TalkType.Other)
                    addTalkRequestMethod.Invoke(pawnState, new object[] { text, null, talkTypeVal });
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RavenRace] Error invoking RimTalk: {ex.Message}");
            }
        }
    }
}