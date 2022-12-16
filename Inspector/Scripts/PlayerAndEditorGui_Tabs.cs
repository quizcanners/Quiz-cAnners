using System;
using System.Collections.Generic;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;


// ReSharper disable InconsistentNaming
#pragma warning disable IDE0011 // Add braces

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public static ChangesToken Tabs(ref string tab, string[] options) 
        {
            int index = 0;
            if (!tab.IsNullOrEmpty())
            {
                for (int i = 0; i < options.Length; i++)
                    if (options[i].Equals(tab))
                    {
                        index = i;
                        break;
                    }
            }

            var changes = ChangeTrackStart();
            CheckLine();
            var newIndex = GUILayout.Toolbar(index, options);
            (newIndex != index).FeedChanges_Internal(LatestInteractionEvent.Enter);

            tab = options[newIndex];

            return changes;
        }
        public static ChangesToken Tabs(ref string tab, List<string> options) => Tabs(ref tab, options.ToArray());

        public class TabContext 
        {
            internal string tab;

            public IDisposable StartContext() 
            {
                var tb = new TabCollector(this);
                s_tabs.Add(tb);
                return tb;
            }

            internal static List<TabCollector> s_tabs = new List<TabCollector>();

            internal class TabCollector : IDisposable
            {
                private readonly TabContext _token;

                private readonly List<string> _tabs = new List<string>();
                private readonly List<Action<ChangesToken>> _actions = new List<Action<ChangesToken>>();

                public void Add(string name, Action<ChangesToken> action)
                {
                    _tabs.Add(name);
                    _actions.Add(action);
                }

                internal TabCollector(TabContext token)
                {
                    _token = token;
                }

                public void Dispose()
                {
                    var changed = Tabs(ref _token.tab, _tabs.ToArray()).Nl();

                    for (int i = 0; i < _tabs.Count; i++)
                    {
                        if (_tabs[i].Equals(_token.tab))
                        {
                            _actions[i].Invoke(changed);
                            return;
                        }
                    }

                    _actions.Clear();
                    _tabs.Clear();
                }
            }
        }

        public static void AddTab(string tabName, Action<ChangesToken> inspect) 
        {
            if (TabContext.s_tabs.Count == 0)
            {
                "NO TAB CONTEXT FOR {0}".F(tabName).PegiLabel().Nl();
                return;
            }

            TabContext.s_tabs[TabContext.s_tabs.Count - 1].Add(tabName, inspect);
        }
        public static void AddTab(IPEGI ipegi) => AddTab(ipegi.GetNameForInspector(), change => ipegi.Nested_Inspect());
        public static void AddTab(string tabName, IPEGI ipegi) => AddTab(tabName, change => ipegi.Nested_Inspect());
    }
}