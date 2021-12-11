using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class QcDebug
    {
        private static bool _forceDebug = false; // To have Debug Access in a build
        private static readonly PlayerPrefValue.Bool _emultateRelease = new PlayerPrefValue.Bool("qc_emlRls", defaultValue: false); // To enable Release functionality in Debug
       
        public static bool IsRelease
        {
            get => (!ShowDebugOptions) || _emultateRelease.GetValue();
            set
            {
                _emultateRelease.SetValue(value);
            }
        }
        public static void ForceDebugOption() => _forceDebug = true;
        public static bool ShowDebugOptions => (_forceDebug || Debug.isDebugBuild);

        private static readonly Migration.ICfgObjectExplorer iCfgExplorer = new Migration.ICfgObjectExplorer();
        private static readonly EncodedJsonInspector jsonInspector = new EncodedJsonInspector();
        private static readonly JsonTest jsonTest = new JsonTest();
        private static int _testSeed = 42;

        private static readonly pegi.EnterExitContext enterExitContext = new pegi.EnterExitContext(playerPrefId: "utlsDbCtx");

        public static void Inspect() 
        {
            using (enterExitContext.StartContext())
            {
                if ("Probability Calculator".PegiLabel().IsEntered().Nl())
                {
                    Percentages = QcMath.NormalizeToPercentage(probabilities, prob => prob.Chances);
                    "Probabilities".PegiLabel().Edit_List(probabilities).Nl();
                }

                if ("Random Seed Test".PegiLabel().IsEntered().Nl())
                {
                    "Seed".PegiLabel().Edit(ref _testSeed).Nl();

                    using (QcMath.RandomBySeedDisposable(_testSeed))
                    {
                        for (int i = 0; i < 4; i++)
                            "Value {0}: {1}".F(i, UnityEngine.Random.value * 100).PegiLabel().Nl();
                    }

                    using (QcMath.RandomBySeedDisposable(_testSeed))
                    {
                        for (int i = 0; i < 4; i++)
                            "B Value {0}: {1}".F(i, UnityEngine.Random.value * 100).PegiLabel().Nl();
                    }
                }

                "Json Inspector".PegiLabel().Enter_Inspect(jsonInspector).Nl();

                if ("ICfg Inspector".PegiLabel().IsEntered().Nl())
                    iCfgExplorer.Inspect(null);

                if ("Managed Coroutines [{0}]".F(QcAsync.DefaultCoroutineManager.GetActiveCoroutinesCount).PegiLabel().IsEntered().Nl())
                    QcAsync.DefaultCoroutineManager.Nested_Inspect();

                if ("Gui Styles".PegiLabel().IsEntered().Nl())
                {
                    pegi.Styles.Inspect();
                    pegi.Nl();
                }

                "Json Parcing".PegiLabel().Enter_Inspect(jsonTest).Nl();
                
                if (enterExitContext.IsAnyEntered == false)
                {
                    var release = IsRelease;
                    if ("Release".PegiLabel().ToggleIcon(ref release).Nl())
                        IsRelease = release;
                }
            }
        }

        private class JsonTest : IPEGI
        {
            private string _testJson = "{ \"Name\" : \"John\" , \"Age\" : 16, \"Logic\" : " +
                "   {" +
                "       \"Type\" : \"A\",  " +
                "       \"Data\" : " +
                "       \"{" +
                "        \\\"SecretData\\\" : \\\" Test\\\"" +
                "       }\"  " +
                "   }  " +
                "}";
            private readonly JsonTestClass _testParcedClass = new JsonTestClass();

            private class JsonTestClass : IPEGI
            {
                [SerializeField] string Name;
                [SerializeField] int Age;
                //[SerializeReference] 
                [SerializeField] private TestFactory Logic;

                public void Inspect()
                {
                    "Name".PegiLabel().Edit(ref Name).Nl();
                    "Age".PegiLabel().Edit(ref Age).Nl();

                    Logic.Nested_Inspect();
                }

                [Serializable] private class TestFactory : TypedInstance.JsonSerializable<Base> { }

                private class A : Base { }

                private class B : Base { }

                private abstract class Base : IPEGI
                {
                    [SerializeField] string SecretData;

                    public void Inspect()
                    {
                        "Secret Data".PegiLabel().Edit(ref SecretData).Nl();
                    }
                }
            }

            public void Inspect()
            {

                "Test Json".PegiLabel().EditBig(ref _testJson).Nl();

                try
                {
                    JsonUtility.FromJsonOverwrite(_testJson, _testParcedClass);

                    _testParcedClass.Nested_Inspect().Nl();


                    if ("Serialize".PegiLabel().Click().Nl())
                        Debug.Log(JsonUtility.ToJson(_testParcedClass));

                }
                catch (Exception ex)
                {
                    ex.ToString().PegiLabel().WriteWarning().Nl();
                }
            }

        }

        #region Probability Calculator

        private static readonly List<Probability> probabilities = new List<Probability>();
        private static List<int> Percentages;

        private struct Probability : IPEGI_ListInspect
        {
            private string name;
            public double Chances;

            public void InspectInList(ref int edited, int index)
            {
                pegi.Edit(ref name);
                pegi.Edit(ref Chances);

                "= {0}%".F(Percentages[index].ToString()).PegiLabel().Nl();
            }
        }
        #endregion
    }
}
