using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;


// ReSharper disable InconsistentNaming
#pragma warning disable IDE0011 // Add braces

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {

        public class TabContext
        {
            internal string tab;
            private readonly string _playerPrefsKey;
            private bool _playerPresfChecked;

            public string CurrentTab => tab;

            public IDisposable StartContext()
            {
                var tb = new TabCollector(this);
                s_tabs.Add(tb);
                return tb;
            }

            internal static List<TabCollector> s_tabs = new();

            void CheckPlayerPrefs()
            {
                if (_playerPresfChecked)
                    return;

                _playerPresfChecked = true;

                if (_playerPrefsKey.IsNullOrEmpty())
                    return;

                tab = PlayerPrefs.GetString(_playerPrefsKey, tab);
            }

            void OnTabChanged()
            {
                if (_playerPrefsKey.IsNullOrEmpty())
                    return;

                PlayerPrefs.SetString(_playerPrefsKey, tab);
            }

            internal class TabCollector : IDisposable
            {
                private readonly TabContext _token;

                private readonly List<TabElement> _tabs = new();

                private bool gotIcons = false;

                void Clear()
                {
                    _tabs.Clear();
                    s_tabs.Remove(this);

                    gotIcons = false;
                }

                public void Add(string name, string key, Action action)
                {
                    _tabs.Add(new TabElement() 
                    {
                        Key = key,
                        TabName = name,
                        Action = action,
                    });
                }

                public void Add(TabElement element) 
                {
                    gotIcons |= element.GotIcon;
                    _tabs.Add(element);
                }

                public void Add(Icon icon, string name, string key, Action action)
                {
                    _tabs.Add(new TabElement()
                    {
                        Key = key,
                        TabName = name,
                        Action = action,
                        Icon = icon.GetIcon()
                    });

                    gotIcons = true;
                }

                internal TabCollector(TabContext token)
                {
                    _token = token;
                }

                public void Dispose()
                {
                    _token.CheckPlayerPrefs();

                    using (QcSharp.DisposableAction(Clear))
                    {
                        ChangesToken changed;

                        string[] keys = new string[_tabs.Count];

                        if (!gotIcons)
                        {
                            string[] names = new string[_tabs.Count];
                            for(int i=0; i< _tabs.Count; i++) 
                            {
                                var tab = _tabs[i];

                                keys[i] = tab.Key;
                                names[i] = tab.TabName;
                            }

                            changed = Tabs(ref _token.tab, keys, names).Nl();
                        }
                        else
                        {
                            GUIContent[] contents = new GUIContent[_tabs.Count];

                            for (int i = 0; i < _tabs.Count; i++)
                            {
                                var tab = _tabs[i];

                                var text = tab.TabName;
                                keys[i] = tab.Key;

                                if (tab.Icon)
                                {
                                    var cnt = new GUIContent() { image = tab.Icon, text = text, };
                                    contents[i] = cnt;
                                }
                                else
                                {
                                    contents[i] = new GUIContent() { text = text };
                                }
                            }

                            changed = Tabs(ref _token.tab, keys, contents).Nl();
                        }

                        if (changed)
                            _token.OnTabChanged();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (keys[i].Equals(_token.tab))
                            {
                                _tabs[i].Action.Invoke();
                                return;
                            }
                        }
                    }
                }
            }

            public TabContext() { }

            public TabContext(string playerPrefsKey)
            {
                _playerPrefsKey = playerPrefsKey;
            }
        }

        public struct TabElement
        {
            public Texture Icon;
            public string TabName;
            public string Key;
            public Action Action;

            public readonly bool GotIcon => Icon;
        }

        public static ChangesToken Tabs(ref string tabKey, string[] options) => Tabs(ref tabKey, optionKeys: options, optionDisplay: options);

        public static ChangesToken Tabs(ref string tabKey, string[] optionKeys, GUIContent[] tabs)
        {
            int index = 0;

            for (int i = 0; i < optionKeys.Length; i++)
            {
                if (!tabKey.IsNullOrEmpty() && optionKeys[i].Equals(tabKey))
                {
                    index = i;
                    break;
                }
            }

            var changes = ChangeTrackStart();
            CheckLine();
            var newIndex = GUILayout.Toolbar(index, tabs, GUILayout.MaxHeight(25), GUILayout.MaxWidth(Screen.width-25));
            (newIndex != index).FeedChanges_Internal(LatestInteractionEvent.Enter);

            tabKey = optionKeys[newIndex];

            return changes;
        }

        public static ChangesToken Tabs(ref string tabKey, string[] optionKeys, string[] optionDisplay) 
        {
            int index = 0;

            for (int i = 0; i < optionKeys.Length; i++)
            {
               // var disp = optionDisplay[i];

                if (!tabKey.IsNullOrEmpty() && optionKeys[i].Equals(tabKey))
                {
                    index = i;
                    break;
                }
            }

            var changes = ChangeTrackStart();
            CheckLine();
            var newIndex = GUILayout.Toolbar(index, optionDisplay);
            (newIndex != index).FeedChanges_Internal(LatestInteractionEvent.Enter);

            tabKey = optionKeys[newIndex];

            return changes;
        }

        public static ChangesToken Tabs(ref string tab, List<string> options) => Tabs(ref tab, options.ToArray());

        public static ChangesToken Tabs_Enum<T>(ref T value) where T:Enum 
        {
            var names = Enum.GetNames(typeof(T));
            var val = (T[])Enum.GetValues(typeof(T));

            int currentIndex = -1;

            for (var i = 0; i < val.Length; i++)
            {
                if (val[i].Equals(value))
                {
                    currentIndex = i;
                    break;
                }
            }

            CheckLine();
            var newIndex = GUILayout.Toolbar(currentIndex, names);

            if (newIndex != currentIndex)
            {
                value = val[newIndex]; //(T)Enum.Parse(typeof(T), names[newIndex]);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }


   
        public static void AddTab(string constTabName, Action inspect) => AddTab(constTabName, tabKey: constTabName, inspect);

        public static void AddTab(string tabName, string tabKey, Action inspect) 
        {
            if (TabContext.s_tabs.Count == 0)
            {
                "NO TAB CONTEXT FOR {0}".F(tabName).PL().Nl();
                return;
            }

            TabContext.s_tabs[^1].Add(tabName, key: tabKey, inspect);
        }


        public static void AddTab(string tabName, string tabKey, Action inspect, Object objectToSetDirty)
        {
            AddTab(tabName, tabKey: tabKey, () =>
            {
                var changes = ChangeTrackStart();

                inspect.Invoke();

                if (changes)
                    objectToSetDirty.SetToDirty();
            });
        }

        public static void AddTab(string constTabName, Action inspect, Object objectToSetDirty)
            => AddTab(constTabName, tabKey: constTabName, inspect, objectToSetDirty);

        public static void AddTab<T>() where T : Singleton.BehaniourBase 
        {
            if (!Singleton.TryGet<T>(out var s))
                return;

            var tabName = s.GetNameForInspector();

            AddTab(tabName, tabKey: tabName, ()=> s.Nested_Inspect());
        }

        public static void AddTab<T>(string constTabName, T ipegi) where T : Object, IPEGI
        {
            AddTab(constTabName, tabKey: constTabName, ()=>
            {
                if (ipegi.Nested_Inspect())
                    ipegi.SetToDirty();
            });
        }

        public static void AddTab(IPEGI ipegi, Object objectToSetDirty) => AddTab(ipegi.GetNameForInspector(), ipegi, objectToSetDirty);

        public static void AddTab(TabElement element) 
        {
            TabContext.s_tabs[^1].Add(element);
        }

        public static void AddTab(Icon icon, string tabName, string tabKey, Action inspectAction)
        {
            if (TabContext.s_tabs.Count == 0)
            {
                "NO TAB CONTEXT FOR {0}".F(tabName).PL().Nl();
                return;
            }

            TabContext.s_tabs[^1].Add(new TabElement()
            {
                Key = tabKey,
                TabName = tabName,
                Icon = icon.GetIcon(),
                Action = inspectAction,
            });
        }

        public static void AddTab<T>(Icon icon, T ipegi) where T : Object, IPEGI
        {
            string name = ipegi.GetNameForInspector();

            AddTab(icon, tabName: "", tabKey: name, () =>
            {
                if (ipegi.Nested_Inspect())
                    ipegi.SetToDirty();
            });
        }

        public static void AddTab<T>(Icon icon, string tabKey, T ipegi) where T : Object, IPEGI
        {
            AddTab(icon, tabName: "", tabKey: tabKey, () =>
            {
                if (ipegi.Nested_Inspect())
                    ipegi.SetToDirty();
            });
        }

        public static void AddTab(Icon icon, string tabKey, Action action) => AddTab(icon, tabName: "", tabKey: tabKey, action);
        
        public static void AddTab(Icon icon, IPEGI ipegi, Object objectToSetDirty) => AddTab(icon, "", tabKey: ipegi == null ? icon.ToString() : ipegi.ToString(), () =>
            {
                if (ipegi.Nested_Inspect())
                    objectToSetDirty.SetToDirty();
            });

        public static void AddTab(Icon icon, string constTabName, IPEGI ipegi, Object objectToSetDirty) =>
            AddTab(icon, constTabName, tabKey: constTabName, () =>
            {
                if (ipegi.Nested_Inspect())
                    objectToSetDirty.SetToDirty();
            });
        
        public static void AddTab(string constTabName, IPEGI ipegi, Object objectToSetDirty) =>
            AddTab(constTabName, tabKey: constTabName, ()=>
            {
                if (ipegi.Nested_Inspect())
                    objectToSetDirty.SetToDirty();
            });
        
    }
}