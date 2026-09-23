# Changelog

## Unreleased

- 修复：Project 窗口放大图标后，资源注释因仍按列表行计算坐标而被裁切；图标网格改在名称附近单独显示注释。

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
