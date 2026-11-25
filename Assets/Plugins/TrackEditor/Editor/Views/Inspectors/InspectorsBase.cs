using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TrackEditor
{
    public class InspectorsBase
    {
        protected object target;

        public void SetTarget(object t)
        {
            target = t;
        }

        public virtual void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        public void DrawDefaultInspector() => DrawFields(target);

        private void DrawFields(object owner)
        {
            var t = owner.GetType();
            //得到字段的值,只能得到public类型的字典的值
            FieldInfo[] fieldInfos = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            //排序一下，子类的字段在后，父类的在前
            Array.Sort(fieldInfos, FieldsSprtBy);

            //判断需要过滤不显示的字段
            List<FieldInfo> needShowField = new List<FieldInfo>();
            foreach (var field in fieldInfos)
            {
                var need = !field.IsNotSerialized && !field.IsDefined(typeof(HideInInspector), true);
                var attributes = field.GetCustomAttributes();
                foreach (var attribute in attributes)
                {
                    if (attribute is OptionRelateParamAttribute option)
                    {
                        var relate = Array.Find(fieldInfos, f1 => f1.Name == option.argsName);
                        if (relate != null)
                        {
                            var value = relate.GetValue(owner);
                            var index = Array.FindIndex(option.argsValue, v1 => v1.Equals(value));
                            if (index < 0)
                            {
                                need = false;
                                break;
                            }
                        }
                    }
                }

                if (need)
                {
                    needShowField.Add(field);
                }
            }

            foreach (var field in needShowField)
            {
                FieldDefaultInspector(field, owner);
            }
        }

        protected void FieldDefaultInspector(FieldInfo field) => FieldDefaultInspector(field, target);

        private void FieldDefaultInspector(FieldInfo field, object owner)
        {
            var fieldType = field.FieldType;
            var showType = field.FieldType;
            var value = field.GetValue(owner);
            var newValue = value;

            //首字母大写
            var name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(field.Name);

            List<object> args = new List<object>();

            var attributes = field.GetCustomAttributes();
            foreach (var attribute in attributes)
            {
                var t = attribute.GetType();
                if (attribute is MenuNameAttribute menuNameAttribute)
                {
                    name = menuNameAttribute.showName;
                }
                else if (attribute is RangeAttribute rangeAttribute && fieldType == typeof(float))
                {
                    showType = t;
                    args = new List<object> { rangeAttribute.min, rangeAttribute.max };
                }
                else if (attribute is OptionParamAttribute option)
                {
                    showType = t;
                    args = new List<object> { option.classType };
                }
                else if (attribute is SelectObjectPathAttribute selectObjectPathAttribute)
                {
                    showType = t;
                    args = new List<object> { selectObjectPathAttribute.type };
                }
            }

            if (field.GetCustomAttribute<TimelineAssetReferenceAttribute>() is TimelineAssetReferenceAttribute assetReference)
            {
                var current = AssetStorage.Resolve((TimelineAssetReference)value, assetReference.Type);
                EditorGUI.BeginChangeCheck();
                var selected = EditorGUILayout.ObjectField(name, current, assetReference.Type, false);
                if (EditorGUI.EndChangeCheck())
                {
                    field.SetValue(owner, AssetStorage.ReferenceOf(selected));
                }

                return;
            }

            EditorGUI.BeginChangeCheck();
            if (showType == typeof(RangeAttribute))
            {
                if (fieldType == typeof(float))
                {
                    newValue = EditorGUILayout.Slider(name, (float)value, (float)args[0], (float)args[1]);
                }
            }
            else if (showType == typeof(OptionParamAttribute))
            {
                // LookAtType.None
                var t = (Type)args[0];
                var fields = t.GetFields();
                //先对fields排序
                Array.Sort(fields, FieldsSort);

                var title = new List<string>();
                var mask = 0;
                foreach (var f in fields)
                {
                    var menuNameAttr = f.GetCustomAttribute<MenuNameAttribute>();
                    title.Add(menuNameAttr != null ? menuNameAttr.showName : f.Name);
                }

                mask = (int)value;

                newValue = EditorGUILayout.Popup(name, mask, title.ToArray());
            }
            else if (showType == typeof(SelectObjectPathAttribute))
            {
                if (args[0] is Type type)
                {
                    Object obj = null;
                    var path = value as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        obj = AssetDatabase.LoadAssetAtPath(path, type);
                    }

                    var newObj = EditorGUILayout.ObjectField(name, obj, type, false);
                    if (newObj != obj)
                    {
                        newValue = AssetDatabase.GetAssetPath(newObj);
                    }
                }
            }

            else
            {
                newValue = DrawValue(fieldType, name, value);
            }

            if (EditorGUI.EndChangeCheck())
            {
                field.SetValue(owner, newValue);
            }
        }

        private object DrawValue(Type type, string name, object value)
        {
            var newValue = value;
            if (type == typeof(int))
            {
                newValue = EditorGUILayout.IntField(name, (int)value);
            }
            else if (type == typeof(float))
            {
                newValue = EditorGUILayout.FloatField(name, (float)value);
            }
            else if (type == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(name, (bool)value);
            }
            else if (type == typeof(string))
            {
                newValue = EditorGUILayout.TextField(name, (string)value ?? string.Empty);
            }
            else if (type == typeof(Color))
            {
                newValue = EditorGUILayout.ColorField(name, (Color)value);
            }
            else if (type.IsSubclassOf(typeof(Object)))
            {
                newValue = EditorGUILayout.ObjectField(name, (Object)value, type, false);
            }
            else if (type.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(name, (Enum)value);
            }
            else if (type == typeof(AnimationCurve))
            {
                AnimationCurve curve = value as AnimationCurve;
                if (curve == null)
                {
                    curve = new AnimationCurve();
                }
                newValue = EditorGUILayout.CurveField(name, curve);
            }
            else if (type == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field(name, (Vector2)value);
            }
            else if (type == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(name, (Vector3)value);
            }
            else if (type == typeof(Vector4))
            {
                newValue = EditorGUILayout.Vector4Field(name, (Vector4)value);
            }
            else if (type == typeof(Vector2Int))
            {
                newValue = EditorGUILayout.Vector2IntField(name, (Vector2Int)value);
            }
            else if (type == typeof(Vector3Int))
            {
                newValue = EditorGUILayout.Vector3IntField(name, (Vector3Int)value);
            }
            else if (type == typeof(Rect))
            {
                newValue = EditorGUILayout.RectField(name, (Rect)value);
            }
            else if (type == typeof(RectInt))
            {
                newValue = EditorGUILayout.RectIntField(name, (RectInt)value);
            }
            else if (type == typeof(Bounds))
            {
                newValue = EditorGUILayout.BoundsField(name, (Bounds)value);
            }
            else if (type.IsArray && type.GetArrayRank() == 1)
            {
                newValue = DrawCollection(type, type.GetElementType(), name, (IList)value);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                newValue = DrawCollection(type, type.GetGenericArguments()[0], name, (IList)value);
            }
            else if (type.IsDefined(typeof(SerializableAttribute), false))
            {
                if (value == null && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    if (GUILayout.Button($"Create {name}"))
                    {
                        GUI.changed = true;
                        newValue = Activator.CreateInstance(type);
                    }
                }
                else if (value != null)
                {
                    EditorGUILayout.LabelField(name, EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    DrawFields(value);
                    EditorGUI.indentLevel--;
                }
            }

            return newValue;
        }

        private IList DrawCollection(Type type, Type elementType, string name, IList collection)
        {
            var oldCount = collection?.Count ?? 0;
            var size = Mathf.Max(0, EditorGUILayout.DelayedIntField(name, oldCount));
            EditorGUI.indentLevel++;
            for (var index = 0; index < oldCount; index++)
            {
                EditorGUI.BeginChangeCheck();
                var value = DrawValue(elementType, $"Element {index}", collection[index]);
                if (EditorGUI.EndChangeCheck())
                {
                    collection[index] = value;
                }
            }
            EditorGUI.indentLevel--;

            if (size == oldCount) return collection;

            if (type.IsArray)
            {
                var resized = Array.CreateInstance(elementType, size);
                if (collection != null) Array.Copy((Array)collection, resized, Mathf.Min(oldCount, size));
                return resized;
            }

            collection = collection ?? (IList)Activator.CreateInstance(type);
            while (collection.Count > size) collection.RemoveAt(collection.Count - 1);
            while (collection.Count < size)
            {
                collection.Add(elementType == typeof(string) ? string.Empty
                    : elementType.IsValueType ? Activator.CreateInstance(elementType) : null);
            }

            return collection;
        }

        private int FieldsSprtBy(FieldInfo f1, FieldInfo f2)
        {
            if (f1 == null || f2 == null)
                return 0;
            //相同则是自身的字段，不相同则是继承的
            var e1 = f1.DeclaringType == f1.ReflectedType;
            var e2 = f2.DeclaringType == f2.ReflectedType;
            if (e1 != e2)
            {
                if (e1)
                {
                    return 1;
                }

                return -1;
            }

            return 0;
        }

        private int FieldsSort(FieldInfo f1, FieldInfo f2)
        {
            var sort1 = f1.GetCustomAttribute<OptionSortAttribute>();
            var sort2 = f2.GetCustomAttribute<OptionSortAttribute>();
            var i1 = 99;
            var i2 = 99;
            if (sort1 != null)
            {
                i1 = sort1.sort;
            }

            if (sort2 != null)
            {
                i2 = sort2.sort;
            }

            return i1 - i2;
        }
    }
}
