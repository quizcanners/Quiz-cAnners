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

            public void Inspect(Animator animator)
            {
                //base.Inspect(animator);

                if (name.PegiLabel().Click())
                    SetOn(animator);
            }

            public Trigger (string name) : base(name) { }
        }

        public abstract class ValueGeneric<T> : Base
        {
            protected T _defaultValue;
            protected T latestValue;

            public T Get() => latestValue;

            public bool SetOn(T value, Animator animator)
            {
                if (latestValue.Equals(value))
                    return false;

                latestValue = value;
                SetInternal(latestValue, animator);
                return true;
            }

            public override void Reset(Animator animator) => SetOn(_defaultValue, animator);

            protected abstract void SetInternal(T value, Animator animator);

            public virtual void Inspect(Animator animator)
            {
                if (!latestValue.Equals(_defaultValue) && Icon.Refresh.Click())
                    Reset(animator);
            }

            public ValueGeneric(string name, T defaultValue) : base(name)
            {
                _defaultValue = defaultValue;
            }
        }

        public class Float : ValueGeneric<float>
        {
            protected override void SetInternal(float value, Animator animator) => animator.SetFloat(name, value);

            public override void Inspect(Animator animator)
            {
                base.Inspect(animator);

                if (name.PegiLabel(90).Edit(ref latestValue))
                    SetOn(latestValue, animator);
            }

            public Float(string name, float defaultValue = 0) : base(name, defaultValue) { }
        }

        public class Bool : ValueGeneric<bool>
        {
            protected override void SetInternal(bool value, Animator animator) => animator.SetBool(name, value);

            public override void Inspect(Animator animator)
            {
                base.Inspect(animator);

                if (name.PegiLabel(90).ToggleIcon(ref latestValue))
                    SetOn(latestValue, animator);
            }

            public Bool(string name, bool defaultValue = false) : base(name, defaultValue) { }
        }

        public class Integer : ValueGeneric<int>
        {
            protected override void SetInternal(int value, Animator animator) => animator.SetInteger(name, value);

            public override void Inspect(Animator animator)
            {
                base.Inspect(animator);

                if (name.PegiLabel(90).Edit(ref latestValue))
                    SetOn(latestValue, animator);
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

    }
}