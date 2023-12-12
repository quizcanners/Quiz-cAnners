using QuizCanners.Inspect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class AnimatorValue
    {
        public static void Reset(this List<Base> bases, Animator animator) 
        {
            foreach (var b in bases)
                b.Reset(animator);
        }

        public abstract class Base 
        {
            protected string name;

  
            public abstract void Reset(Animator animator);

            public Base(string name)
            {
                this.name = name;
            }

        }

        public class Trigger : Base
        {
            public void SetOn(Animator animator) => animator.SetTrigger(name);
            public override void Reset(Animator animator) => animator.ResetTrigger(name);

            public pegi.ChangesToken Inspect(Animator animator)
            {
                var changes = pegi.ChangeTrackStart();
                //base.Inspect(animator);

                if (name.PegiLabel().Click())
                    SetOn(animator);

                return changes;
            }

            public Trigger (string name) : base(name) { }
        }

        public abstract class ValueGeneric<T> : Base
        {
            protected T _defaultValue;
            public T LatestValue { get; private set; }
            private bool _latestSet = false;

            public T GetLatest() => LatestValue;

            public abstract T GetFrom(Animator animator);

            public bool SetOn(T value, Animator animator)
            {
                if (_latestSet && LatestValue.Equals(value))
                    return false;

                _latestSet = true;
                LatestValue = value;
                SetInternal(LatestValue, animator);
                return true;
            }

            public override void Reset(Animator animator) => SetOn(_defaultValue, animator);

            protected abstract void SetInternal(T value, Animator animator);

            public virtual pegi.ChangesToken Inspect(Animator animator)
            {
                var changed = pegi.ChangeTrackStart();

                if (!LatestValue.Equals(_defaultValue) && Icon.Refresh.Click())
                    Reset(animator);

                return changed;
            }

            public ValueGeneric(string name, T defaultValue) : base(name)
            {
                _defaultValue = defaultValue;
            }
        }

        public class Float : ValueGeneric<float>
        {
            protected override void SetInternal(float value, Animator animator) => animator.SetFloat(name, value);

            public override pegi.ChangesToken Inspect(Animator animator)
            {
                var changed = base.Inspect(animator);

                var val = LatestValue;
                if (name.PegiLabel(90).Edit(ref val))
                    SetOn(LatestValue, animator);

                return changed;
            }

            public override float GetFrom(Animator animator) => animator.GetFloat(name);

            public Float(string name, float defaultValue = 0) : base(name, defaultValue) { }
        }

        public class Bool : ValueGeneric<bool>
        {
            protected override void SetInternal(bool value, Animator animator) => animator.SetBool(name, value);

            public override bool GetFrom(Animator animator) => animator.GetBool(name);

            public override pegi.ChangesToken Inspect(Animator animator)
            {
                var changed = pegi.ChangeTrackStart();//base.Inspect(animator);

                var val = LatestValue;
                if (name.PegiLabel(90).ToggleIcon(ref val))
                    SetOn(val, animator);

                return changed;
            }

            public Bool(string name, bool defaultValue = false) : base(name, defaultValue) { }
        }

        public class Integer : ValueGeneric<int>
        {
            protected override void SetInternal(int value, Animator animator) => animator.SetInteger(name, value);

            public override int GetFrom(Animator animator) => animator.GetInteger(name);

            public override pegi.ChangesToken Inspect(Animator animator)
            {
                var changed = base.Inspect(animator);

                var val = LatestValue;
                if (name.PegiLabel(90).Edit(ref val))
                    SetOn(LatestValue, animator);

                return changed;
            }

            public Integer(string name, int defaultValue = 0) : base(name, defaultValue) { }
        }

        public static Animator Set(this Animator animator, Trigger trig)
        {
            trig.SetOn(animator);
            return animator;
        }

        public static Animator Set(this Animator animator, Float param, float value)
        {
            param.SetOn(value, animator);
            return animator;
        }

        public static Animator Set(this Animator animator, Integer param, int value)
        {
            param.SetOn(value, animator);
            return animator;
        }

        public static Animator Set(this Animator animator, Bool param, bool value)
        {
            param.SetOn(value, animator);
            return animator;
        }

        public static float Get(this Animator animator, Float param) => param.GetFrom(animator);
        public static int Get(this Animator animator, Integer param) => param.GetFrom(animator);
        public static bool Get(this Animator animator, Bool param) => param.GetFrom(animator);


    }
}