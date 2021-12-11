using QuizCanners.Inspect;
using System.Collections.Generic;

namespace QuizCanners.Utils
{
    public class StaticDictionaryGeneric<T>: IPEGI where T : IGotName
    {
        public Dictionary<string, T> AllElements;

        public StaticDictionaryGeneric(params T[] elements)
        {
            AllElements = new Dictionary<string, T>();
            foreach (var el in elements)
                AllElements.Add(el.NameForInspector, el);
        }

        private int _inspectedElement = -1;
        public void Inspect()
        {
            typeof(T).ToPegiStringType().PegiLabel().Edit_Dictionary(AllElements, ref _inspectedElement).Nl();
        }
    }
}