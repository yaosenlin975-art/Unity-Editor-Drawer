using Lin.Runtime.Annotation;
using Lin.Editor.Annotation.Settings;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lin.Editor.Annotation.Inspector
{
    public static class ScriptInspector
    {
        [MenuItem("CONTEXT/MonoBehaviour/修改GameObject的名字")]
        private static void RenameGameObjectChinese(MenuCommand command) => RenameGameObjectInternal(command);

        [MenuItem("CONTEXT/MonoBehaviour/Rename GameObject")]
        private static void RenameGameObjectEnglish(MenuCommand command) => RenameGameObjectInternal(command);

        [MenuItem("CONTEXT/MonoBehaviour/修改GameObject的名字", true)]
        private static bool ValidateRenameGameObjectChinese(MenuCommand command) =>
            !EditorAnnotationLocalization.IsEnglish && command.context is MonoBehaviour;

        [MenuItem("CONTEXT/MonoBehaviour/Rename GameObject", true)]
        private static bool ValidateRenameGameObjectEnglish(MenuCommand command) =>
            EditorAnnotationLocalization.IsEnglish && command.context is MonoBehaviour;

        public static void RenameGameObject(MenuCommand command) => RenameGameObjectInternal(command);

        private static void RenameGameObjectInternal(MenuCommand command)
        {
            MonoBehaviour monoBehaviour = command.context as MonoBehaviour;
            if (monoBehaviour != null)
            {
                var nameAtt = monoBehaviour.GetType().GetCustomAttributes(typeof(NameAttribute), false);
                if (nameAtt?.Any() ?? false)
                    monoBehaviour.name = (nameAtt.First() as NameAttribute).Name;
                else
                    monoBehaviour.name = monoBehaviour.GetType().Name;
            }
        }
    }
}
