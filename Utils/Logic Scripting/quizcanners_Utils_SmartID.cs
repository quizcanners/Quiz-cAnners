using QuizCanners.Inspect;
using QuizCanners.Migration;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{

#pragma warning disable IDE0034 // Simplify 'default' expression

    public static class SmartId
    {

        public static bool Contains<T,TValue>(this List<T> ids, TValue el) 
            where T: StringGeneric<TValue>
            where TValue : IGotStringId
        {
            foreach (var id in ids) 
            {
                if (id.Id.Equals(el.StringId))
                    return true;
            }

            return false;
        }

        public abstract class Base
        {
            public abstract bool Equals(Base other);
        }

        public abstract class StringBase : Base
        {
            [SerializeField] protected string _id;

            public virtual string Id
            {
                get => _id;
                set => _id = value;
            }
            protected virtual bool AllowEdit => false;

            protected virtual bool ShowIndex => false;
        }

        /*
        public interface IGotStandardId <TValue> where TValue : IGotStandardId<TValue>, IGotStringId
        {
            public Standard<TValue> GetId();
        }

        public static pegi.ChangesToken InspectSelect<TKey, TValue>(this TKey id, ref TKey key) where TKey : Standard<TValue> where TValue : IGotStandardId<TValue>, IGotStringId
        {
            var changes = pegi.ChangeTrackStart();

            var prots = id.GetEntities();

            if (prots == null)
                "NO PROTS".ConstLabel().Write();

            if (!id.Id.IsNullOrEmpty() && Icon.Clear.Click())
                id.Id = "";

            if (pegi.Select_A<TKey, TValue>(ref id, prots, nameGenerator: GetName))
                key = id;

            string GetName(TValue val) 
            {
                if (val == null)
                    return "-";

                return val.ToString();
            }

            return changes;
        }

        public abstract class Standard<TValue> : StringBase, IPEGI_ListInspect, IPEGI, INeedAttention, ISearchable where TValue : IGotStandardId<TValue>, IGotStringId
        {

            [Serializable]
            public class Dictionary : SerializableDictionary<Standard<TValue>, TValue> { }

            public abstract Dictionary GetEntities();

            public virtual bool TryGetEntity(out TValue entity)
            {
                if (Id != null)
                {
                    var prots = GetEntities();

                    if (prots != null)
                        return prots.TryGetValue(this, out entity) && entity != null;
                }

                entity = default(TValue);
                return false;
            }

            public virtual TValue GetEntity()
            {
                Dictionary prots = GetEntities();

                if (prots != null && prots.TryGetValue(this, out var val))
                    return val;

                return default(TValue);
            }

            public bool Equals(Standard<TValue> other)
            {
                if (other == null)
                    return false;

                if (Id.IsNullOrEmpty())
                    return false;

                return Id.Equals(other.Id);
            }

            public override bool Equals(Base other)
            {
                if (other == null)
                    return false;

                if (Id.IsNullOrEmpty())
                    return false;

                if (GetType() != other.GetType())
                    return false;

                var asId = other as Standard<TValue>;

                return Id.Equals(asId.Id);
            }

            public override bool Equals(object obj) => Equals(obj as Standard<TValue>);
            public override int GetHashCode() => Id.GetHashCode();

            public void SetEntityId(Standard<TValue> value) => Id = value.Id;
            public void SetEntity(TValue value)
            {
                if (value.StringId.IsNullOrEmpty())
                    value.StringId = Guid.NewGuid().ToString();

                Id = value.StringId;
            }

            public virtual void CreateAndAdd(Func<TValue> creator, bool autogenerateId, out TValue newElement)
            {
                newElement = creator();
                if (autogenerateId)
                    newElement.StringId = Guid.NewGuid().ToString();

                GetEntities()[newElement.GetId()] = newElement;
                SetEntity(newElement);
            }


            #region Inspector

            public virtual void Inspect()
            {
                if (AllowEdit)
                    InspectSelectPart().Nl();

                using (pegi.Indent())
                {
                    TValue val = GetEntity();

                    if (val != null)
                        pegi.Try_Nested_Inspect(val).Nl();
                    else
                        ("ID {0} not found in Prototypes".F(Id)).PegiLabel().Nl();
                }

            }

            public pegi.ChangesToken InspectSelectPart()
            {
                var changes = pegi.ChangeTrackStart();

                var prots = GetEntities();

                if (prots == null)
                    "NO PROTS".ConstLabel().Write();

                if (!Id.IsNullOrEmpty() && Icon.Clear.Click())
                    Id = "";

                var id = Id;
                pegi.Select(ref id, prots, showIndex: ShowIndex).OnChanged(() => Id = id);

                return changes;
            }

            public pegi.ChangesToken InspectSelectForList<T>(List<T> alreadyAdded) where T : StringGeneric<TValue>
                => pegi.Select(ref _id, GetEntities(), showIndex: ShowIndex, optionValidator: pair =>
                {
                    foreach (var cur in alreadyAdded)
                    {
                        if (cur.Id == pair.Key)
                            return false;
                    }
                    return true;
                });


            public virtual void InspectInList(ref int edited, int ind)
            {
                if (AllowEdit)
                {
                    InspectSelectPart();
                }
                else if (TryGetEntity(out var ent))
                {
                    if (ent is IPEGI_ListInspect lst)
                    {
                        lst.InspectInList(ref edited, ind);
                        return;
                    }

                    if (ToString().PegiLabel(pegi.Styles.Text.EnterLabel).ClickLabel())
                        edited = ind;
                }
                else if (ToString().PegiLabel(pegi.Styles.Text.EnterLabel).ClickLabel())
                    edited = ind;

                if (this.Click_Enter_Attention())
                    edited = ind;

            }

            public override string ToString()
            {
                TValue ent = GetEntity();
                return ent != null ? "{0}".F(ent.GetNameForInspector()) : "NOT FOUND: {0}".F(Id);
            }

            public virtual string NeedAttention()
            {
                if (GetEntities() == null)
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

            public Standard() { }

            public Standard(TValue val)
            {
                SetEntity(val);
            }
        }
        */

        public abstract class StringGeneric<TValue> : StringBase, IPEGI_ListInspect, ICfg, IPEGI, INeedAttention, ISearchable where TValue : IGotStringId
        {

            protected abstract Dictionary<string, TValue> GetEntities();

            public virtual bool TryGetEntity(out TValue entity)
            {
                if (Id != null)
                {
                    var prots = GetEntities();

                    if (prots != null)
                        return prots.TryGetValue(Id, out entity) && entity != null;
                }

                entity = default(TValue);
                return false;
            }

            public virtual TValue GetEntity()
            {
                var prots = GetEntities();

                if (prots != null)
                    return prots.GetValueOrDefault(Id);

                return default(TValue);
            }

            public bool Equals(StringGeneric<TValue> other)
            {
                if (other == null)
                    return false;

                if (Id.IsNullOrEmpty())
                    return false;

                return Id.Equals(other.Id);
            }

            public override bool Equals(Base other)
            {
                if (other == null)
                    return false;

                if (Id.IsNullOrEmpty())
                    return false;

                if (GetType() != other.GetType())
                    return false;

                var asId = other as StringGeneric<TValue>;

                return Id.Equals(asId.Id);
            }

            public override bool Equals(object obj) => Equals(obj as Base);
            public override int GetHashCode() => HashCode.Combine(Id);

            public void SetEntityId(StringGeneric<TValue> value) => Id = value.Id;
            public void SetEntity(TValue value)
            {
                if (value.StringId.IsNullOrEmpty())
                    value.StringId = Guid.NewGuid().ToString();

                Id = value.StringId;
            }

            public virtual void CreateAndAdd(Func<TValue> creator, bool autogenerateId, out TValue newElement)
            {
                newElement = creator();
                if (autogenerateId)
                    newElement.StringId = Guid.NewGuid().ToString();

                GetEntities()[newElement.StringId] = newElement;
                SetEntity(newElement);
            }

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

            public virtual void Inspect()
            {
                if (AllowEdit)
                    InspectSelectPart().Nl();

                using (pegi.Indent())
                {
                    TValue val = GetEntity();

                    if (val != null)
                        pegi.Try_Nested_Inspect(val).Nl();
                    else
                        ("ID {0} not found in Prototypes".F(Id)).PL().Nl();
                }

            }

            public pegi.ChangesToken InspectSelectPart()
            {
                var changes = pegi.ChangeTrackStart();

                var prots = GetEntities();

                if (prots == null)
                    "NO PROTS".ConstL().Write();

                if (!Id.IsNullOrEmpty() && Icon.Clear.Click())
                    Id = "";

                var id = Id;
                pegi.Select(ref id, prots, showIndex: ShowIndex).OnChanged(() => Id = id);
                
                return changes;
            }

            public pegi.ChangesToken InspectSelectForList<T>(List<T> alreadyAdded) where T : StringGeneric<TValue>
                => pegi.Select(ref _id, GetEntities(), showIndex: ShowIndex, optionValidator: pair =>
                {
                    foreach (var cur in alreadyAdded) 
                    {
                        if (cur.Id == pair.Key)
                            return false;
                    }
                    return true;
                });
            

            public virtual void InspectInList(ref int edited, int ind)
            {
                if (AllowEdit)
                {
                    InspectSelectPart();
                } else if (TryGetEntity(out var ent))
                {
                    if (ent is IPEGI_ListInspect lst)
                    {
                        lst.InspectInList(ref edited, ind);
                        return;
                    }
                    
                    if (ToString().PL(pegi.Styles.Text.EnterLabel).ClickLabel())
                    edited = ind;
                }
                else if (ToString().PL(pegi.Styles.Text.EnterLabel).ClickLabel())
                    edited = ind;

                if (this.Click_Enter_Attention())
                    edited = ind;

            }

            public override string ToString()
            {
                TValue ent = GetEntity();
                return ent != null ? "{0}".F(ent.GetNameForInspector()) : "NOT FOUND: {0}".F(Id);
            }

            public virtual string NeedAttention()
            {
                if (GetEntities() == null)
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

            public StringGeneric() { }

            public StringGeneric(TValue val)
            {
                SetEntity(val);
            }
        }

        public abstract class StringGeneric_Cached<TValue> : StringGeneric<TValue> where TValue : IGotStringId
        {
            private bool cached;
            private TValue cachedValue;

            protected virtual bool IsDirty
            {
                get => false;
                set { }
            }

            public override string Id
            {
                get => base.Id;
                set
                {
                    base.Id = value;
                    cached = false;
                }
            }

            public override bool TryGetEntity(out TValue entity)
            {
                if (cached && !IsDirty)
                {
                    entity = cachedValue;
                    return entity != null;
                }

                cached = base.TryGetEntity(out entity);
                cachedValue = entity;
                IsDirty = false;

                return entity != null;

            }

            public override TValue GetEntity()
            {
                if (cached)
                {
                    return cachedValue;
                }

                cached = base.TryGetEntity(out cachedValue);

                return cachedValue;
            }
        }

        public static bool IsValid<T>(this StringGeneric<T> id) where T:IGotStringId => id != null && !id.Id.IsNullOrEmpty();

        public abstract class IntGeneric<TValue> : Base, IPEGI_ListInspect, ICfg, IPEGI, INeedAttention, ISearchable
        {
            public int Id = -1;

            public bool IsValid => Id >= 0;

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

            public void SetEntity(IntGeneric<TValue> value) => Id = value.Id;

            public virtual void SetEntity(TValue value)
            {
                if (value == null)
                {
                    Id = -1;
                }
                else
                {
                    Id = GetEnities().IndexOf(value);
                }
            }

            public override bool Equals(object obj) => Equals(obj as Base);

            public override int GetHashCode() => Id + typeof(TValue).GetHashCode();

            public override bool Equals(Base other)
            {
                if (other == null)
                    return false;

                if (GetType() != other.GetType())
                    return false;

                var asId = other as SmartId.IntGeneric<TValue>;

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

                "REFERENCED OBJECT".PL(style: pegi.Styles.ListLabel).Nl();

                TValue val = GetEntity();


                if (val != null)
                    pegi.Try_Nested_Inspect(val).Nl();
                else
                    ("ID {0} not found in Prototypes".F(Id)).PL().Nl();
            }

            public pegi.ChangesToken InspectSelectPart()
            {
                var changes = pegi.ChangeTrackStart();

                var prots = GetEnities();

                if (prots == null)
                    "NO PROTS".ConstL().Write();

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

            public override string ToString()
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

        public abstract class IntGeneric_Dictionary<TValue> : Base, IPEGI_ListInspect, ICfg, IPEGI, INeedAttention, ISearchable where TValue : IGotIndex
        {
            public int Id = -1;

            public bool IsValid => Id >= 0;

            protected abstract Dictionary<int, TValue> GetEnities();

            public virtual bool TryGetEntity(out TValue entity)
            {
                if (Id >= 0)
                {
                    var prots = GetEnities();
                    if (prots != null)
                        return prots.TryGetValue(Id, out entity);
                }

                entity = default(TValue);
                return false;
            }

            public virtual TValue GetEntity()
            {
                var prots = GetEnities();

                if (prots != null && prots.TryGetValue(Id, out var val))
                    return val;

                return default(TValue);
            }

            public void SetEntity(IntGeneric_Dictionary<TValue> value) => Id = value.Id;

            public virtual void SetEntity(TValue value) => Id = (value == null) ? -1 : value.IndexForInspector;
            
            public override bool Equals(object obj) => Equals(obj as Base);

            public override int GetHashCode() => Id + typeof(TValue).GetHashCode();

            public override bool Equals(Base other)
            {
                if (other == null)
                    return false;

                if (GetType() != other.GetType())
                    return false;

                var asId = other as IntGeneric_Dictionary<TValue>;

                return Id == asId.Id;
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

                "REFERENCED OBJECT".PL(style: pegi.Styles.ListLabel).Nl();

                TValue val = GetEntity();


                if (val != null)
                    pegi.Try_Nested_Inspect(val).Nl();
                else
                    ("ID {0} not found in Prototypes".F(Id)).PL().Nl();
            }

            public pegi.ChangesToken InspectSelectPart()
            {
                var changes = pegi.ChangeTrackStart();

                var prots = GetEnities();

                if (prots == null)
                    "NO Mission prototypes".ConstL().WriteWarning();
                else 
                    "Mission".ConstL().Select(ref Id, prots);

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

            public override string ToString()
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
}