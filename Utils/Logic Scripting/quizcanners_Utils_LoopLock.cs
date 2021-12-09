using System;
using System.Collections;

namespace QuizCanners.Utils
{
    public class LoopLock : IEnumerator
    {
        private volatile bool _lLock;

        public SkipLock Lock()
        {
            if (!Unlocked)
            {
                throw new Exception("Should check if LoopLock is Unlocked first.");
            }

            return new SkipLock(this);
        }

        public bool Unlocked => !_lLock;

        public object Current => _lLock;

        public class SkipLock : IDisposable
        {
            public void Dispose()
            {
                creator._lLock = false;
            }

            private volatile LoopLock creator;

            public SkipLock(LoopLock make)
            {
                creator = make;
                make._lLock = true;
            }
        }

        public bool MoveNext() => _lLock;

        public void Reset() { }
    }
}