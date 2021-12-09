
namespace QuizCanners.Utils
{
    public class RecursionCounter 
    {
        public int Recursions { get; private set; }

        public Incrementor Increment() => new Incrementor(this);

        public class Incrementor : System.IDisposable
        {
            private volatile RecursionCounter _creator;
            private int _recursions;

            public void Increment(int by = 1) 
            {
                _recursions += by;
            }

            public void Dispose() => _creator.Recursions = _recursions;
            
            public Incrementor(RecursionCounter creator)
            {
                _creator = creator;
                _recursions = _creator.Recursions;
                _creator.Recursions++;
            }
        }

    }
}