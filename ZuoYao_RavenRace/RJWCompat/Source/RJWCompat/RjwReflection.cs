// File: RJWCompat/Source/RJWCompat/RjwReflection.cs
using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace RavenRace.RJWCompat
{
    /// <summary>
    /// 存储通过反射获取的RJW类型、方法和字段的缓存。
    /// 仅保留月经补丁所需的部分。
    /// </summary>
    public static class RjwReflection
    {
        // Types
        public static Type HediffComp_Menstruation_Type { get; private set; }
        public static Type Egg_Type { get; private set; }

        // Fields
        public static FieldInfo Menstruation_eggs_Field { get; private set; }
        public static FieldInfo Egg_fertilized_Field { get; private set; }
        public static FieldInfo Egg_fertilizer_Field { get; private set; }

        public static bool IsInitialized { get; private set; } = false;

        public static void Initialize()
        {
            if (IsInitialized) return;

            Log.Message("[RavenRace RJWCompat] RjwReflection.Initialize() called.");

            // 寻找关键类型
            HediffComp_Menstruation_Type = AccessTools.TypeByName("RJW_Menstruation.HediffComp_Menstruation");

            if (HediffComp_Menstruation_Type == null)
            {
                Log.Error("[RavenRace RJWCompat] FATAL: RJW_Menstruation.HediffComp_Menstruation type not found. Cannot initialize reflection cache for Menstruation patch.");
                return;
            }

            // 寻找嵌套类型和字段
            Egg_Type = AccessTools.Inner(HediffComp_Menstruation_Type, "Egg");
            if (Egg_Type == null)
            {
                Log.Error("[RavenRace RJWCompat] FATAL: 'Egg' nested type not found in HediffComp_Menstruation.");
                return;
            }

            Menstruation_eggs_Field = AccessTools.Field(HediffComp_Menstruation_Type, "eggs");
            Egg_fertilized_Field = AccessTools.Field(Egg_Type, "fertilized");
            Egg_fertilizer_Field = AccessTools.Field(Egg_Type, "fertilizer");

            // 验证字段是否找到
            if (Menstruation_eggs_Field == null || Egg_fertilized_Field == null || Egg_fertilizer_Field == null)
            {
                Log.Error("[RavenRace RJWCompat] FATAL: One or more fields required for menstruation patch were not found via reflection.");
                return;
            }

            IsInitialized = true;
            Log.Message("[RavenRace RJWCompat] Reflection cache for RJW types initialized successfully.");
        }
    }
}