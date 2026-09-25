using Lin.Editor.Annotation.Asset;
using NUnit.Framework;

namespace Lin.Editor.Annotation.Tests
{
    /// <summary>
    /// userData 容器的字符串变换。只打 string→string 的纯函数：不需要 Newtonsoft 类型、
    /// 不建资产 fixture、不跑导入（测试程序集拿不到预编译 dll 的自动引用，见 ADR-002 补充）。
    /// </summary>
    public class ImporterUserDataTests
    {
        // Learn 的真实存量形状，取自 Learn/Assets/Arts/Animations/Mixamo/*.fbx.meta
        private const string ForeignTags = "{\"TAGS_KEY\":\"[\\\"ImporterModified\\\"]\"}";

        [Test]
        public void C1_NativeValueRoundTrips()
        {
            var json = "{\"title\":\"batch-runtime\",\"titleColor\":\"44AAFF\"}";

            var written = ImporterUserData.SetNativeKey(string.Empty, ImporterUserData.AnnotationKey, json);

            // 逐字节断言：证明无空白（Formatting.None）与 key 顺序，这是 .meta 里可 diff 的前提
            Assert.AreEqual("{\"lin.annotation\":" + json + "}", written);
            Assert.AreEqual(json, ImporterUserData.TryGetValue(written, ImporterUserData.AnnotationKey));
        }

        [Test]
        public void C2_KeepsForeignKeysByteIdentical()
        {
            var written = ImporterUserData.SetNativeKey(ForeignTags, "other", "{\"n\":1}");

            Assert.AreEqual("{\"TAGS_KEY\":\"[\\\"ImporterModified\\\"]\",\"other\":{\"n\":1}}", written);
            // 删掉自己的 key 之后，别人的内容必须逐字回到原样
            Assert.AreEqual(ForeignTags, ImporterUserData.RemoveKey(written, "other"));
        }

        [Test]
        public void C2b_NonAsciiSurvivesRoundTrip()
        {
            // 中文只断言往返，不断言字节：非 ASCII 是否转义由序列化器决定，不是本容器的契约
            var written = ImporterUserData.SetNativeKey(string.Empty, ImporterUserData.AnnotationKey,
                "{\"title\":\"运行时合批\"}");

            Assert.AreEqual("{\"title\":\"运行时合批\"}",
                ImporterUserData.TryGetValue(written, ImporterUserData.AnnotationKey));
        }

        [Test]
        public void C3_ToleratesEmptyAndNonJsonInput()
        {
            Assert.IsNull(ImporterUserData.TryGetValue(null, "k"));
            Assert.IsNull(ImporterUserData.TryGetValue(string.Empty, "k"));
            Assert.IsNull(ImporterUserData.TryGetValue("hello", "k"));
            Assert.IsNull(ImporterUserData.TryGetValue("{\"k\":1}", "missing"));
            // 非 JSON 时按空档处理：不抛，且自己的 key 写进去了
            Assert.AreEqual("{\"k\":1}", ImporterUserData.SetNativeKey("hello", "k", "1"));
        }

        [Test]
        public void C4_LastKeyGoneYieldsEmptyString()
        {
            Assert.AreEqual(string.Empty, ImporterUserData.RemoveKey("{\"k\":1}", "k"));
            Assert.AreEqual(string.Empty, ImporterUserData.RemoveKey(string.Empty, "k"));
            // 空值等价于删除，不留 "k":null 之类的空壳
            Assert.AreEqual(string.Empty, ImporterUserData.SetNativeKey("{\"k\":1}", "k", null));
            Assert.AreEqual(string.Empty, ImporterUserData.SetNativeKey("{\"k\":1}", "k", string.Empty));
            Assert.AreEqual(string.Empty, ImporterUserData.SetStringKey("{\"k\":1}", "k", null));
        }

        [Test]
        public void C5_Adr004FrozenTagWireFormat()
        {
            // 双层编码：值本身是序列化好的字符串，作为字符串标量塞进去
            var written = ImporterUserData.SetStringKey(string.Empty, "TAGS_KEY", "[\"ImporterModified\"]");

            Assert.AreEqual(ForeignTags, written);
            // 读的方向对称：字符串标量要解包，才能再交给反序列化
            Assert.AreEqual("[\"ImporterModified\"]", ImporterUserData.TryGetValue(ForeignTags, "TAGS_KEY"));
        }

        [Test]
        public void C6_AllFiveAnnotationFieldsSurvive()
        {
            var summary = new AssetSummary
            {
                title = "batch-runtime",
                titleColor = "44AAFF",
                description = "note",
                createTime = "2026-09-25T00:00:00",
                updateTime = "2026-09-25T01:00:00",
            };

            // AssetSummary 全靠 public 字段：少序列化一个字段的表现是"保存成功但下次打开是空的"
            Assert.AreEqual(
                "{\"title\":\"batch-runtime\",\"titleColor\":\"44AAFF\",\"description\":\"note\"," +
                "\"createTime\":\"2026-09-25T00:00:00\",\"updateTime\":\"2026-09-25T01:00:00\"}",
                ImporterUserData.AnnotationToJson(summary));
        }

        [Test]
        public void C7_MissingFieldsAndJunkDegradeToDefault()
        {
            var back = ImporterUserData.AnnotationFromJson("{\"title\":\"t\"}");
            Assert.AreEqual("t", back.title);
            Assert.IsNull(back.updateTime);
            Assert.AreEqual(default(AssetSummary), ImporterUserData.AnnotationFromJson("hello"));
            Assert.AreEqual(default(AssetSummary), ImporterUserData.AnnotationFromJson(null));
        }
    }
}
