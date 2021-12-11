using QuizCanners.Inspect;
using QuizCanners.Migration;
using System;
using System.Collections;
using System.Collections.Generic;

namespace QuizCanners.Utils
{

#pragma warning disable IDE0034 // Simplify 'default' expression

    public abstract class SmartId
    {
        public abstract bool SameAs(SmartId other);
    }

    public abstract class SmartStringIdGeneric<TValue> : SmartId, IPEGI_ListInspect, ICfg, IPEGI, IGotReadOnlyName, INeedAttention, ISearchable where TValue : IGotName//, new()
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

        public void SetEntity(SmartStringIdGeneric<TValue> value) => Id = value.Id;
        public void SetEntity(TValue value) => Id = value.NameForInspector;
        
        #region Encode & Decode
        public CfgEncoder Encode() => new CfgEncoder().Add_String("id", Id);

        public void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "id": Id = data.ToString(); break;
            }
        }

        #endregion

        #region Inspector

        //  [NonSerialized] private int _inspectedStuff = -1;
        //     [NonSerialized] private int _inspectedElement = -1;

        public virtual void Inspect()
        {
            InspectSelectPart().Nl();
         
            "REFERENCED OBJECT".PegiLabel(style: pegi.Styles.ListLabel).Nl();

            TValue val = GetEntity();


            if (val != null)
                pegi.Try_Nested_Inspect(val).Nl();
            else
                ("ID {0} not found in Prototypes".F(Id)).PegiLabel().Nl();

        }

        public pegi.ChangesToken InspectSelectPart()
        {
            var changes = pegi.ChangeTrackStart();

            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".PegiLabel().Write();

            pegi.Select(ref Id, prots);

            return changes;
        }

        public virtual void InspectInList(ref int edited, int ind)
        {
            var th = this;
            pegi.CopyPaste.InspectOptionsFor(ref th);

            InspectSelectPart();

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

        public virtual IEnumerator SearchKeywordsEnumerator()
        {
            yield return Id;

            if (TryGetEntity(out var val)) 
            {
                yield return val;   
            }
        }

        #endregion
    }


    public abstract class SmartIntIdGeneric<TValue> : SmartId, IPEGI_ListInspect, ICfg, IPEGI, IGotReadOnlyName, INeedAttention, ISearchable
    {
        public int Id = -1;

        protected abstract List<TValue> GetEnities();

        public virtual bool TryGetEntity(out TValue entity)
        {
            if (Id != -1)
            {
                var prots = GetEnities();

                if (prots != null)
                {
                    entity = prots.TryGet(Id);
                    return entity != null;
                }
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

        public void SetEntity(SmartIntIdGeneric<TValue> value) => Id = value.Id;

        public virtual void SetEntity(TValue value) 
        {
            if (value == null)
            {
                Id = -1;
            } else 
            {
                Id = GetEnities().IndexOf(value);
            }
        }

        public override bool Equals(object obj) => SameAs(obj as SmartId);

        public override int GetHashCode() => Id + typeof(TValue).GetHashCode();

        public override bool SameAs(SmartId other)
        {
            if (other == null)
                return false;

            if (GetType() != other.GetType())
                return false;

            var asId = other as SmartIntIdGeneric<TValue>;

            return Id.Equals(asId.Id);
        }

        #region Encode & Decode
        public CfgEncoder Encode() => new CfgEncoder().Add("iid", Id);

        public void DecodeTag(string key, CfgData data)
        {
            switch (key)
            {
                case "iid": Id = data.ToInt(); break;
            }
        }

        #endregion

        #region Inspector

        public virtual void Inspect()
        {
            InspectSelectPart().Nl();

            "REFERENCED OBJECT".PegiLabel(style: pegi.Styles.ListLabel).Nl();

            TValue val = GetEntity();


            if (val != null)
                pegi.Try_Nested_Inspect(val).Nl();
            else
                ("ID {0} not found in Prototypes".F(Id)).PegiLabel().Nl();
        }

        public pegi.ChangesToken InspectSelectPart() 
        {
            var changes = pegi.ChangeTrackStart();

            var prots = GetEnities();

            if (prots == null)
                "NO PROTS".PegiLabel().Write();
            
            pegi.Select_Index(ref Id, prots);

            return changes;
        }

        public virtual void InspectInList(ref int edited, int ind)
        {
            var th = this;
            pegi.CopyPaste.InspectOptionsFor(ref th);

            InspectSelectPart();

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

        public virtual IEnumerator SearchKeywordsEnumerator()
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