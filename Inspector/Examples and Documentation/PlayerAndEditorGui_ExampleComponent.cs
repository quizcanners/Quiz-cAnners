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

    public class PlayerAndEditorGui_ExampleComponent : MonoBehaviour, IPEGI
    {
        bool showInspectorInTheGameView;
        [SerializeField] private PlayerAndEditorGui_ExampleNested someOtherScript;

        //You should attach this script to the game object and see the example
        private readonly pegi.EnterExitContext _menuContext = new pegi.EnterExitContext();
        private readonly pegi.EnterExitContext _examplesSubContext = new pegi.EnterExitContext();
        public void Inspect()
        {
            pegi.Nl();

            using (_menuContext.StartContext())
            {
                "Documentation".PegiLabel().IsEntered().Nl().If_Entered(() => PlayerAndEditorGui_Documentation.Inspect());
                   
                if ("Example".PegiLabel().IsEntered().Nl())
                {
                    using (_examplesSubContext.StartContext())
                    {
                        if (_examplesSubContext.IsAnyEntered == false)
                        {
                            Icon.Debug.Draw();
                            " - this icon at the top switches to Default Unity inspector".PegiLabel().Nl();
                            "Open this script to learn how what code to use to display the elements.".PegiLabel().Write_Hint().Nl();
                        }

                        "GameView OnGUI Inspector".PegiLabel().IsEntered().Nl().If_Entered(() =>
                        {
                            "Inspector visible in the game view".PegiLabel().Toggle(ref showInspectorInTheGameView);
                            pegi.Nl();

                            if (showInspectorInTheGameView)
                                "Inspector size".PegiLabel(70).Edit(ref OnGUIWindow.Upscale, min: 1, max: 3).Nl();
                        });

                        if ("Nested Inspect".PegiLabel().IsEntered().Nl())
                        {
                            if (!someOtherScript)
                            {
                                "Nested component not found".PegiLabel().WriteWarning();
                                pegi.Nl();

                                if ("Search for Component".PegiLabel().Click().Nl())
                                {
                                    someOtherScript = GetComponent<PlayerAndEditorGui_ExampleNested>();
                                    if (!someOtherScript)
                                        Debug.Log("One is not attached. Please click Create");
                                }

                                if ("Attach component".PegiLabel().Click().Nl())
                                {
                                    someOtherScript = gameObject.AddComponent<PlayerAndEditorGui_ExampleNested>();
                                }
                            }
                            else
                            {
                                someOtherScript.Nested_Inspect().Nl(); // Always use NestedInspect(); when referencing Inspect(); function of another UnityEngine.Object,
                                                                       // otherwise changes may not be serialized.
                            }
                        }
                    }

                    if ("I will not be seen".PegiLabel().IsEntered().Nl())
                    {
                        //This will not be visible, as this section will use _menuContext, which already has index of Example Section.
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
    [PEGI_Inspector_Override(typeof(PlayerAndEditorGui_ExampleComponent))] internal class InspectEXAMPLE_DOCDrawer : PEGI_Inspector_Override { }


}