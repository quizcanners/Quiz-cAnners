using QuizCanners.Inspect;
using System.Collections.Generic;

namespace QuizCanners.Utils
{
    public class StaticDictionaryGeneric<T>: IPEGI where T : IGotStringId
    {
        public Dictionary<string, T> AllElements;

        public StaticDictionaryGeneric(params T[] elements)
        {
            AllElements = new Dictionary<string, T>();
            foreach (var el in elements)
                AllElements.Add(el.StringId, el);
        }

        private int _inspectedElement = -1;
        void IPEGI.Inspect()
        {
            typeof(T).ToPegiStringType().PL().Edit_Dictionary(AllElements, ref _inspectedElement).Nl();
        }
    }
}