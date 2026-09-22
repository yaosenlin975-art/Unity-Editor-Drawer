using UnityEngine.UIElements;

namespace Lin.Editor.Annotation.Toolbar.Element
{
    public interface IToolbarElement
    {
        EAlign align { get; }
        EVisibleMode visibleMode { get; }
        float width { get; }
        VisualElement Create();
    }
}
