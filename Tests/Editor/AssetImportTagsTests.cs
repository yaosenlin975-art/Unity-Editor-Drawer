using Lin.Editor.AssetImport;
using NUnit.Framework;

namespace Lin.Editor.Annotation.Tests
{
    /// <summary>
    /// tag 那条线的字符串变换。容器是 ImporterUserData（同一个 .meta 上还住着 lin.annotation），
    /// 所以这里只打 string→string 的纯函数：不建资产 fixture、不跑导入、也不需要 Newtonsoft 类型
    /// （测试程序集拿不到预编译 dll 的自动引用，见 ADR-002 补充）。
    /// 断言一律钉完整字节：单参 string.Contains 就是 IndexOf>=0 的别名，
    /// 写成 A || A 是恒真的废断言。
    /// </summary>
    public class AssetImportTagsTests
    {
        private const string Modified = "ImporterModified";
        // Learn 里 174 个带 TAGS_KEY 的 .meta 实测是**两种**形状（yaml.safe_load + json.loads 逐档解，不是 grep 数行）：
        //   96 个单行紧凑 —— 就是下面这条 LearnShape，ADR-004 冻结的是它的字节
        //   78 个多行美化（真 CRLF + 两个空格缩进），且同档还挂着 legacy key TAG_KEY（单数）—— 见 LearnMultiLineShape
        // "174 个都是这个样"是错的：grep 数出来的 95/73 把转义与 YAML 折行都算错了，别拿它当分布。
        private const string LearnShape = "{\"TAGS_KEY\":\"[\\\"ImporterModified\\\"]\"}";
        // 别家写成单层数组（Learn 的 GetUserData<T> 用 token.ToString() 再反序列化，效果同上）也要认
        private const string SingleLayerShape = "{\"TAGS_KEY\":[\"ImporterModified\"]}";
        // 那 78 个档里解出来的 userData 字符串本体（TAG_KEY 本层从不读写，但它必须逐字节活着）
        private const string LearnMultiLineShape =
            "{\r\n  \"TAG_KEY\": \"[\\\"ImporterModified\\\"]\",\r\n  \"TAGS_KEY\": \"[\\\"ImporterModified\\\"]\"\r\n}";
        // 写回去的完整紧凑字节：\r\n 与缩进被 Formatting.None 吃掉，两个 key 一个不少、顺序不变
        private const string LearnMultiLineShapeCompact =
            "{\"TAG_KEY\":\"[\\\"ImporterModified\\\"]\",\"TAGS_KEY\":\"[\\\"ImporterModified\\\"]\"}";

        [Test]
        public void T1_AddThenRemove_RoundTrips()
        {
            var written = ImporterTags.Add(string.Empty, Modified);
            Assert.IsTrue(ImporterTags.Contains(written, Modified), "Add 后应含该 tag");
            Assert.IsFalse(ImporterTags.Contains(written, "Other"), "不应凭空含别的 tag");

            var removed = ImporterTags.Remove(written, Modified);
            Assert.IsFalse(ImporterTags.Contains(removed, Modified));
            Assert.AreEqual(string.Empty, removed, "最后一个 key 删空后整串应为空串");
        }

        [Test]
        public void T2_OutputMatchesFrozenWireFormat()
        {
            Assert.AreEqual(LearnShape, ImporterTags.Add(string.Empty, Modified));
        }

        [Test]
        public void T2b_ReadsLearnShapeAsTagged()
        {
            Assert.IsTrue(ImporterTags.Contains(LearnShape, Modified),
                "读不出 Learn 存量形状会导致整库重压缩");
        }

        [Test]
        public void T3_ToleratesForeignAndNonJsonUserData()
        {
            // 未知 key 必须**逐字节**原样保留：钉死合并后的完整字节
            var withForeign = ImporterTags.Add("{\"other\":\"keep\"}", Modified);
            Assert.AreEqual("{\"other\":\"keep\",\"TAGS_KEY\":\"[\\\"ImporterModified\\\"]\"}", withForeign);
            Assert.IsTrue(ImporterTags.Contains(withForeign, Modified));

            // 非 JSON 不能崩，且按无 tag 处理后写回自己的内容
            Assert.IsFalse(ImporterTags.Contains("hello", Modified));
            Assert.AreEqual(LearnShape, ImporterTags.Add("hello", Modified));

            // 单层数组形状也认
            Assert.IsTrue(ImporterTags.Contains(SingleLayerShape, Modified));

            // 空输入
            Assert.IsFalse(ImporterTags.Contains(null, Modified));
            Assert.IsFalse(ImporterTags.Contains(string.Empty, Modified));
        }

        [Test]
        public void T3b_AddTwiceIsIdempotent()
        {
            var once = ImporterTags.Add(string.Empty, Modified);
            Assert.AreEqual(once, ImporterTags.Add(once, Modified));
        }

        [Test]
        public void T4_PresentButUnreadableTagsKeyIsDetectable()
        {
            // "没有 TAGS_KEY"与"有 TAGS_KEY 却读不出 List<string>"必须可区分：后者若静默当无标记，
            // 写路径会把别人的值覆盖掉（WarnIfTagsKeyUnreadable 据此告警，assetPath 只有那一层知道；
            // 它现在挂在 AddUserTag/RemoveUserTag"算出来确实要写"之后、赋值之前，另一半"整串不是 JSON"见 T7）。
            Assert.IsTrue(ImporterTags.HasUnreadableTagsKey("{\"TAGS_KEY\":\"ImporterModified\"}"));
            Assert.IsTrue(ImporterTags.HasUnreadableTagsKey("{\"TAGS_KEY\":5}"));
            Assert.IsTrue(ImporterTags.HasUnreadableTagsKey("{\"TAGS_KEY\":\"null\"}"));

            // 没有 key / 两种可读形状 / 非 JSON / 空输入都不算
            Assert.IsFalse(ImporterTags.HasUnreadableTagsKey("{\"other\":\"keep\"}"));
            Assert.IsFalse(ImporterTags.HasUnreadableTagsKey(LearnShape));
            Assert.IsFalse(ImporterTags.HasUnreadableTagsKey(SingleLayerShape));
            Assert.IsFalse(ImporterTags.HasUnreadableTagsKey("hello"));
            Assert.IsFalse(ImporterTags.HasUnreadableTagsKey(null));
        }

        [Test]
        public void T5_CoexistsWithAnnotationKeyByteForByte()
        {
            // 跨容器共存：tag 写进同一个 userData 时，主人（lin.annotation）那段字节一个字符都不能变
            const string both =
                "{\"lin.annotation\":{\"title\":\"x\"},\"TAGS_KEY\":\"[\\\"ImporterModified\\\"]\"}";

            var added = ImporterTags.Add(both, "SecondTag");
            Assert.AreEqual(
                "{\"lin.annotation\":{\"title\":\"x\"}," +
                "\"TAGS_KEY\":\"[\\\"ImporterModified\\\",\\\"SecondTag\\\"]\"}", added);
            Assert.IsTrue(ImporterTags.Contains(added, Modified), "原有的 tag 不能被第二个挤掉");

            // 删到只剩一个：外层仍是双层编码的字符串标量，annotation 段照原样
            var oneLeft = ImporterTags.Remove(added, Modified);
            Assert.AreEqual(
                "{\"lin.annotation\":{\"title\":\"x\"},\"TAGS_KEY\":\"[\\\"SecondTag\\\"]\"}", oneLeft);

            // tag 全删空 = 摘掉自己的 key，主人的记录必须还在，且整串不能塌成空串
            Assert.AreEqual("{\"lin.annotation\":{\"title\":\"x\"}}", ImporterTags.Remove(oneLeft, "SecondTag"));
        }

        [Test]
        public void T6_MultiLineLearnShapeIsReadAndRewrittenCompact()
        {
            // Learn 174 个里 78 个是这一档（多行 + legacy TAG_KEY）。喂不进去就等于这条真实形状从未被跑过。
            Assert.IsTrue(ImporterTags.Contains(LearnMultiLineShape, Modified),
                "读不出多行形状会让这 78 个资产被当成未标记，进而重导一遍");

            // 写：完整期望字节。空白被规范化成紧凑形，legacy TAG_KEY 必须原样在里面
            Assert.AreEqual(LearnMultiLineShapeCompact, ImporterTags.Add(LearnMultiLineShape, Modified));
            Assert.AreEqual(
                "{\"TAG_KEY\":\"[\\\"ImporterModified\\\"]\"," +
                "\"TAGS_KEY\":\"[\\\"ImporterModified\\\",\\\"SecondTag\\\"]\"}",
                ImporterTags.Add(LearnMultiLineShape, "SecondTag"));

            // 删：只摘自己的 key，legacy 那段还在，整串不塌成空串
            Assert.AreEqual("{\"TAG_KEY\":\"[\\\"ImporterModified\\\"]\"}",
                ImporterTags.Remove(LearnMultiLineShape, Modified));
        }

        [Test]
        public void T7_GuardPredicateDistinguishesTheTwoLossShapes()
        {
            // 守卫（WarnIfTagsKeyUnreadable）的两条分支各自的可判定部分。多行合法 JSON 不该报，
            // 报了就是把 78 个正常档当成事故；非 JSON 才是真会被整体覆盖的那种。
            Assert.IsTrue(ImporterTags.HasNonJsonUserData("hello"));
            Assert.IsTrue(ImporterTags.HasNonJsonUserData("{\"a\":1"));
            Assert.IsTrue(ImporterTags.HasNonJsonUserData("[\"ImporterModified\"]"), "顶层数组也接不进对象里");

            Assert.IsFalse(ImporterTags.HasNonJsonUserData(null));
            Assert.IsFalse(ImporterTags.HasNonJsonUserData(string.Empty));
            Assert.IsFalse(ImporterTags.HasNonJsonUserData(LearnShape));
            Assert.IsFalse(ImporterTags.HasNonJsonUserData(LearnMultiLineShape));
            Assert.IsFalse(ImporterTags.HasNonJsonUserData("{\"lin.annotation\":{\"title\":\"x\"}}"));

            // 两条分支互不重叠：同一条档只该由前一条报（HasUnreadableTagsKey 为真时不再看第二条）
            Assert.IsTrue(ImporterTags.HasUnreadableTagsKey("{\"TAGS_KEY\":\"ImporterModified\"}"));
            Assert.IsFalse(ImporterTags.HasNonJsonUserData("{\"TAGS_KEY\":\"ImporterModified\"}"));
        }
    }
}
