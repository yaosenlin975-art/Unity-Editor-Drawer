# Changelog

## Unreleased

- 改名：Package Manager 产品名与包 ID 更新为 Lin Editor Drawer / `com.lin.editor-drawer`；保留既有命名空间、程序集及用户配置存储标识以兼容已有工程。
- 修复：调用点写全限定形式 `[ToolbarButton(Toolbar.Element.EAlign...)]` 时报 `CS0117: 'Toolbar' does not contain a definition for 'Element'`——`using UnityEditor.UIElements;` 带进来的公开类型 `Toolbar` 挤掉了 Lin 的 `Toolbar` 命名空间（2021.3.45 与 2022.3.62t12 实测都存在，不是版本差异）。工具栏命名空间由 `Lin.Editor.Annotation.Toolbar[.Element]` 改为 `Lin.Editor.Toolbar[.Element]`，`Lin.Editor.*` 里的调用点因此按外层命名空间解析，两个编辑器版本都通过。**破坏性变更**：外部代码需把 `using Lin.Editor.Annotation.Toolbar.Element;` 改成 `using Lin.Editor.Toolbar.Element;`。
- 修复：Project 窄分栏列表被误判为图标网格，导致注释下移到下一行。
- 调整：Project 注释绘制样式同步配置字号，统一富文本标签与样式的字号度量。
- 优化：Project 资源注释贴齐条目右侧；字号设置改为带预览的滑动条，描述标识改为可编辑列表。
- 新增：设置页「每个脚本扫描最大行数」（默认 100），脚本注释只读文件开头这么多行，避免把第三方包里的几千行脚本整份读进 Project 绘制。
- 修复：Project 窗口放大图标后，资源注释因仍按列表行计算坐标而被裁切；图标网格改在名称附近单独显示注释。
- 变更：**资源/文件夹注释不再存 `ProjectSettings/LinEditorAnnotation.json`，改存各资产 `.meta` 的 `userData`（key `lin.annotation`）**。注释随资产进版本库、随资产删除，"清除已删除资源的注释"菜单随之移除。**破坏性变更**：旧中央档不迁移、也不再读取，老注释需重新填写（或自行按 GUID 把 `items` 搬回各资产 `.meta`）。
- 变更：**包设置从 `EditorPrefs` 迁到 `ProjectSettings/LinEditorDrawer.asset`**（`AnnotationSettings` ScriptableObject，首次访问自动生成，文本序列化）。界面语言与主工具栏开关态仍留在 `EditorPrefs`。**破坏性变更**：9 个注释显示设置不做搬迁，升级后回到默认值；SO 里缺字段会被反序列化成 0/空，由 `settingsVersion` + `Migrate()` 归位。设置页每项的悬停提示改为指向该 `.asset`。
- 新增：`ImporterUserData` —— `userData` 的公开读写容器（`GetUserData<T>/SetUserData<T>/RemoveUserData`、`Get/Set/RemoveAnnotation`）。未知 key 逐字保留、读路径绝不写盘、删空最后一个 key 后整串置空。新增 `Tests/Editor`（EditMode，10 条）覆盖容器、编解码与设置默认值/迁移，含一条断言锁住与 ADR-004 冻结格式（`{"TAGS_KEY":"[\"ImporterModified\"]"}`）的字节一致性。**破坏性变更**：包自此依赖 `com.unity.nuget.newtonsoft-json`（下限 3.0.2），不再是零依赖包。

## 0.2.0 (2026-09-22)

**改名**：`com.lin.editor-toolbox` → `com.lin.editor-annotation`，命名空间 → `Lin.Editor.Annotation.*`。旧版本未被任何工程引用，无迁移成本。

- 新增：Project 窗口资源与文件夹注释，右键 **修改注释** 编辑，标题按所选颜色显示在名字右侧。
- 新增：脚本注释——从 `.cs` 源码里按可配置的标识（默认 `Description:` 等三项）提取一句说明并着色显示。
- 新增：**Project Settings → Lin Editor Annotation** 配置页，分资源注释 / 场景物体注释 / 脚本注释三段；主工具栏右侧加「注释」按钮直达。
- 新增：注释数据落在宿主工程 `ProjectSettings/LinEditorAnnotation.json`，用 `JsonUtility` 读写。
- 修复：清理已删除资源注释的菜单原本用 `File.Exists` 判断资源是否还在，而它对目录恒为 false——跑一次会把所有文件夹注释删掉。现在同时判目录。
- 变更：脚本注释现在也吃 `注释字体大小`（原先只有导入器标题吃），一个旋钮同时管两处。
- 变更：两个注释窗口共用一份 `Editor/UI/AnnotationWindow.uss`（原先两份逐字节相同）。

## 0.1.0 (2026-09-22)

首个版本，从 `Learn` 工程的 `Assets/Plugins/Lin/Editor/{Inspector,Toolbar,Hierarchy}` 移植而来。

- 新增：主工具栏挂载点，支持 `[ToolbarButton]` / `[ToolbarToggle]` 特性与 `IToolbarElement` 两种扩展方式。
- 新增：Hierarchy 行内物体注释、注释编辑窗口，与 `IHierarchyDrawable` 扩展接口。
- 新增：`CONTEXT/MonoBehaviour → 修改GameObject的名字`，配合 `[Name]` 特性使用。
- 零外部依赖：移除对 ZLinq、Cysharp.Text 及项目私有 `Lin.*` 程序集的全部引用。
- 未移植：`InitializerEditor`（绑定项目框架的运行时引导体）；`DefaultAssetInspector`（依赖对象池与 URI 编辑，后者需要 Newtonsoft.Json）。
