[简体中文](README.md) | [English](README_EN.md)

# Lin Editor Drawer

A lightweight toolkit for Unity workflows across the Project window, Hierarchy, Scene View, Inspector, and main toolbar. It has no external package dependencies. The settings page and package-owned UI support Chinese and English.

> Package Manager display name: **Lin Editor Drawer**. The package ID and folder are `com.lin.editor-drawer`.

## Installation

**Embedded package:** place the entire `com.lin.editor-drawer` folder under the target project's `Packages/` directory. Unity will detect it automatically.

**Local package reference:** add this entry to `Packages/manifest.json`:

```json
"com.lin.editor-drawer": "file:<path>/com.lin.editor-drawer"
```

You can also use **Package Manager → Add package from git URL / disk**.

The minimum supported Unity version is 2021.3. The package was compiled in Unity 2021.3.45f2c1. Unity 6 toolbar and `EntityId` branches have not been compiled locally; see [Compatibility](#compatibility). Validate Tuanjie 1.x against the specific target version.

## Feature overview

| Feature | Includes |
|---|---|
| Project annotations | Titles, colors, descriptions, and tooltips for assets and folders; script descriptions extracted from configured `.cs` comment markers |
| Hierarchy tools | Scene object annotations and the `IHierarchyDrawable` extension for inline row drawing |
| Main toolbar extensions | `[ToolbarButton]`, `[ToolbarToggle]`, `IToolbarElement`, a built-in settings shortcut, and a play-mode TimeScale slider |
| Scene View attributes | `ShowInSceneGUI` fields, properties, and method buttons; `DrawInSceneGUI` custom drawing callbacks |
| Inspector shortcut | `[Name]` supplies a target GameObject name for the component context-menu command |
| Settings and localization | Annotation styles, script markers, click behavior, and Chinese/English switching through Project Settings and package menus |

### Asset and folder annotations

In the Project window, select an asset or folder, right-click, and choose **Edit Annotation** (or **修改注释**). Enter a title, color, and description. In list view, the colored title appears to the right of the asset name and is aligned to the item's right edge; in icon grid view, it appears right-aligned on a separate line above the name. Hover to read the description. Click the annotation to edit it; choose single- or double-click behavior in the settings page.

Annotations are **not stored in `.meta` files**. They are saved in the host project at `ProjectSettings/LinEditorAnnotation.json`:

- Adding an annotation does not dirty the asset importer or modify asset files.
- The JSON file must be versioned with the project. It can become a merge-conflict hotspot in team workflows.
- Remove entries for deleted assets from **Lin → Editor Drawer → Clear Deleted Asset Annotations** (or **清除已删除资源的注释**).

### Script annotations

For each configured marker, the package extracts the first matching line in a `.cs` file and displays it before the title, using the configured color, bold, and italic settings. If multiple markers match, each can contribute a description. For example:

```csharp
// Description: Keeps the main camera follow logic in one place
public class CameraFollow : MonoBehaviour { }
```

The default markers are `Description: `, `Description：`, and `功能说明: ` (the first two use half-width and full-width colons respectively).

The search reads only the first **Max scanned lines per script** lines (settings page, default 100) and uses the first matching line; it is not limited to the comment block. A matching string inside a string literal can also be picked up, while a marker placed below the configured line limit is missed. Configure one marker per line on the settings page, without blank lines.

### Scene object annotations

Select an object in the Hierarchy, right-click, and choose **Edit Annotation** (or **修改注释**) from the GameObject menu. Data is stored in a `Description` component on the object and is saved with the scene. The component uses `HideFlags.DontSaveInBuild | HideFlags.HideInInspector`, so it is hidden in the Inspector and stripped from builds.

### Scene View attributes

Use `ShowInSceneGUI` and `DrawInSceneGUI` on static members or members of `MonoBehaviour` components on the selected object and its children:

```csharp
using Lin.Runtime.Attribute;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneViewHints : MonoBehaviour
{
    [ShowInSceneGUI]
    [SerializeField] private Vector3 point;

    [ShowInSceneGUI]
    private void ResetPoint() => point = transform.position;

    [DrawInSceneGUI]
    private void DrawHint()
    {
#if UNITY_EDITOR
        Handles.Label(transform.position, point.ToString());
#endif
    }
}
```

`ShowInSceneGUI` supports fields, properties, and parameterless methods. Fields and properties display their current values; methods appear as buttons. Values are read on every Scene View GUI event, so property getters should be fast and side-effect free. `DrawInSceneGUI` must mark a parameterless method and runs for each Scene View GUI event (`void` is recommended). The package preserves IMGUI and Handles layout and interaction events, so keep these callbacks lightweight. Instance members come from the selected object and its children, including inactive objects; static members are shown globally. The static section label follows the package language.

### Settings page

Open **Project Settings → Lin Editor Drawer**. The page contains asset annotation, scene object annotation, and script annotation settings. Font size is limited to 8–32; changes refresh the Project window automatically.

Click **English** or **中文** at the top right to switch languages. The settings, annotation windows, built-in toolbar text, and package menu commands update immediately. The language choice is saved in local `EditorPrefs` and remains after Unity restarts. Native Unity menus retain both language entries: the active language is enabled and the other is disabled. Open annotation windows update their labels without changing the annotation text being edited.

Settings are stored in `EditorPrefs` under the `com.lin.editor-annotation/` key prefix. These preferences are local to the current user and machine; they are not stored in the project or shared with teammates.

Toolbar buttons registered by consumers through `ToolbarButtonAttribute` or other extension APIs keep the text supplied by the consumer; the package does not translate custom extension labels.

### Main toolbar extensions

The package adds a **Note** button on the right side of the main toolbar to open the settings page. It also exposes extension points so consumers can add buttons without changing the package:

```csharp
using Lin.Editor.Annotation;
using Lin.Editor.Annotation.Toolbar.Element;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MyToolbarButtons
{
    // Replace the icon path with an existing texture asset path.
    [ToolbarButton(EAlign.Right, EVisibleMode.Editor, "Assets/Editor/Icons/NewScene.png", "Add an empty scene")]
    private static void AddEmptyScene()
        => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

    // Signature must be static void(bool). The final key persists the toggle state in EditorPrefs.
    [ToolbarToggle(EAlign.Left, EVisibleMode.Both, "Assets/Editor/Icons/Pause.png", "Pause playback", "My.Pause")]
    private static void TogglePaused(bool on) => EditorApplication.isPaused = on;
}
```

Both `using` directives are required. Without `Lin.Editor.Annotation.Toolbar.Element`, Unity's `ToolbarButton` type from `UnityEditor` takes precedence and the compiler reports that `ToolbarButton` is not an attribute class. The third attribute argument can also be a built-in icon name or button text. ASCII strings are treated as icon names; for a literal label, use non-ASCII text or a valid texture path.

Alternatively, implement `IToolbarElement` and return a `VisualElement`:

```csharp
using UnityEngine.UIElements;
using Lin.Editor.Annotation.Toolbar.Element;

public class MySlider : IToolbarElement
{
    public EAlign align => EAlign.Middle;
    public EVisibleMode visibleMode => EVisibleMode.Runtime;
    public float width => 200;
    public VisualElement Create() => null;
}
```

`EVisibleMode` controls whether an element is shown in Edit mode (`Editor`), Play mode (`Runtime`), or both. The package rebuilds the three alignment containers when the play mode changes. `TimeScaleSlider` is included as an example and appears in the middle while playing.

Elements are discovered by reflection across assemblies whose names contain `Editor`; this is the extension point used by the examples above. An element whose constructor fails is skipped with an error log, without interrupting the rest of the toolbar.

Consumers can also implement `IHierarchyDrawable` to draw custom content inline in the Hierarchy:

```csharp
using Lin.Editor.Annotation;
using UnityEngine;

public class MyDrawer : IHierarchyDrawable
{
    public int drawPriority => 10;   // Higher values draw first.
    public float DrawInHierarchy(int instanceId, Rect drawablePoint)
    {
        // Draw at drawablePoint and return the occupied width.
        return 0f;
    }
}
```

### Rename a GameObject by class name

```csharp
using Lin.Editor.Annotation;

[Name("Player Controller")]
public class PlayerController : MonoBehaviour { }
```

Choose **Rename GameObject** (or **修改GameObject的名字**) from that component's Inspector context menu. The object is renamed to the value in `NameAttribute`; without the attribute, its class name is used. `NameAttribute` is in the Runtime assembly so it can be applied to consumer runtime scripts.

## Assemblies

| Assembly definition | Platforms | Contents |
|---|---|---|
| `Lin.Runtime.Annotation` | All | `Description`, `NameAttribute`, `ShowInSceneGUIAttribute`, `DrawInSceneGUIAttribute` |
| `Lin.Editor.Annotation` | Editor only | Scene View attribute drawer and other editor functionality |

To use package types from another assembly, add an explicit reference to the corresponding asmdef. `autoReferenced` is `true`, so the default `Assembly-CSharp` assembly can see the types.

The two Scene View attributes keep the `Lin.Runtime.Attribute` namespace, but are now provided by the `Lin.Runtime.Annotation` assembly. Custom asmdefs that previously referenced the framework's `Lin.Runtime` assembly must add a reference to this package Runtime asmdef.

## Rename compatibility

The package ID changed from `com.lin.editor-annotation` to `com.lin.editor-drawer`; projects using the old ID in a manifest or local package path must switch to the new ID. To preserve existing code and data, the C# namespaces `Lin.Editor.Annotation.*`, assembly names `Lin.Editor.Annotation` / `Lin.Runtime.Annotation`, the `com.lin.editor-annotation/` EditorPrefs prefix, and the annotation data file `ProjectSettings/LinEditorAnnotation.json` remain unchanged. No migration is needed for these identifiers.

## Unity compatibility

The package declares Unity 2021.3 as its minimum version and has been compiled in Unity 2021.3.45f2c1. Conditional code for Unity 6's main toolbar (`MainToolbarWindow`) and `EntityId` has not been compiled locally. Confirm those branches in a Unity 6 project before relying on them. Tuanjie 1.x should be validated against the target installation.

## License

Not specified.
