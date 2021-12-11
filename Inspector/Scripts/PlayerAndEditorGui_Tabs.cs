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
                private TabContext _token;

                private List<string> tabs = new List<string>();
                private List<Action> actions = new List<Action>();

                public void Add(string name, Action action)
                {
                    tabs.Add(name);
                    actions.Add(action);
                }

                internal TabCollector(TabContext token)
                {
                    _token = token;
                }

                public void Dispose()
                {
                    Tabs(ref _token.tab, tabs.ToArray()).Nl();

                    for (int i = 0; i < tabs.Count; i++)
                    {
                        if (tabs[i].Equals(_token.tab))
                        {
                            actions[i].Invoke();
                            return;
                        }
                    }

                    actions.Clear();
                    tabs.Clear();
                }
            }
        }

        public static void AddTab(string tabName, Action inspect) 
        {
            if (TabContext.s_tabs.Count == 0)
            {
                "NO TAB CONTEXT FOR {0}".F(tabName).PegiLabel().Nl();
                return;
            }

            TabContext.s_tabs[TabContext.s_tabs.Count - 1].Add(tabName, inspect);
        }
        public static void AddTab(IPEGI ipegi) => AddTab(ipegi.GetNameForInspector(), () => ipegi.Nested_Inspect());
        public static void AddTab(string tabName, IPEGI ipegi) => AddTab(tabName, () => ipegi.Nested_Inspect());
    }
}