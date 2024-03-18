using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public static bool SearchMatch_ObjectList(IEnumerable list, string searchText) => list.Cast<object>().Any(e => Try_SearchMatch_Obj(e, searchText));

        internal static IEnumerable<Type> GetBaseClassesAndInterfaces(Type type, bool includeSelf = false)
        {
            List<Type> allTypes = new();

            if (includeSelf) allTypes.Add(type);

            allTypes.AddRange(
                (type.BaseType == typeof(object)) ?
                    type.GetInterfaces() :
                     Enumerable
                    .Repeat(type.BaseType, 1)
                    .Concat(type.GetInterfaces())
                    .Concat(GetBaseClassesAndInterfaces(type.BaseType))
                    .Distinct()

                    );


            return allTypes;
        }

    }
}