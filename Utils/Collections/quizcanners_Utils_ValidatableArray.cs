using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public abstract class ValidatabeArrayGeneric<T> : IPEGI, IGotCount, IEnumerable<T> where T: struct
    {
        [SerializeField] protected T[] _array;
        [SerializeField] private bool[] _valid = new bool[0];

        [SerializeField] private int _firstVacantElement;
        [SerializeField] private int _lastValidElement;
        [SerializeField] private int _validatedCount;

        public int Length => _array.Length;

        public bool IsValid(int index) => _valid[index];

        public T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        protected abstract T Revalidate(int index);

        public T Create(out int createdIndex)
        {
            _validatedCount++;

            if (_array == null)
            {
                _array = new T[8];
                _valid = new bool[8];
                createdIndex = 0;
                return Recreate(createdIndex);
            }

            for (int i = _firstVacantElement; i < _array.Length; i++)
            {
                if (!_valid[i])
                {
                    createdIndex = i;
                    return Recreate(createdIndex);
                }
            }

            int len = _array.Length;

            QcSharp.ExpandBy(ref _array, len);
            QcSharp.ExpandBy(ref _valid, len);

            createdIndex = len;
            return Recreate(createdIndex);

            T Recreate(int index)
            {
                _firstVacantElement = index + 1;
                _lastValidElement = Mathf.Max(index, _lastValidElement);
                _valid[index] = true;
                var ent = Revalidate(index);
                _array[index] = ent;
                return ent;
            }
        }

        public bool TryDestroy(int index) 
        {
            if (IsValid(index)) 
            {
                _validatedCount--;
                _valid[index] = false;
                _firstVacantElement = Mathf.Min(_firstVacantElement, index);
                if (_lastValidElement == index) 
                {
                    _lastValidElement--;
                    while (_lastValidElement > 0 && !IsValid(_lastValidElement))
                        _lastValidElement--;
                }
                return true;
            }

            return false;
        }

        #region Inspector

        protected virtual bool CanAddFromInspector => false;

        public virtual void TryDestroyFromInspectro(int index) => TryDestroy(index);

        [NonSerialized] private int _inspectStart = 0;
        [NonSerialized] private int _inspectedIndex = -1;

        public void Inspect()
        {
            "Vacant: {0}".F(_firstVacantElement).PegiLabel().Nl();
            "Last Valid: {0}".F(_lastValidElement).PegiLabel().Nl();

            const int SHOW_AT_ONCE = 30;

            if (_array != null)
            {
                if (_inspectedIndex >= 0 && IsValid(_inspectedIndex))
                {
                    var el = this[_inspectedIndex];

                    if (Icon.Exit.Click() | el.GetNameForInspector().PegiLabel().ClickLabel().Nl())
                        _inspectedIndex = -1;
                    else
                    {
                      
                        var pgi = el as IPEGI;

                        if (pgi != null)
                            pgi.Nested_Inspect().OnChanged(() => _array[_inspectedIndex] = (T)pgi);
                    }
                }
                else
                {
                    var last = Mathf.Min(_array.Length, _inspectStart + SHOW_AT_ONCE);

                    if (_inspectStart > 0 && Icon.Up.Click().Nl())
                        _inspectStart = Mathf.Max(0, _inspectStart - SHOW_AT_ONCE);

                    for (int i = _inspectStart; i < last; i++)
                    {
                        if (IsValid(i) && Icon.Delete.Click())
                             TryDestroyFromInspectro(i);

                        //i.ToReadableString().PegiLabel(20).Write();

                        //(IsValid(i) ? Icon.Active : Icon.InActive).Draw();
                        /*

                        if (i > _lastValidElement)
                            Icon.Off.Draw(toolTip: "Will not be checked");
                        else
                        if (i < _firstVacantElement)
                            Icon.Done.Draw(toolTip: "No vacant Entities before this one");*/

                        var el = _array[i];

                        pegi.InspectValueInArray(ref _array, i, ref _inspectedIndex);

                        pegi.Nl();
                    }

                    if (_inspectStart + SHOW_AT_ONCE < _array.Length && Icon.Down.Click().Nl())
                        _inspectStart += SHOW_AT_ONCE;
                }
            }
            else
                "Empty Array".PegiLabel().Nl();

            if (CanAddFromInspector && _inspectedIndex == -1 && "Create {0}".F(typeof(T).Name).PegiLabel().Click().Nl())
                Create(out _);
            

        }

        public override string ToString() => "{0} Array [{1}]".F(typeof(T).ToPegiStringType(), GetValidatedCount());

        public int GetCount() => _validatedCount;

        public int GetValidatedCount() => _validatedCount;

        #endregion

        #region Enumerator
        private class Enumerator : IEnumerator<T>
        {
            private readonly ValidatabeArrayGeneric<T> _parent;
            private int _enumeratorIndex = -1;
            private int _lastValid;

            public bool MoveNext()
            {
                _enumeratorIndex += 1;

                while (_enumeratorIndex <= _lastValid && !_parent.IsValid(_enumeratorIndex))
                {
                    _enumeratorIndex++;
                }

                return _enumeratorIndex <= _lastValid;
            }

            public void Reset()
            {
                _enumeratorIndex = -1;
            }

            public void Dispose()
            {
                _enumeratorIndex = -1;
            }

            public T Current => _parent._array[_enumeratorIndex];

            object IEnumerator.Current => _parent._array[_enumeratorIndex];

            public Enumerator(ValidatabeArrayGeneric<T> parent)
            {
                _parent = parent;
                _lastValid = parent._lastValidElement;
            }
        }
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
        #endregion

    }
}