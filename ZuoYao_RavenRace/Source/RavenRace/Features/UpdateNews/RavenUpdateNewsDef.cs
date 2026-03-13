using System;
using Verse;

namespace RavenRace.Features.UpdateNews
{
    /// <summary>
    /// 更新日志的数据结构定义。
    /// 允许通过纯 XML 添加新的更新日志，彻底解耦 C# 代码。
    /// </summary>
    public class RavenUpdateNewsDef : Def, IComparable<RavenUpdateNewsDef>
    {
        public string version = "1.0.0";
        public string publishDate = "未知日期";
        public string title = "更新日志";
        public string bannerPath = "";

        [MustTranslate]
        public string content = "更新内容...";

        // 缓存解析后的版本号，用于精确排序
        private Version parsedVersion;

        public Version GetParsedVersion()
        {
            if (parsedVersion == null)
            {
                if (!Version.TryParse(version, out parsedVersion))
                {
                    parsedVersion = new Version(0, 0, 0, 0);
                }
            }
            return parsedVersion;
        }

        // 实现比较接口，让日志自动按版本号从新到旧排序
        public int CompareTo(RavenUpdateNewsDef other)
        {
            if (other == null) return 1;
            return other.GetParsedVersion().CompareTo(this.GetParsedVersion());
        }
    }
}