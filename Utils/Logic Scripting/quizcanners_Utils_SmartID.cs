using QuizCanners.Inspect;
using System;
using System.Collections.Generic;

namespace QuizCanners.Utils
{

#pragma warning disable IDE0034 // Simplify 'default' expression

    public abstract class SmartId
    {
        public abstract bool SameAs(SmartId other);
    }

    public abstract class SmartStringIdGeneric<TValue> : SmartId, IPEGI_ListInspect, IPEGI, IGotReadOnlyName, INeedAttention, ISearchable where TValue : IGotName//, new()
    {
        public string Id;

        protected abstract Dictionary<string, TValue> GetEnities();

        public virtual bool TryGetEntity(out TValue entity)
        {
            if (Id.IsNullOrEmpty() == false)
            {
                var prots = GetEnities();

                if (prots != null)
                    return prots.TryGetValue(Id, out entity) && entity != null;
            }

            entity = default(TValue);
            return false;
        }

        public virtual TValue GetEntity()
        {
            var prots = GetEnities();

            if (prots != null)
                return prots.TryGet(Id);

            return default(TValue);
        }

        public override bool SameAs(SmartId other) 
        {
            if (other == null)
                return false;

            if (GetType() != other.GetType())
                return false;
            
            var asId = other as SmartStringIdGeneric<TValue>;

            return Id.Equals(asId.Id);
        }

        #region Inspector

      //  [NonSerialized] private int _inspectedStuff = -1;
   //     [NonSerialized] private int _inspectedElement = -1;

        public virtual void Inspect()
        {
            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".PegiLabel().nl();

         //   if (_inspectedStuff == -1)
                "Smart ID".PegiLabel(70).select(ref Id, prots).nl();

            "REFERENCED OBJECT".PegiLabel(style: pegi.Styles.ListLabel).nl();

            TValue val = GetEntity();

          /*  if (val.GetNameForInspector().isEntered(ref _inspectedStuff, 1).nl())
            {*/ 
                if (val != null)
                    pegi.Try_Nested_Inspect(val).nl();
                else
                    ("ID {0} not found in Prototypes".F(Id)).PegiLabel().nl();
            //}

            /*if ("{0} Dictionary".F(typeof(TValue).ToPegiStringType()).isEntered(ref _inspectedStuff, 2).nl())
            {
                typeof(TValue).ToPegiStringType().edit_Dictionary(GetEnities(), ref _inspectedElement);

                if (_inspectedElement == -1)
                    pegi.addDictionaryPairOptions(GetEnities(), newElementName: "A Band of Knuckleheads");
            }*/
        }

        public virtual void InspectInList(ref int edited, int ind)
        {
            var th = this;
            pegi.CopyPaste.InspectOptionsFor(ref th);

            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".PegiLabel().write();

            pegi.select(ref Id, prots);

            if (this.Click_Enter_Attention())
                edited = ind;

        }

        public virtual string GetReadOnlyName()
        {
            TValue ent = GetEntity();
            return ent != null ? "Id of {0}".F(ent.GetNameForInspector()) : "Target (Id: {0}) NOT FOUND".F(Id);
        }

        public virtual string NeedAttention()
        {
            if (GetEnities() == null)
                return "No Entities";

            if (GetEntity() == null)
                return "No Entity for {0}".F(Id);

            return null;
        }

        public virtual IEnumerator<object> SearchKeywordsEnumerator()
        {
            yield return Id;

            if (TryGetEntity(out var val)) 
            {
                yield return val;   
            }
        }
        #endregion
    }
}