using System;
using UnityEditor;
using UnityEngine;

namespace LDtkUnity.Editor
{
    /// <summary>
    /// Reminder: Responsibility is just for drawing the Header content and and other unique functionality. All of the numerous content is handled in the Reference Drawers
    /// </summary>
    internal abstract class LDtkSectionDrawer : ILDtkSectionDrawer
    {
        private SerializedObject _serializedObject;
        protected LDtkImporterEditor Editor { get; }
        private bool _dropdown;

        protected abstract string GuiText { get; }
        protected abstract string GuiTooltip { get; }
        protected abstract Texture GuiImage { get; }
        protected abstract string ReferenceLink { get; }
        
        public bool HasResizedArrayPropThisUpdate { get; protected set; } = false;

        protected LDtkProjectImporter ProjectImporter
        {
            get
            {
                return TryGetSerializedObject(out SerializedObject serializedObject)
                    ? serializedObject.targetObject as LDtkProjectImporter
                    : null;
            }
        }

        public virtual bool HasProblem => false;
        protected virtual bool SupportsMultipleSelection => false; 
        

        protected LDtkSectionDrawer(LDtkImporterEditor editor, SerializedObject serializedObject)
        {
            Editor = editor;
            _serializedObject = serializedObject;
        }

        protected SerializedObject SerializedObject => GetValidSerializedObject();

        protected bool TryGetSerializedObject(out SerializedObject serializedObject)
        {
            serializedObject = GetValidSerializedObject();
            return serializedObject != null;
        }

        private SerializedObject GetValidSerializedObject()
        {
            if (!IsSerializedObjectValid(_serializedObject))
            {
                _serializedObject = null;
            }

            if (_serializedObject == null && Editor != null)
            {
                SerializedObject editorSerializedObject = Editor.serializedObject;
                if (IsSerializedObjectValid(editorSerializedObject))
                {
                    _serializedObject = editorSerializedObject;
                }
            }

            return IsSerializedObjectValid(_serializedObject) ? _serializedObject : null;
        }

        private static bool IsSerializedObjectValid(SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return false;
            }

            try
            {
                return serializedObject.targetObject != null;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        public virtual void Init()
        {
            _dropdown = EditorPrefs.GetBool(GetType().Name, true);
        }
        
        public virtual void Dispose()
        {
            EditorPrefs.SetBool(GetType().Name, _dropdown);
        }
        
        public virtual void Draw()
        {
            if (!TryGetSerializedObject(out SerializedObject serializedObject))
            {
                return;
            }

            DrawFoldoutArea();

            //don't process any data or resize arrays when we have multi-selections; references will break because of how dynamic the arrays can be.
            if (serializedObject.isEditingMultipleObjects && !SupportsMultipleSelection)
            {
                EditorGUILayout.HelpBox($"Multi-object editing not supported for {GuiText}.", MessageType.None);
                return;
            }
            
            if (CanDrawDropdown())
            {
                DrawDropdownContent();
            }
        }
        protected virtual bool CanDrawDropdown()
        {
            return _dropdown;
        }

        private static void DrawSectionProblem(Rect controlRect)
        {
            float dimension = controlRect.height - 2;
            Rect errorArea = new Rect(controlRect)
            {
                x = controlRect.x + EditorGUIUtility.labelWidth - dimension + 1,
                y = controlRect.y +1,
                width = dimension,
                height = dimension
            };

            GUIContent tooltipContent = new GUIContent()
            {
                tooltip = "Expand this section to view the error"
            };
            
            GUI.Label(errorArea, tooltipContent);
            GUI.DrawTexture(errorArea, EditorGUIUtility.IconContent("console.warnicon.sml").image);
        }

        protected void DrawFoldoutArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawFoldout();
                if (!string.IsNullOrEmpty(ReferenceLink))
                {
                    LDtkEditorGUI.DrawHelpIcon(ReferenceLink, GuiText);
                }
            }
        }

        private void DrawFoldout()
        {
            GUIContent content = new GUIContent()
            {
                text = GuiText,
                tooltip = GuiTooltip,
                image = GuiImage
            };

            //Rect foldoutRect = controlRect;
            //foldoutRect.xMax -= 20;

            GUIStyle style = EditorStyles.foldoutHeader;
            _dropdown = EditorGUILayout.Foldout(_dropdown, content, style);
        }

        protected virtual void DrawDropdownContent()
        {
            
        }
    }
}