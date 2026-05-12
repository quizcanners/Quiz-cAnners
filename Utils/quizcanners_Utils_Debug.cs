using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuizCanners.Utils
{
    public static partial class QcDebug
    {
        private static bool _forceDebug = false; // To have Debug Access in a build
        private static readonly PlayerPrefValue.Bool _emultateRelease = new("qc_emlRls", defaultValue: false); // To enable Release functionality in Debug

        public static bool IsRelease
        {
            get => (!ShowDebugOptions) || _emultateRelease.GetValue();
            set
            {
                _emultateRelease.SetValue(value);
            }
        }
        public static void ForceDebugOption() => _forceDebug = true;
        public static bool ShowDebugOptions
        {  
            get => _forceDebug || Debug.isDebugBuild;
        }

        private static readonly OtherStuffInspector _inspector = new();


        public static void InspectOther() 
        {
            _inspector.Nested_Inspect();
        }

        private class OtherStuffInspector : IPEGI
        {
            private readonly Migration.ICfgObjectExplorer iCfgExplorer = new();
            private readonly EncodedJsonInspector jsonInspector = new();
            private int _testSeed = 42;

            private readonly pegi.EnterExitContext _context = new(playerPrefId: "utlsDbCtx");
            private readonly pegi.EnterExitContext _testsContext = new(playerPrefId: "qcDbgTst");
            private readonly pegi.CollectionInspectorMeta _blockersMeta = new();

            private string _testString;
            private string _testRegex;

            void IPEGI.Inspect()
            {
                using (_context.StartContext())
                {
                    "Json Inspector".PL().Enter_Inspect(jsonInspector).NL();

                    if ("ICfg Inspector".PL().IsEntered().NL())
                        iCfgExplorer.Inspect(null);

                    if ("Blockers [0]".F(InspectableBlock.allBlocks.Count).PL().IsEntered().NL()) 
                    {
                        _blockersMeta.Edit_List(InspectableBlock.allBlocks).NL();

                        if ("Create Test Blocks".PL().Click().NL()) 
                        {
                            new InspectableBlock("Test A");
                            new InspectableBlock("Test B");
                            new InspectableBlock("Test C");
                        }
                    }

                    if ("Managed Coroutines [{0}]".F(QcAsync.DefaultCoroutineManager.GetActiveCoroutinesCount).PL().IsEntered().NL())
                        QcAsync.DefaultCoroutineManager.Nested_Inspect();

                    if ("Tests".PL().IsEntered().NL())
                    {
                        using (_testsContext.StartContext())
                        {
                            if ("Gui Styles".PL().IsEntered().NL())
                            {
                                pegi.Styles.Inspect();
                                pegi.NL();
                            }

                            if ("Random Seed Test".PL().IsEntered().NL())
                            {
                                "Seed".ConstL().Edit(ref _testSeed).NL();

                                using (QcMath.RandomBySeedDisposable(_testSeed))
                                {
                                    for (int i = 0; i < 4; i++)
                                        "Value {0}: {1}".F(i, UnityEngine.Random.value * 100).NL();
                                }

                                using (QcMath.RandomBySeedDisposable(_testSeed))
                                {
                                    for (int i = 0; i < 4; i++)
                                        "B Value {0}: {1}".F(i, UnityEngine.Random.value * 100).NL();
                                }
                            }

                            if ("Probability Calculator".PL().IsEntered().NL())
                            {
                                Percentages = QcMath.NormalizeToPercentage(probabilities, prob => prob.Chances);
                                "Probabilities".PL().Edit_List(probabilities).NL();
                            }
                        }
                    }

                    if ("Scenes".PL().IsEntered().NL()) 
                    {
                        for (int i=0; i< SceneManager.sceneCount; i++) 
                        {
                            var s = SceneManager.GetSceneAt(i);
                            if (s.isLoaded)
                                Icon.Done.Draw();
                            s.path.NL();
                        }
                    }

                    if ("Regex tests".PL().IsEntered().NL()) 
                    {
                        "Text".ConstL().Edit_Big(ref _testString).NL();
                        "Pattern".ConstL().Edit(ref _testRegex).NL();

                        if (!_testString.IsNullOrEmpty() && !_testRegex.IsNullOrEmpty())
                        {

                            // const string pattern = @"\b\d{5}\s\d{5}\b";
                            Regex regex = new(_testRegex);

                            //MatchCollection matches = regexp.Matches(_testString);
                            MatchCollection matches = regex.Matches(_testRegex);

                            for (int i = 0; i < matches.Count; i++)
                            {
                                var m = matches[i];
                                m.Value.NL();
                            }
                        }

                    }

                    if (_context.IsAnyEntered == false)
                    {
                        var release = IsRelease;
                        if ("Release".PL().ToggleIcon(ref release).NL())
                            IsRelease = release;
                    }
                }
            }
        }

        public class InspectableBlock : IPEGI, IPEGI_ListInspect, INeedAttention
        {
            internal static List<InspectableBlock> allBlocks = new();

            private string _loadingStage = "Unknown";
            private bool _blocked;
            private readonly bool _disabled;
            public bool IsBlocked => _blocked;

            public bool Unblock() 
            {
                var wasBlocked = _blocked;
                _blocked = false;
                allBlocks.Remove(this);
                return wasBlocked;
            }

            private void BlockInternal(string loadingStage)
            {
                if (_blocked)
                {
                    Debug.LogError("Blocker was already started by {0}".F(_loadingStage));
                }
                else
                {
                    allBlocks.Add(this);
                    _blocked = true;
                }

                _loadingStage = loadingStage;
            }

            public async Task StartBlock(string loadingStage)
            {
                if (_disabled)
                    return;

                BlockInternal(loadingStage);

                while (_blocked)
                {
                    await Task.Yield();
                }

                Unblock();
            }

            #region Inspector

            private static readonly pegi.CollectionInspectorMeta _context = new();

            public static void InspectIfAny() 
            {
                if (allBlocks.Count > 0) 
                {
                    "Blocking In Progress".PL().WriteWarning().NL();

                    _context.Edit_List(allBlocks).NL();
                }
            }

            void IPEGI.Inspect()
            {
                if (_blocked && "Load {0}".F(_loadingStage).PL().Click().NL())
                    _blocked = false;
            }

            public string NeedAttention()
            {
                if (_blocked)
                    return "Unblock me";
                return null;
            }

            public void InspectInList(ref int edited, int index)
            {
                if (_blocked && Icon.Play.Click())
                    Unblock();

                if (_loadingStage.PL().ClickLabel() | Icon.Enter.Click())
                    edited = index;
            }
            #endregion
            public InspectableBlock() { }

            public InspectableBlock(bool disable)
            {
                _disabled = disable;
            }

            public InspectableBlock(string blocker)
            {
                BlockInternal(blocker);
            }
        }

        #region Probability Calculator

        private static readonly List<Probability> probabilities = new();
        private static List<int> Percentages;

        private struct Probability : IPEGI_ListInspect
        {
            private string name;
            public double Chances;

            public void InspectInList(ref int edited, int index)
            {
                pegi.Edit(ref name);
                pegi.Edit(ref Chances);

                "= {0}%".F(Percentages[index].ToString()).NL();
            }
        }
        #endregion
    }
}
