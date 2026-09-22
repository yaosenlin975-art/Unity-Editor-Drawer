# Lin Editor Annotation

零外部依赖的编辑器注释工具。`package.json` 的 `dependencies` 为空对象，可安装进任意 Unity / 团结工程。

## 安装

**内嵌**：把 `com.lin.editor-annotation` 整个目录放进目标工程的 `Packages/`，Unity 自动识别。

**外部引用**：`Packages/manifest.json` 里加一条

```json
"com.lin.editor-annotation": "file:<路径>/com.lin.editor-annotation"
```

或在 Package Manager 里 *Add package from git URL / disk*。

要求 Unity 2022.3 或更高（团结 1.x 亦可）。

## 包含什么

| 能力 | 出现在哪 |
|---|---|
| 资源/文件夹注释 | Project 窗口列表视图，名字右侧 |
| 脚本注释 | 同上，从 `.cs` 源码注释里提取 |
| 场景物体注释 | Hierarchy 行内 |
| 注释样式配置 | Project Settings → Lin Editor Annotation |
| 配置页快捷入口 | 主工具栏右侧「注释」按钮 |
| 按类名重命名 GameObject | 组件右键菜单 |

### 资源与文件夹注释

在 Project 窗口里选中资源或文件夹 → 右键 **修改注释**，填标题/颜色/说明。标题会以所选颜色显示在名字右侧，鼠标悬停出 tooltip。再次点击标题即可编辑（单击还是双击由配置页决定）。

注释**不写进 `.meta`**，而是集中存在宿主工程的 `ProjectSettings/LinEditorAnnotation.json`：

- 加注释不会让资源导入器变脏，不会污染版本库里的资产文件；
- 代价是这个 JSON 得跟着版本库走，多人协作时它会成为冲突热点；
- 清理已删除资源留下的条目：**Tools → Lin Editor Annotation → 清除已删除资源的注释**。

### 脚本注释

对 `.cs` 文件，会按配置页里的「描述标识」逐行提取一句说明，用脚本注释的颜色/粗体/斜体显示在标题前面，例如：

```csharp
// Description: 负责把主相机跟随逻辑收敛到这里
public class CameraFollow : MonoBehaviour { }
```

默认标识为 `Description: `、`Description：`、`功能说明: `（注意前两个分别是半角和全角冒号）。

**它是在整个文件里找第一个匹配行，并不限定在文件头**——所以任何位置出现的 `// Description: ...`、甚至字符串字面量里的该文本都可能被抓出来。标识列表在配置页一行一个，不要留空行。

### 场景物体注释

Hierarchy 里选中物体 → 右键 **修改注释**（GameObject 菜单下）。数据存在该物体上的 `Description` 组件里，随场景保存、切场景不残留；`Description` 设了 `HideFlags.DontSaveInBuild`，不进构建产物。

### 配置页

**Project Settings → Lin Editor Annotation**，三段：资源注释、场景物体注释、脚本注释。字号限定 8–32；改动会自动重绘 Project 窗口。

所有值存 `EditorPrefs`（key 前缀 `com.lin.editor-annotation/`），即**本机本用户**偏好，不进版本库、不随工程共享。

### 主工具栏

包在主工具栏右侧放了一个「注释」按钮直达配置页。同时本包对外开放工具栏挂载点，使用方加自己的按钮不需要改包内代码：

```csharp
using Lin.Editor.Annotation;
using Lin.Editor.Annotation.Toolbar.Element;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MyToolbarButtons
{
    // 第二个参数三选一：纹理资产路径、EditorGUIUtility 的内置图标名、或直接写按钮文字。
    // 内置图标名全是 ASCII，所以含中文的取值会被直接当作文字，不会去查图标（避免刷一条加载失败的警告）。
    [ToolbarButton(EAlign.Right, EVisibleMode.Editor, "新建", "追加一个空场景")]
    private static void AddEmptyScene()
        => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

    // 方法签名必须是 static void(bool)；末位 key 用于把开关状态持久化到 EditorPrefs
    [ToolbarToggle(EAlign.Left, EVisibleMode.Both, "暂停", "挂起播放", "My.Pause")]
    private static void TogglePaused(bool on) => EditorApplication.isPaused = on;
}
```

> 两个 `using` 都不能省。少了 `Lin.Editor.Annotation.Toolbar.Element`，`ToolbarButton` 会被 `using UnityEditor;` 里的同名 Unity 类型吃掉，报"'ToolbarButton' 不是特性类"。

或者实现 `IToolbarElement` 自行返回一个 `VisualElement`：

```csharp
using Lin.Editor.Annotation.Toolbar.Element;

public class MySlider : IToolbarElement
{
    public EAlign align => EAlign.Middle;
    public EVisibleMode visibleMode => EVisibleMode.Runtime;
    public float width => 200;
    public VisualElement Create() => null;
}
```

`EVisibleMode` 决定元素在编辑态（`Editor`）、播放态（`Runtime`）还是两者都显示；切换播放模式时三个对齐容器会整体重建。包自带 `TimeScaleSlider` 作为该接口的示例，播放时出现在工具栏中部。

> 元素发现靠反射扫描所有名字含 `Editor` 的程序集，这是本包对外的扩展点，收紧成只扫本包会让上面两种写法失效。某个元素构造失败只会打一条错误日志并跳过它，不中断整个工具栏。

自研 Hierarchy 行内绘制则实现 `IHierarchyDrawable`：

```csharp
using Lin.Editor.Annotation;
using UnityEngine;

public class MyDrawer : IHierarchyDrawable
{
    public int drawPriority => 10;   // 越大越先绘制
    public float DrawInHierarchy(int instanceId, Rect drawablePoint)
    {
        // 在 drawablePoint 处绘制，返回占用的宽度以便下一个绘制器左移
        return 0f;
    }
}
```

### 按类名重命名 GameObject

```csharp
using Lin.Editor.Annotation;

[Name("玩家控制器")]
public class PlayerController : MonoBehaviour { }
```

之后在该组件的右键菜单里选 **修改GameObject的名字**，物体名字改成特性里的值（未标特性则改成类名）。`NameAttribute` 位于 Runtime 程序集，因为使用方要把它标在自己的运行时脚本上。

## 程序集

| asmdef 名 | 平台 | 内容 |
|---|---|---|
| `Lin.Runtime.Annotation` | 全平台 | `Description`、`NameAttribute` |
| `Lin.Editor.Annotation` | 仅编辑器 | 其余全部 |

要在别的程序集里使用本包的类型，需要在该 asmdef 中显式引用对应 asmdef；`autoReferenced` 为 `true`，故 Assembly-CSharp 默认可见。

## 兼容性说明

代码保留了 Unity 6 的主工具栏（`MainToolbarWindow`）与 `EntityId` 分支。**该分支未在本地编译验证**——验证用的引用集来自团结 1.10.0（2022.3.62t12 基线），其中不存在 `UnityEngine.EntityId`。装进 Unity 6 工程前请自行确认这两个分支。

## 许可

未指定。
