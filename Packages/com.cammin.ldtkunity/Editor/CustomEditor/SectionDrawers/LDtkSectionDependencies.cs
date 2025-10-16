using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LDtkUnity.Editor
{
    internal sealed class LDtkSectionDependencies : LDtkSectionDrawer
    {
        private string[] _dependencies = Array.Empty<string>();
        private Object[] _dependencyAssets = Array.Empty<Object>();
        private GUIContent[] _dependencyContent = Array.Empty<GUIContent>();
        
        protected override string GuiText => "Dependencies";
        protected override string GuiTooltip => "Dependencies that were defined since the last import.\n" +
                                                "If any of these dependencies save changes, then this asset will automatically reimport.";
        protected override Texture GuiImage => LDtkIconUtility.GetUnityIcon("UnityEditor.FindDependencies", "");
        protected override string ReferenceLink => LDtkHelpURL.SECTION_DEPENDENCIES;
        protected override bool SupportsMultipleSelection => false;
        
        public LDtkSectionDependencies(LDtkImporterEditor editor, SerializedObject serializedObject) : base(editor, serializedObject)
        {
            UpdateDependencies();
        }

        public void UpdateDependencies() //originally this was updated less regularly for performance, but was difficult to find the right event for.
        {
            if (Editor == null)
            {
                return;
            }

            if (!TryGetSerializedObject(out SerializedObject serializedObject))
            {
                return;
            }

            serializedObject.Update();
            Object target = serializedObject.targetObject;
            if (target == null)
            {
                return;
            }
            
            string importerPath = AssetDatabase.GetAssetPath(target);
            
            _dependencies = LDtkDependencyCache.Load(importerPath);
            _dependencyAssets = new Object[_dependencies.Length];
            _dependencyContent = new GUIContent[_dependencies.Length];

            for (int i = 0; i < _dependencies.Length; i++)
            {
                //can be null!
                _dependencyAssets[i] = AssetDatabase.LoadAssetAtPath<Object>(_dependencies[i]);

                if (_dependencyAssets[i] == null)
                {
                    _dependencyContent[i] = new GUIContent
                    {
                        text = _dependencies[i] + " (missing)",
                        tooltip = _dependencies[i],
                        image = null
                    };
                    continue;
                }

                _dependencyContent[i] = new GUIContent
                {
                    text = _dependencyAssets[i].name,
                    tooltip = _dependencies[i],
                    image = GetIconForDependency(_dependencyAssets[i].GetType(), _dependencies[i])
                };
            }
            
            Texture2D GetIconForDependency(Type type, string assetPath)
            {
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                
                if (importer != null && importer is LDtkTilesetImporter)
                {
                    return LDtkIconUtility.LoadTilesetFileIcon();
                }

                return AssetPreview.GetMiniTypeThumbnail(type);
            }
        }

        public override void Draw()
        {
            LDtkEditorGUIUtility.DrawDivider();
            base.Draw();
        }
        
        protected override void DrawDropdownContent()
        {
            EditorGUIUtility.SetIconSize(Vector2.one * 16f);
            using (new LDtkGUIEnabledScope(false))
            {
                for (int i = 0; i < _dependencies.Length; i++)
                {
                    EditorGUILayout.ObjectField(_dependencyContent[i], _dependencyAssets[i], typeof(Object), false);
                }
            }
        }
    }
}