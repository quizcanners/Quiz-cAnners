using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect.Examples
{
    internal static class PlayerAndEditorGui_Documentation
    {
        private static readonly pegi.EnterExitContext _context = new pegi.EnterExitContext();

        public static void Inspect()
        {
            if (_context.IsAnyEntered == false)
            {
                pegi.nl();
                "PEGI DOCUMENTATION MENU".PegiLabel(style: pegi.Styles.ListLabel).nl();

                "Open this script:".PegiLabel().nl();
                nameof(PlayerAndEditorGui_Documentation).PegiLabel().write_ForCopy(showCopyButton: true).nl();

            }

            using (_context.StartContext())
            {
                
                if ("PEGI inspector short description".PegiLabel().isEntered().nl()) 
                {
                    ("It streamlines the workflow of using Unity's native EditorGui/Gui to create custom inspector. {0}" +
                        " Not based on serialization - lets you edit static classes {0}" +
                        " Isn't using reflection {0}" +
                        " Can be used in Editor's Inspector window and in GameView {0}" +
                        " With PEGI, any class can contain a user(developer) interface in addition to Data & Functions {0}" +
                        " Helps provide a point of entry to test functionality and inspect any data {0}" +
                        " In it's toolbox it helps make focus assisting inspector - represent only the data needed and in the convenient format {0}").F(pegi.EnvironmentNl).PegiLabel().nl();
                }

                if ("write, nl".PegiLabel().isEntered().nl())
                {
                    "PegiL() extension provides means to convert a string into Label(). As arguments you can add tooltip, width and GuiStyle".PegiLabel().writeHint().nl();

                    string text = "Label Test 1";
                    pegi.TextLabel label = pegi.PegiLabel(text, toolTip: "The two examples look the same in the inspector. But the second is shorter", width: 90, style: pegi.Styles.BaldText);
                    label.write();
                    pegi.nl();

                    "Label Test 2".PegiLabel(toolTip: "This is a shorter example", width: 90, pegi.Styles.BaldText).write().nl();
                }

                if ("edit, toggle".PegiLabel().isEntered().nl())
                {
                    "pegi.edit/PegiLabel.edit is used for all your editing needs. pegi.toggle/PegiLabel.toggle is a small exeption for boolean value".PegiLabel().writeHint().nl();
                }

                if ("click".PegiLabel().isEntered().nl())
                {
                    "pegi.Click(Button Label)/PegiLabel.Click() are used to execute a function".PegiLabel().nl();

                    if (pegi.Click("pegi.Click(\" Button name \")".PegiLabel()))
                    {
                        Debug.Log("Clicking");
                    }

                    pegi.nl();
                }

                // Some sugar:
                pegi.StateToken Enter(string label) => label.PegiLabel().isEntered().nl();


                if (Enter("Nested_Inspect()"))
                {
                    "Use NestedInspect when inspecting another class. This makes sure to set Dirty is the target class is a UnityObject and requires serialization.".PegiLabel().writeBig();
                }


                if (Enter("Enter Exit Context"))
                {

                }


                if (Enter("edit_List, edit_Dictionary"))
                {

                }

                
            }
        }
    }
}