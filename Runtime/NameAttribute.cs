namespace Lin.Runtime.Annotation
{
    /// <summary>
    /// 标注在 MonoBehaviour 类上，供 ScriptInspector 的右键菜单快速把 GameObject 名字改成该类名。
    /// 必须在 Runtime 程序集：使用方要把它标在自己的运行时脚本上。
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class NameAttribute : System.Attribute
    {
        public NameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
