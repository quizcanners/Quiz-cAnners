using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public class C_EnumeratedSingleton : MonoBehaviour, IPEGI
    {
        [SerializeField] private SingletonRole _role;
        [SerializeField] private SingletonConflictSolution _conflictResolve = SingletonConflictSolution.DestroyThis;
        [SerializeField] private GameObject _child;

        private enum SingletonConflictSolution 
        {
            DestroyThis, DestroyPrevious
        }

        public enum SingletonRole 
        { 
            MainCamera = 0, 
            Canvas = 1,
            Player = 2,
            DirectionalLight = 3,
            EventSystem = 4,
            EffectManagers = 5,
            AudioListener = 6,
            //UiCamera = 7, // For Ui Camera use C_UiCameraForEffectsManagement
        }

        public static bool TryGet(SingletonRole role, out GameObject result) => _inTheScene.TryGetValue(role, out result) && result;

        private static readonly Dictionary<SingletonRole, GameObject> _inTheScene = new();

        private void Awake()
        {
            CheckSingleton();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            CheckSingleton();
        }
#endif

        private void CheckSingleton() 
        {
            if (_inTheScene.TryGetValue(_role, out GameObject go) && go && go != gameObject)
            {
                switch (_conflictResolve) 
                {
                    case SingletonConflictSolution.DestroyThis: Destroy(gameObject); break;
                    case SingletonConflictSolution.DestroyPrevious: Destroy(go);
                        InitializeThis(); break;
                }
            }
            else
            {
                InitializeThis();
            }

            void InitializeThis() 
            {
                _inTheScene[_role] = gameObject;
                if (_child)
                    _child.SetActive(true);
            }
        }

        void IPEGI.Inspect()
        {
            pegi.Nl();

            "Role".ConstL().Edit_Enum(ref _role).Nl();
            "On Conflict".ConstL().Edit_Enum(ref _conflictResolve).Nl();

            "Object to enable (Optional)".ConstL().Edit(ref _child);



            if (_child)
            {
                var name = QcSharp.AddSpacesToSentence(_role.ToString());

                if (!_child.name.Equals(name) && Icon.Refresh.Click("Assign Name by Role"))
                    _child.name = name;
            }
            pegi.Nl();

            if (!Application.isPlaying && _child) 
            {
                if (gameObject == _child)
                {
                    "Should be child of this object".PL().WriteWarning().Nl();
                }
                else
                {
                    if (!_child.transform.IsChildOf(transform))
                        "Child is not a child of this object. Could be a mistake".PL().WriteWarning().Nl();

                    if (_child.activeSelf)
                    {
                        "Child object probably needs to be disabled".PL().WriteWarning().Nl();
                        if ("Disable".PL().Click().Nl())
                            _child.SetActive(false);
                    }
                }
            }
        }
    }

    [PEGI_Inspector_Override(typeof(C_EnumeratedSingleton))] internal class C_GameObjectSingletonDrawer : PEGI_Inspector_Override { }
}
