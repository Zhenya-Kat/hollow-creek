using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HollowCreek.Core.Logic;
using UnityEditor;
using UnityEngine;

namespace HollowCreek.Editor
{
    /// <summary>
    /// Рисует поле <c>[SerializeReference, SubclassSelector]</c>: кнопка справа открывает список типов
    /// (например, всех видов условий), выбранный тип создаётся и его настройки показываются ниже.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public sealed class SubclassSelectorDrawer : PropertyDrawer
    {
        const string NoneLabel = "(нет)";
        static readonly Dictionary<Type, Type[]> CandidatesCache = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var labelWidth = EditorGUIUtility.labelWidth;
            var buttonRect = new Rect(position.x + labelWidth + 2f, position.y,
                position.width - labelWidth - 2f, EditorGUIUtility.singleLineHeight);
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(LabelOf(property.managedReferenceValue?.GetType())),
                    FocusType.Keyboard))
                ShowMenu(property, BaseTypeOf(fieldInfo));

            // Если значение пустое, раскрывать нечего — рисуем только подпись.
            if (property.managedReferenceValue == null)
                EditorGUI.LabelField(new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight), label);
            else
                EditorGUI.PropertyField(position, property, label, true);

            EditorGUI.EndProperty();
        }

        static void ShowMenu(SerializedProperty property, Type baseType)
        {
            var serializedObject = property.serializedObject;
            var path = property.propertyPath;
            var current = property.managedReferenceValue?.GetType();

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(NoneLabel), current == null, () => Assign(serializedObject, path, null));
            menu.AddSeparator(string.Empty);
            foreach (var type in CandidatesFor(baseType))
            {
                var t = type;
                menu.AddItem(new GUIContent(LabelOf(t)), t == current,
                    () => Assign(serializedObject, path, Activator.CreateInstance(t)));
            }
            menu.ShowAsContext();
        }

        static void Assign(SerializedObject serializedObject, string path, object value)
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty(path);
            property.managedReferenceValue = value;
            property.isExpanded = value != null;
            serializedObject.ApplyModifiedProperties();
        }

        static IEnumerable<Type> CandidatesFor(Type baseType)
        {
            if (!CandidatesCache.TryGetValue(baseType, out var types))
            {
                types = TypeCache.GetTypesDerivedFrom(baseType)
                    .Where(t => !t.IsAbstract && !t.IsGenericType && t.IsSerializable
                                && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                                && t.GetConstructor(Type.EmptyTypes) != null)
                    .OrderBy(LabelOf)
                    .ToArray();
                CandidatesCache[baseType] = types;
            }
            return types;
        }

        static string LabelOf(Type type)
        {
            if (type == null) return NoneLabel;
            var attribute = type.GetCustomAttribute<SelectorLabelAttribute>();
            return attribute != null ? attribute.Label : ObjectNames.NicifyVariableName(type.Name);
        }

        static Type BaseTypeOf(FieldInfo field)
        {
            var type = field.FieldType;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return type.GetGenericArguments()[0];
            return type;
        }
    }
}
