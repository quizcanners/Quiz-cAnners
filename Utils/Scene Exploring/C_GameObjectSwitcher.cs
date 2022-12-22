using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public class C_GameObjectSwitcher : MonoBehaviour, IPEGI
    {
        [SerializeField] private List<GameObject> _targets = new();
        [SerializeField] private int _index;
        [SerializeField] private bool _autoswitch;
        [SerializeField] private float _autoswitchDelay;

        private readonly Gate.UnityTimeUnScaled _switchDelay = new(initialValue: Gate.InitialValue.Uninitialized);

        void Update() 
        {
            if (_autoswitch && _switchDelay.TryUpdateIfTimePassed(_autoswitchDelay)) 
            {
                SetNext();
            }
        }

        void SetNext() 
        {
            _index = (_index + 1) % _targets.Count;
            UpdateTargets();
        }

        void UpdateTargets() 
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i])
                {
                    _targets[i].SetActive(_index == i);
                }
                else if (_index == i)
                {
                    _index++;
                }
            }
        }

        public void Inspect()
        {
            "Switch".PegiLabel().Click(SetNext).Nl();

            "Autoswitch".PegiLabel().ToggleIcon(ref _autoswitch, hideTextWhenTrue: true);

            if (_autoswitch)
                "Autoswitch Delay:".PegiLabel().Edit(ref _autoswitchDelay);

            pegi.Nl();

            "Elements".PegiLabel().Edit_List_UObj(_targets).Nl();

        }
    }

    [PEGI_Inspector_Override(typeof(C_GameObjectSwitcher))] internal class C_GameObjectSwitcherDrawer : PEGI_Inspector_Override { }
}
