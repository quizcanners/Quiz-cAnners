using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuizCanners.Inspect.Examples
{
    internal static partial class PlayerAndEditorGui_Documentation
    {
        private abstract class DocEntry : IPEGI, ISearchable
        {

            protected pegi.CollectionInspectorMeta functionsList = new pegi.CollectionInspectorMeta("Functions", allowDeleting: false, showAddButton: false, showEditListButton: false);

            public virtual void Inspect()
            {
                if (functionsList.IsAnyEntered == false)
                {
                    Inspect_About();
                    pegi.Line();
                }

                var l = GetFunctions();
                if (l.Count > 0)
                    functionsList.Edit_List(GetFunctions());
            }

            public abstract void Inspect_About();

            protected void DrawExample(Action drawElement, string code)
            {
                pegi.Line();
                drawElement.Invoke(); pegi.Nl();

                using (pegi.Indent())
                {
                    code.PegiLabel().Write_ForCopy(writeAsEditField: true).Nl(); //DrawExample();
                }
            }

            public virtual IEnumerator SearchKeywordsEnumerator()
            {
                yield return GetFunctions();
            }

            protected virtual List<FunctionData> GetFunctions() => new List<FunctionData>();

            protected enum ReturnType { Void, Changes, Click, State, TextToken, ChangesTracker, SameReturnType }

            protected class FunctionData : IPEGI, IPEGI_ListInspect
            {
                private readonly string FunctionName;
                private readonly ReturnType ReturnType;
                private readonly string About;
                private readonly List<Parameter> Parameters;
                private readonly bool ExtendsPegiLabel;

                public FunctionData(string functionName, ReturnType returnType, string about, bool extendsPegiLabel, params Parameter[] parameters)
                {
                    FunctionName = functionName;
                    ReturnType = returnType;
                    About = about;
                    Parameters = new List<Parameter>(parameters);
                    ExtendsPegiLabel = extendsPegiLabel;
                }

                public override string ToString()
                {
                    string returnTypeString;

                    switch (ReturnType)
                    {
                        case ReturnType.State:
                        case ReturnType.Click:
                        case ReturnType.ChangesTracker:
                        case ReturnType.Changes: returnTypeString = "bool"; break;
                        case ReturnType.SameReturnType: returnTypeString = "T"; break;
                        default: returnTypeString = "void"; break;
                    }

                    StringBuilder sb = new StringBuilder();

                    if (ExtendsPegiLabel)
                    {
                        sb.Append("this PegiLabel label");
                    }

                    foreach (var p in Parameters)
                    {
                        if (sb.Length > 0)
                            sb.Append(',').Append(' ');
                        sb.Append(p.Name);
                    }

                    return "{0} {1}({2});".F(returnTypeString, FunctionName, sb.ToString());
                }

                void IPEGI.Inspect()
                {
                    switch (ReturnType)
                    {
                        case ReturnType.State:
                            "Returns {0} which can be cast to bool. True every time if {1} is being entered or folded out.".F(nameof(pegi.StateToken), FunctionName).PegiLabel().Write_Hint().Nl(); break;
                        case ReturnType.Click:
                            "Returns {0} which can be cast to bool. True if user clicked on the {1}.".F(nameof(pegi.ChangesToken), FunctionName).PegiLabel().Write_Hint().Nl(); break;
                        case ReturnType.Changes:
                            "Returns {0} which can be cast to bool. True if user modified the value {1}.".F(nameof(pegi.ChangesToken), FunctionName).PegiLabel().Write_Hint().Nl(); break;
                        case ReturnType.ChangesTracker:
                            "Returns {0} which casts to True if any changes were made between the moment it was Started to the moment of the case.".F(nameof(pegi.ChangesTracker), FunctionName).PegiLabel().Write_Hint().Nl(); break;
                        case ReturnType.SameReturnType:
                            "{0} can work as an extension on top of {1}, {2} and {3}. Will return the same type and value as extended.".F(FunctionName, nameof(pegi.ChangesToken), nameof(pegi.StateToken), nameof(pegi.TextToken)).PegiLabel().Write_Hint().Nl(); break;
                        case ReturnType.TextToken:
                            "Text token is returned by write functions. Main purpouse is to allow writing .nl() extension at the end of the text.".PegiLabel().Write_Hint().Nl(); break;
                        default: "Undocumented {0}".F(ReturnType).PegiLabel().Write_Hint().Nl(); break;
                    }

                    foreach (var v in Parameters)
                    {
                        v.Nested_Inspect().Nl();
                    }
                }

                public void InspectInList(ref int edited, int index)
                {
                    string txt = ToString();
                    // icon.Copy.Click().OnChanged(()=> pegi.SetCopyPasteBuffer(txt));

                    if (Icon.Enter.Click() | txt.PegiLabel().ClickText(fontSize: 18))
                        edited = index;

                    // (writeAsEditField: true);//DrawExample();
                }
            }

            protected class Parameter : IPEGI
            {
                public string Name;
                public string Description;
                public bool Optional;

                public Parameter(string name, string description, bool optional = false)
                {
                    Name = name;
                    Description = description;
                    Optional = optional;
                }

                void IPEGI.Inspect()
                {
                    ((Optional ? "(Optional) " : "") + Name).PegiLabel(toolTip: "Parameter Name", style: pegi.Styles.BaldText).Nl();

                    using (pegi.Indent())
                    {
                        Description.PegiLabel().WriteBig();
                    }
                }
            }


            protected int exampleIndex = -1;
            protected string exampleKey = "none";

            protected readonly List<TestClass> exampleList = new List<TestClass>() { new TestClass("A"), new TestClass("B"), new TestClass("C"), new TestClass("D"), new TestClass("E") };

            protected readonly Dictionary<string, TestClass> exampleDictionary = new Dictionary<string, TestClass>()
            {
                {"a", new TestClass("Element A") },
                {"b", new TestClass("Element B") },
                {"c", new TestClass("Element C") },
                {"d", new TestClass("Element D") },
                {"e", new TestClass("Element E") },
            };

            protected class TestClass
            {
                private readonly string name;

                public override string ToString()
                {
                    return name;
                }

                public TestClass(string name)
                {
                    this.name = name;
                }
            }
        }

        private class CodeStringBuilder
        {
            const string TAB = "\t";

            private int _indented = 0;
            private int lines;
            private bool _newLineRequested = false;

            private readonly StringBuilder _sb = new StringBuilder();

            private void Nl()
            {
                lines++;
                _newLineRequested = true;
            }

            public CodeStringBuilder In()
            {
                AppendLine('{');
                Indent();
                return this;
            }

            public CodeStringBuilder Out()
            {
                UnIndent();
                AppendLine('}');
                return this;
            }

            public CodeStringBuilder Indent()
            {
                _indented++;
                return this;
            }

            public CodeStringBuilder UnIndent()
            {
                _indented = Math.Max(0, _indented - 1);
                return this;
            }

            public void DrawExample()
            {
                using (pegi.Indent())
                {
                    _sb.ToString().PegiLabel(pegi.Styles.FoldedOutLabel).Write_ForCopy_Big(lines: lines);
                }
            }

            public CodeStringBuilder AppendLine()
            {
                _sb.AppendLine("");
                Nl();
                return this;
            }
            public CodeStringBuilder AppendLine(string value)
            {
                CheckPreviousLine();
                _sb.AppendLine(value);
                Nl();
                return this;
            }
            public CodeStringBuilder AppendLine(char ch)
            {
                CheckPreviousLine();
                _sb.Append(ch).AppendLine();
                Nl();
                return this;
            }

            public CodeStringBuilder Append(string value)
            {
                CheckPreviousLine();
                _sb.Append(value);
                return this;
            }
            public CodeStringBuilder Append(char value)
            {
                CheckPreviousLine();
                _sb.Append(value);

                return this;
            }

            private void CheckPreviousLine()
            {
                if (_newLineRequested)
                {
                    _newLineRequested = false;
                    for (int i = 0; i < _indented; i++)
                        Tab();
                }
            }

            private StringBuilder Tab() => _sb.Append(TAB);
        }


    }
}
