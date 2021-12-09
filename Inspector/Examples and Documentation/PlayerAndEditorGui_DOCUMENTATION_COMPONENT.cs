/* ATTACH THIS SCRIPT TO ANY OBJECT TO START EXPLORING THE INSPECTOR
 *
 * Player & Editor GUI, further referenced as PEGI is a wrapper of Unity's native EditorGuiLayout & GUILayout systems.
 * GitHub: https://github.com/quizcanners/Tools/tree/master/Playtime%20Painter/Scripts/quizcanners  (GNU General Public License)
 * I used and developed it since around 2016 and the goal is to simply write inspectors as effortlessly as possible.
 */

using System;
using UnityEngine;

namespace QuizCanners.Inspect.Examples
{

    public class PlayerAndEditorGui_DOCUMENTATION_COMPONENT : MonoBehaviour, IPEGI
    {
        int testValue;
        bool showInspectorInTheGameView;
        [SerializeField] private PlayerAndEditorGui_ExampleNested someOtherScript;

        //You should attach this script to the game object and see the example
        private pegi.EnterExitContext _menuContext = new pegi.EnterExitContext();
        private pegi.EnterExitContext _examplesSubContext = new pegi.EnterExitContext();
        public void Inspect()
        {
            pegi.nl();

            using (_menuContext.StartContext())
            {
                "Documentation".PegiLabel().isEntered().nl().If_Entered(() => PlayerAndEditorGui_Documentation.Inspect());
                   
                if ("Example".PegiLabel().isEntered().nl())
                {
                    using (_examplesSubContext.StartContext())
                    {
                        if (_examplesSubContext.IsAnyEntered == false)
                            "Open this script. Use debug button to see default inspector for this script".PegiLabel().writeHint().nl();

                        "GameView OnGUI Inspector".PegiLabel().isEntered().nl().If_Entered(() =>
                        {
                            "Inspector visible in the game view".PegiLabel().toggle(ref showInspectorInTheGameView);
                            pegi.nl();

                            if (showInspectorInTheGameView)
                                "Inspector size".PegiLabel(70).edit(ref OnGUIWindow.Upscale, min: 1, max: 3).nl();
                        });

                        if ("Script InspectCeptions".PegiLabel().isEntered().nl())
                        {
                            if (!someOtherScript)
                            {
                                "Nested component not found".PegiLabel().writeWarning();
                                pegi.nl();

                                if ("Search for Component".PegiLabel().Click().nl())
                                {
                                    someOtherScript = GetComponent<PlayerAndEditorGui_ExampleNested>();
                                    if (!someOtherScript)
                                        Debug.Log("One is not attached. Please click Create");
                                }

                                if ("Attach component".PegiLabel().Click().nl())
                                {
                                    someOtherScript = gameObject.AddComponent<PlayerAndEditorGui_ExampleNested>();
                                }
                            }
                            else
                            {
                                someOtherScript.Nested_Inspect().nl(); // Always use NestedInspect(); when referencing Inspect(); function of another UnityEngine.Object,
                                                                       // otherwise changes may not be serialized.
                            }
                        }
                    }
                }
            }
        }

        //Optional: To Render in game view
        private static readonly pegi.GameView.Window OnGUIWindow = new pegi.GameView.Window();
        public void OnGUI()
        {
            if (showInspectorInTheGameView)
                OnGUIWindow.Render(this);
        }

    }

    //Optional: To Override Unity's default inspector for this component
    [PEGI_Inspector_Override(typeof(PlayerAndEditorGui_DOCUMENTATION_COMPONENT))] internal class InspectEXAMPLE_DOCDrawer : PEGI_Inspector_Override { }


}