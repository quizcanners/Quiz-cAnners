using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace QuizCanners.Utils
{

    public static partial class QcSharp
    {

     
        public static class Reflector 
        {
            private class AssignableTypeSetting : IPEGI_ListInspect
            {
                public Type Type;

                public void InspectInList(ref int edited, int index)
                {
                    ToString().PegiLabel().Write_ForCopy();

                    if (Icon.Enter.Click())
                        edited = index;
                }

                public override string ToString() => Type.FullName;

                public AssignableTypeSetting(Type type) 
                {
                    Type = type;
                }
            }

            private class AssemblySetting : IPEGI_ListInspect
            {
                public Assembly Assembly;
                public bool SearchIn = true;

                public AssemblySetting(Assembly ass) 
                {
                    Assembly = ass;
                }

                public void InspectInList(ref int edited, int index)
                {
                    pegi.ToggleIcon(ref SearchIn);
                    Assembly.GetName().Name.PegiLabel().Write_ForCopy();
                }
            }

            private static List<AssemblySetting> _allAssemblies;
            private static PlayerPrefValue.String _reflectCLassName = new PlayerPrefValue.String(key: "debClNmRefl", defaultValue: "");
            private static List<Type> matches;
            private static Type selectedType;
            private static List<AssignableTypeSetting> derrivedFromSelected;

            private static readonly HashSet<string> EXCLUDED_ASSEMBLIES = new HashSet<string>()
            {
                "mscorlib",
                "UnityEngine",
                "UnityEditor",
                "System",
                "Mono.",
                "Unity.",
                "ExCSS",
                "BeeDriver",
                "nunit",
                "NiceIO",
                "PlayerBuildProgram",
                "netstandard"
            };

            private static List<AssemblySetting> AllAssemblies 
            {
                get 
                {
                    if (_allAssemblies == null) 
                    {
                        _allAssemblies = new List<AssemblySetting>();

                        var entAss = AppDomain.CurrentDomain.GetAssemblies();

                        foreach (var ass in entAss)
                        {
                            bool exclude = false;
                            foreach (var exc in EXCLUDED_ASSEMBLIES)
                                if (ass.FullName.Contains(exc))
                                {
                                    exclude = true;
                                    break;
                                }

                            if (!exclude)
                                _allAssemblies.Add(new AssemblySetting(ass));
                        }
                    }

                    return _allAssemblies;
                }
            }

            private static void Clear() 
            {
                matches = null;
                selectedType = null;
                derrivedFromSelected = null;
            }

            private static bool foldoutAssemblies = false;

            public static void Inspect()
            {
                var changes = pegi.ChangeTrackStart();

                if ("Assmeblies".PegiLabel().IsFoldout(ref foldoutAssemblies).Nl())
                {
                    "Assemblies to search in".PegiLabel().Edit_List(AllAssemblies).Nl().OnChanged(Clear);

                    "Refresh".PegiLabel().Click().OnChanged(() => _allAssemblies = null).Nl();
                }
                else
                {
                    var typeName = _reflectCLassName.GetValue();
                    "Class Name".PegiLabel(90).Edit_Delayed(ref typeName).OnChanged(() => _reflectCLassName.SetValue(typeName)).OnChanged(Clear).UnfocusOnChange().Nl();

                    if (matches == null && !typeName.IsNullOrEmpty())
                    {
                        matches = new List<Type>();

                        foreach (var ass in AllAssemblies)
                            foreach (Type type in ass.Assembly.GetTypes())
                            {
                                if (type.Name.Contains(typeName))
                                {
                                    matches.Add(type);
                                }
                            }
                            

                        if (matches.Count == 1)
                            selectedType = matches[0];
                    }

                    if (selectedType == null)
                    {
                        if (!matches.IsNullOrEmpty())
                        {
                            foreach (var t in matches)
                            {
                                if (t.Name.PegiLabel(toolTip: t.FullName).Click().UnfocusOnChange().Nl())
                                    selectedType = t;
                            }
                        }
                    }
                    else
                    {
                        if (Icon.Clear.Click().UnfocusOnChange())
                            Clear();
                        else
                        {
                            if (derrivedFromSelected == null)
                            {
                                derrivedFromSelected = new List<AssignableTypeSetting>();

                                foreach (var a in AllAssemblies)
                                {
                                    if (a.SearchIn)
                                    {
                                        var types = a.Assembly.GetTypes();
                                        foreach (var t in types)
                                        {
                                            if (selectedType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && (t != selectedType))
                                                derrivedFromSelected.Add(new AssignableTypeSetting(t));
                                        }
                                    }
                                }
                            }

                            if (derrivedFromSelected.Count == 0)
                                "No classes are assignable from {0}".F(selectedType.Name).PegiLabel().Nl();
                            else
                            {
                                "Assignable From {0}".F(selectedType.Name).PegiLabel(pegi.Styles.ListLabel).Nl();

                                if ("{0}s To Clipboard".F(selectedType.Name).PegiLabel().Click().Nl())
                                {
                                    var sb = new StringBuilder();
                                    foreach (var t in derrivedFromSelected)
                                    {
                                        sb.Append(t.Type.FullName).Append(pegi.EnvironmentNl);
                                    }

                                    pegi.CopyPasteBuffer = sb.ToString();
                                }

                                "Assignable Types".PegiLabel().Edit_List(derrivedFromSelected).Nl();
                            }
                        }
                    }
                }
            }

            public static void InspectDerrived<T>() 
            {
                var rews = GetTypesAssignableFrom<T>(); 

                if ("{0}s To Clipboard".F(typeof(T).Name).PegiLabel().Click().Nl())
                {
                    var sb = new StringBuilder();
                    foreach (var t in rews)
                    {
                        sb.Append(t.Name).Append(pegi.EnvironmentNl);
                    }

                    pegi.CopyPasteBuffer = sb.ToString();
                }

                foreach (var t in rews)
                {
                    t.Name.PegiLabel().Write_ForCopy(showCopyButton: true).Nl();
                }
            }
        }



        internal static T TryGetClassAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {

            if (!type.IsClass) return null;

            var attrs = type.GetCustomAttributes(typeof(T), inherit);
            return (attrs.Length > 0) ? (T)attrs[0] : null;

        }

        private static class DerrivedReflectionCache<T>
        {
            private static List<Type> _list;

            public static List<Type> GetAssignableTypes()
            {
                if (_list != null)
                    return _list;

                _list = new List<Type>();

                var type = typeof(T);

                var types = Assembly.GetAssembly(type).GetTypes();

                foreach (var t in types)
                {
                    if (type.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract && (t != type))
                        _list.Add(t);
                }

                return _list;
            }
        }

        public static List<Type> GetTypesAssignableFrom<T>() => DerrivedReflectionCache<T>.GetAssignableTypes();

        public static List<Type> FindAllChildTypes(this Type type)
        {
            var types = Assembly.GetAssembly(type).GetTypes();

            List<Type> list = new List<Type>();

            foreach (var t in types)
            {
                if (t.IsSubclassOf(type) && t.IsClass && !t.IsAbstract && (t != type))
                    list.Add(t);
            }

            return list;
        }

        public static bool ContainsInstanceOfType<T>(this List<T> collection, Type type)
        {

            foreach (var t in collection)
                if (t != null && t.GetType() == type) return true;

            return false;
        }

    }
}