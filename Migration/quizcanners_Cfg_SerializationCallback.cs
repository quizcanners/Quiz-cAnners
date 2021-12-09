using UnityEngine;

namespace QuizCanners.Migration
{
    public abstract class CfgSelfSerializationBase : ICfg, ISerializationCallbackReceiver
    {
        public abstract void DecodeTag(string key, CfgData data);
        public abstract CfgEncoder Encode();

        [SerializeField] private CfgData _data; 

        public virtual void OnBeforeSerialize()
        {
            _data = Encode().CfgData;
        }

        public virtual void OnAfterDeserialize() => this.Decode(_data);
    }

    public abstract class CfgSelfSerializationBaseScriptableObject : ScriptableObject, ICfg, ISerializationCallbackReceiver
    {
        public abstract void DecodeTag(string key, CfgData data);
        public abstract CfgEncoder Encode();

        [SerializeField] private CfgData _data;

        public virtual void OnBeforeSerialize()
        {
            _data = Encode().CfgData;
        }

        public virtual void OnAfterDeserialize() => this.Decode(_data);
    }
}