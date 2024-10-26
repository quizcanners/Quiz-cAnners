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
        private static readonly List<DocEntry> entries = new() { 
            new OverviewEntry(), 
            new InterfacesEntry(),
            new LabelEntry(), 
            new WriteEntry(), 
            new NewLineEntry(), 
            new ChangesEntry(), 
            new EditEntry(), 
            new ClickEntry(), 
            new NestedInspectEntry(), 
            new EnterEntry(), 
            new SelectEntry(),
            new CollectionEntry(), 
            new DrawEntry(), 
            new ReturnTypesEntry() };

        private static readonly pegi.CollectionInspectorMeta _entriesListMeta = new("Player & Editor GUI Guide", allowDeleting: false, showAddButton: false, showEditListButton: false, playerPrefsIndex: "pgFnkInd");

        public static void Inspect() => _entriesListMeta.Edit_List(entries);
        

        private class OverviewEntry : DocEntry
        {
            public override string ToString() => "Overview";

            public override void Inspect_About()
            {
                "This script is a Documentation & Example at the same time:".PegiLabel().Nl();
                nameof(PlayerAndEditorGui_Documentation).PegiLabel().Write_ForCopy(showCopyButton: true).Nl();

                "Override Inspector".PegiLabel(pegi.Styles.ListLabel).Nl();

                "The following can be used for {0} and {1}. Material inspector override is different ({2}).".F(nameof(MonoBehaviour), nameof(ScriptableObject), nameof(PEGI_Inspector_Material)).PegiLabel().Nl();

                var sb = new CodeStringBuilder();
                sb.AppendLine("class Example : {0}, IPEGI".F(nameof(MonoBehaviour)))
                  .In().AppendLine("void IPEGI.Inspect()")
                            .In().AppendLine("//pegi Functions")
                            .Out()
                    .Out()
                    .UnIndent()
                    .AppendLine()
                    .AppendLine("[{0}(typeof(Example))]".F(nameof(PEGI_Inspector_Override)))
                    .AppendLine("internal class ExampleDrawer : {0}".F(nameof(PEGI_Inspector_Override))).Append("{}")
                  .DrawExample(); 

                (" This is a wrapper on top of Unity's EditorGUILayout and GUI, rendering one in Inspector window and second in GameView. {0}"
                    ).F(pegi.EnvironmentNl).PegiLabel().WriteBig();

                "Player Inspector".PegiLabel(pegi.Styles.ListLabel).Nl();

                sb = new CodeStringBuilder();
                sb.AppendLine("class ExampleMono : {0}, IPEGI".F(nameof(MonoBehaviour)))
                  .In()
                  .AppendLine("void IPEGI.Inspect()")
                            .In().AppendLine("//pegi Functions")
                            .Out()
                            .AppendLine()
                            .AppendLine("private readonly pegi.GameView.Window _window = new pegi.GameView.Window(upscale: 2.5f);")
                    .AppendLine("void OnGUI() => _window.Render(Mgmt);")
                    .Out()
                  .DrawExample();

            }
        }

        // TODO: Interfaces
        private class InterfacesEntry : DocEntry
        {
            public override string ToString() => "Interfaces";

            public override void Inspect_About()
            {
                WriteInterface<IPEGI>();

                "Interface that asks you to implement Inspect(). Interface is used by PEGI_Inspector_Override to override default inspector.".PegiLabel().WriteBig();

                WriteInterface<IPEGI_ListInspect>();

                "A short, preferably one-line version of the inspector for the class. Automatically used by {0} function, but can also be called manuall.".F(nameof(pegi.Edit_List)).PegiLabel().WriteBig();

                WriteInterface<IGotName>();
                WriteInterface<IGotIndex>();
                WriteInterface<IGotCount>();
                WriteInterface<IPEGI_Handles>();

                static void WriteInterface<T>() 
                {
                    typeof(T).ToPegiStringType().PegiLabel(style: pegi.Styles.BaldText).Write_ForCopy(writeAsEditField: true).Nl();
                }

            }
        }


        private class LabelEntry : DocEntry
        {
            public override string ToString() => "Pegi Label";

            private string Test_Text = "Test_Text";
            private int _testInt = 12345;

            public override void Inspect_About()
            {
                "{0} extension creates a structure that includes text with optional: width, tooltip & GuiStyle. As arguments you can add tooltip, width and GuiStyle. Most other pegi functions are extensions on top of this fucntions.".F(nameof(pegi.PegiLabel)).PegiLabel().Write_Hint().Nl();
                "SomeText.PegiLabel(toolTip, width, stype);".PegiLabel(toolTip: "tip", width: 310, style: pegi.Styles.BaldText).Nl();

                DrawExample(() => "label".PegiLabel().Write(),
                                    "\"label\".PegiLabel().Write();");

                DrawExample(() => "label".PegiLabel(toolTip: "tooltip").Edit(ref Test_Text),
                                    "\"label\".PegiLabel(toolTip: tooltip).Edit(ref Test_Text);");

                DrawExample(() => "label".PegiLabel(toolTip: "tooltip", width: 50, style: pegi.Styles.BaldText).Select(ref exampleKey, exampleDictionary),
                                    "\"label\".PegiLabel(toolTip: tooltip, width: 50, style: pegi.Styles.BaldText).Select(ref exampleKey, exampleDictionary);");

                "If label text will not change and there are more elements that need space next to it, consider using ConstLabel (added recently)".PegiLabel(pegi.Styles.BaldText).Nl();

                DrawExample(() => "const label".ConstLabel(toolTip: "toolTip").Edit(ref _testInt),
                                    "\"const label\".ConstLabel(toolTip: \"toolTip\").Edit(ref _testInt);");

                DrawExample(() =>
                {
                    "const very long label nefore button".ConstLabel(toolTip: "toolTip").Edit(ref _testInt);
                    if ("Button".PegiLabel().Click())
                        Debug.Log("Clicking");
                },
                                   "\"const very long label nefore button\".ConstLabel(toolTip: \"toolTip\").Edit(ref _testInt);");
            }
        }

        private class WriteEntry : DocEntry, ISearchable
        {
            public override string ToString() => "Write";

            public override void Inspect_About()
            {
                "Label Example 1".PegiLabel(width: 120).Write();
                "Label Example 2".PegiLabel().Click().OnChanged(()=> Debug.Log("Click!"));
                pegi.Nl();
                "Label Example 3".PegiLabel().Write();


                new CodeStringBuilder()
                   .AppendLine("label1.PegiLabel(width: 120).write();")
                   .AppendLine("label2.PegiLabel().Click().OnChanged(()=> Debug.Log(clickText));")
                   .AppendLine("pegi.nl();")
                   .AppendLine("label3.PegiLabel().write();")
                   .DrawExample();

                pegi.Line();

                "Label Example 2".PegiLabel(toolTip: "description text", width: 90, pegi.Styles.BaldText).Write().Nl();

                new CodeStringBuilder()
                   .AppendLine("label2.PegiLabel(toolTip: description, width: 90, pegi.Styles.BaldText).write().nl()")
                   .DrawExample();

                pegi.Line();

                "Handling Long Text".PegiLabel(pegi.Styles.HeaderText).Nl();

                ("Line 1 write {0}" + "Line 2 write {0}" + "Line 3 write {0}").F(pegi.EnvironmentNl).PegiLabel().Write();
                pegi.Nl();

                nameof(pegi.WriteBig).PegiLabel(pegi.Styles.BaldText).Nl();
                ("Line 1 writeBig {0}" + "Line 2 writeBig {0}" + "Line 3 writeBig {0}").F(pegi.EnvironmentNl).PegiLabel().WriteBig();
                
            }


            protected override List<FunctionData> GetFunctions() => functions;

            private readonly List<FunctionData> functions = new()
            {
                new FunctionData(nameof(pegi.Write), ReturnType.TextToken,
                    about: "Writes the provided text.",
                    extendsPegiLabel: true),

                new FunctionData(nameof(pegi.WriteBig), ReturnType.TextToken,
                    about: "Writes the provided text but wraps it and extends the element size to fit all all of the text.",
                    extendsPegiLabel: true),
            };

            private readonly List<string> searchBy = new() { "write", "writeBig", "text", "label", "string", "draw"};

            public override IEnumerator SearchKeywordsEnumerator()
            {
                yield return searchBy;
                yield return base.SearchKeywordsEnumerator();
            }
        }

        private class NewLineEntry : DocEntry
        {
            public override string ToString() => "New Line";

            public override void Inspect_About()
            {
                "nl() is short for New Line. By default all the elements are placed in a single line.{0} Use nl() to make following elements drawn from a new line. {0}".F(nameof(pegi.EnvironmentNl)).PegiLabel().Write_Hint().Nl();
            }

            protected override List<FunctionData> GetFunctions() => functions;

            private readonly List<FunctionData> functions = new()
            {
                new FunctionData(nameof(pegi.Nl), ReturnType.SameReturnType,
                    about: "Makes sure that the next elements is drawn from a new line.",
                    extendsPegiLabel: false),
            };

        }

        private class ChangesEntry : DocEntry
        {
            public override string ToString() => "Changes";

            public override void Inspect_About()
            {
                (" . {0}" +
                 "  {0}"
                    ).F(pegi.EnvironmentNl).PegiLabel().WriteBig();
            }


            protected override List<FunctionData> GetFunctions() => functions;

            private readonly List<FunctionData> functions = new()
            {
                new FunctionData(nameof(pegi.ChangeTrackStart), ReturnType.ChangesTracker,
                    about: "Creates a tracker that can be cast to bool with value True if any changes were made since it's creation.",
                    extendsPegiLabel: false),
            };
        }

        private class EditEntry : DocEntry
        {
            public override string ToString() => "Edit";

            public override void Inspect_About()
            {
                ("A field to edit most common value types, such as: double, float, long, int, string, Vector2, Vector3, Vector4, Quaternion, Color, Color32, Rect {0}" +
                    "To edit bool and enum use toggle & editEnum respectively. {0}" +
                    "To edit Lists, Arrays and Dictionaries go to Collections entry").F(pegi.EnvironmentNl).PegiLabel().Write_Hint().Nl();
            }

            protected override List<FunctionData> GetFunctions() => functions;
          
            private readonly List<FunctionData> functions = new()
            {
                new FunctionData(nameof(pegi.Edit), ReturnType.Changes, 
                    about: "Allows to edit value with a slider to interpolate between min & max value, inclusively.", 
                    extendsPegiLabel: true,
                    new Parameter(name: "ref int value", description: "A value to edit", optional: false),
                    new Parameter(name: "minInclusive", description: "A minimum value", optional: true),
                    new Parameter(name: "maxInclusive", description: "A maximum value.", optional: true)),

                 new FunctionData(nameof(pegi.Edit_Delayed), ReturnType.Changes, 
                     about: "Changes to this field aren't registered  until user presses Enter or unfocuses from the field. Useful for when you don't want to react to change until it is finalized.",
                      extendsPegiLabel: true,
                    new Parameter(name: "ref value", description: "A value to edit", optional: false),
                    new Parameter(name: "min", description: "A minimum value", optional: true),
                    new Parameter(name: "max", description: "A maximum value.", optional: true)),

                new FunctionData(nameof(pegi.Toggle), ReturnType.Changes, 
                    about: "A field for boolean value type",
                     extendsPegiLabel: true,
                    new Parameter(name: "ref value", description: "A value to edit", optional: false)),

                 new FunctionData(nameof(pegi.Edit_Enum), ReturnType.Changes, 
                     about: "A field for enum value type",
                      extendsPegiLabel: true,
                    new Parameter(name: "ref value", description: "A value to edit", optional: false)),

                 new FunctionData(nameof(pegi.Edit_01), ReturnType.Changes,
                     about: "Edit within range",
                      extendsPegiLabel: true,
                    new Parameter(name: "ref value", description: "A value to edit", optional: false)),
            };

            private readonly List<string> searchBy = new() { "enum", "bool", "double", "float", "long", "int", "string", "Vector2", "Vector3", "Vector4", "Quaternion", "Color", "Color32", "Rect" };

            public override IEnumerator SearchKeywordsEnumerator()
            {
                yield return searchBy;
                yield return base.SearchKeywordsEnumerator();
            }

        }

        private class ClickEntry : DocEntry
        {
            public override string ToString() => "Click";

            public override void Inspect_About()
            {
                

                if ("Button".PegiLabel().Click())
                {
                    Debug.Log("Clicking");
                }

                var sb = new CodeStringBuilder();

                sb.AppendLine(" if (buttonName.PegiLabel().Click())")
                    .In().AppendLine("Debug.Log(message)").Out()
                    .DrawExample();

                pegi.Nl();
            }

            protected override List<FunctionData> GetFunctions() => functions;

            private readonly List<FunctionData> functions = new()
            {
                new FunctionData(nameof(pegi.Click), ReturnType.Click,
                    about: "Creates a button with a text. Returns True if button was clicked.",
                     extendsPegiLabel: true),

                 new FunctionData(nameof(pegi.ClickConfirm), ReturnType.Click,
                     about: "Creates a button with a pext. Asks for confirmation before returning True.",
                      extendsPegiLabel: true,
                    new Parameter(name: "confirmationTag", description: "A unique tag for the button. It should be consistent for the same button and not duplicated between buttons.", optional: false),
                     new Parameter(name: "toolTip", description: "A tooltip to provide more information. Will be displayed after button is clicked but not yet confirmed.", optional: true)),

                  new FunctionData(nameof(pegi.ClickLabel), ReturnType.Click,
                     about: "Creates a button that looks like a text with special formating. Will use pegi.Style if provided with PegiLabel.",
                       extendsPegiLabel: true),

                  new FunctionData(nameof(pegi.ClickUnFocus), ReturnType.Click,
                     about: "After click unfocuses any UI element. Use this when UI or editable data is expected to change after the click. For example, a Reset buton that sets default values on all the fields.",
                    extendsPegiLabel: true),
            };

            private readonly List<string> searchBy = new() { "Click", "Function", "Action" };

            public override IEnumerator SearchKeywordsEnumerator()
            {
                yield return searchBy;

                yield return base.SearchKeywordsEnumerator();
            }


        }

        private class NestedInspectEntry : DocEntry
        {
            public override string ToString() => "Nested Inspect";

            public override void Inspect_About()
            {
                "Use NestedInspect when inspecting another class. This makes sure to set Dirty is the target class is a UnityObject and requires serialization.".PegiLabel().WriteBig();
            }
        }

        private class EnterEntry : DocEntry, ISearchable
        {
            public override string ToString() => "Enter";

            private int _foldedOutIndex = -1;
            private readonly pegi.EnterExitContext _enteredIndex = new();

            public override void Inspect_About()
            {
                "{0} helps to create a menu. Replaces traditional FoldOut. Create context where Enter/Exit elements will be mutually exclusive.".F(nameof(pegi.EnterExitContext)).PegiLabel().WriteBig();

                "Using Foldouts".PegiLabel(pegi.Styles.ListLabel).Nl();

                "Element A".PegiLabel().IsFoldout(ref _foldedOutIndex, 0).Nl().If_Entered(() => WriteStuff("A"));
                "Element B".PegiLabel().IsFoldout(ref _foldedOutIndex, 1).Nl().If_Entered(() => WriteStuff("B"));
                "Element C".PegiLabel().IsFoldout(ref _foldedOutIndex, 2).Nl().If_Entered(() => WriteStuff("C"));


                "Using Enter".PegiLabel(pegi.Styles.ListLabel).Nl();

                using (_enteredIndex.StartContext())
                {
                    "Element A".PegiLabel().IsEntered().Nl().If_Entered(() => WriteStuff("A"));
                    "Element B".PegiLabel().IsEntered().Nl().If_Entered(() => WriteStuff("B"));
                    "Element C".PegiLabel().IsEntered().Nl().If_Entered(() => WriteStuff("C"));
                }

                var sb = new CodeStringBuilder();
                sb.AppendLine(" private readonly pegi.{0} _enteredIndex = new pegi.{0}();".F(nameof(pegi.EnterExitContext)))
                    .AppendLine()
                    .AppendLine("// Inspect(): ")
                    .AppendLine()
                    .AppendLine("using (_enteredIndex.StartContext())")
                    .AppendLine('{')
                    .Indent().AppendLine("labelA.PegiLabel().isEntered().nl().If_Entered(() => WriteStuff(text));")
                    .AppendLine("labelB.PegiLabel().isEntered().nl().If_Entered(() => WriteStuff(text));")
                    .AppendLine("labelC.PegiLabel().isEntered().nl().If_Entered(() => WriteStuff(text));")
                    .UnIndent().AppendLine('}')


                    .DrawExample();


                static void WriteStuff(string label) 
                {
                    using(pegi.Indent())
                    {
                        "Some stuff {0}".F(label).PegiLabel().Write().Nl();
                    }
                }

            }


            protected override List<FunctionData> GetFunctions() => functions;

            private readonly List<FunctionData> functions = new()
            {
               

                new FunctionData(nameof(pegi.IsEntered), ReturnType.State, about: "A button & A clickable text. Element is hiddent if another element from this context is entered/foldedOut and returns False.",
                     extendsPegiLabel: true,
                    new Parameter(name: "bool showLabelIfTrue", description: "If element is entered, it's label will be hidden. Useful when entered data group also writes a title.", optional: true))
            };

            private readonly List<string> searchBy = new() { "context", "entered", "exited", "folded", "hide", "isEntered", "menu", "sections"};

            public override IEnumerator SearchKeywordsEnumerator()
            {
                yield return searchBy;

                yield return base.SearchKeywordsEnumerator();
            }
        }

        private class SelectEntry : DocEntry
        {
            public override string ToString() => "Select";

            public override void Inspect_About()
            {
                "Index".PegiLabel(50).Edit(ref exampleIndex).Nl();

                "Select Index".PegiLabel(width: 90).Select_Index(ref exampleIndex, exampleList).Nl();

                "Key".PegiLabel(50).Edit(ref exampleKey).Nl();

                "Select Key from Dictionary".PegiLabel(170).Select(ref exampleKey, exampleDictionary).Nl();

            }


            protected override List<FunctionData> GetFunctions() => functions;

            private readonly List<FunctionData> functions = new()
            {
                new FunctionData(nameof(pegi.Select), ReturnType.Changes, about: "Allow to select an element from a collection of elements.",
                     extendsPegiLabel: true,
                    new Parameter(name: "ref T value", description: "The value to change.", optional: false), 
                    new Parameter(name: " List<T> collection", description: "The collection to select value from.", optional: false)),

                new FunctionData(nameof(pegi.Select_Index), ReturnType.Changes, about: "Allow to change index of an element the reflects an element from the collection.",
                     extendsPegiLabel: true,
                    new Parameter(name: "ref int value", description: "The index to change.", optional: false),
                    new Parameter(name: " List<T> collection", description: "The collection to select value from.", optional: false))
            };

        }


        private class CollectionEntry : DocEntry
        {
            public override string ToString() => "Collection";

            public override void Inspect_About()
            {
                "edit_List, edit_Dictionary".PegiLabel().Nl();
            }
        }

        private class DrawEntry : DocEntry, ISearchable
        {
            public override string ToString() => "Draw";

            public override void Inspect_About()
            {
                "Draw fucntion allows to display textures, sprites and some pegi icons".PegiLabel().WriteBig().Nl();
            }

            protected override List<FunctionData> GetFunctions() => functions;

            private readonly List<FunctionData> functions = new()
            {
                new FunctionData(nameof(pegi.Draw), ReturnType.Changes,
                    about: "Draws a sprite.",
                    extendsPegiLabel: false,
                    new Parameter(name: "this Sprite sprite", description: "A sprite to raw"),
                    new Parameter(name: "alphaBlend", description: "if True, alpha channel will be displayed as transparency", optional: true)
                    ),
                new FunctionData(nameof(pegi.Draw), ReturnType.Changes,
                    about: "Draws an icon.",
                    extendsPegiLabel: false,
                    new Parameter(name: "this icon pegiIcon", description: "An icon enum to draw")
                    ),
            };

            private readonly List<string> searchBy = new() { "texture", "sprite", "icon" };

            public override IEnumerator SearchKeywordsEnumerator()
            {
                yield return searchBy;

                yield return base.SearchKeywordsEnumerator();
            }
        }

        private class ReturnTypesEntry : DocEntry
        {
            public override string ToString() => "Return Types";

            public override void Inspect_About()
            {
                "edit_List, edit_Dictionary".PegiLabel().Nl();
            }
        }


    }
}