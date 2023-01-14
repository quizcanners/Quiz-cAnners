using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Migration
{

    [AttributeUsage(AttributeTargets.Class)]
    public class DerivedListAttribute : UnityEngine.Scripting.PreserveAttribute //Attribute
    {
        public readonly List<Type> derivedTypes;
        public DerivedListAttribute(params Type[] types)
        {
            derivedTypes = new List<Type>(types);
        }
    }

    public abstract class SO_Configurations_Generic<T> : ConfigurationsSO_Base, ICfg, IPEGI_ListInspect where T : Configuration, new()
    {
        public List<T> configurations = new List<T>();

        public int IndexOfActiveConfiguration 
        {
            get 
            {
                var active = ActiveConfiguration;
                if (active != null)
                    return configurations.IndexOf(active);

                return -1;
            }
            set 
            {
                 ActiveConfiguration = configurations.TryGet(value);
            }
        }

        public T ActiveConfiguration
        {
            get => configurations.IsNullOrEmpty() ? null : (configurations[0].ActiveConfiguration as T);
            set
            {
                if (configurations.IsNullOrEmpty())
                    return;

                if (value == null)
                    configurations[0].ActiveConfiguration = null;
                else 
                    value.ActiveConfiguration = value;
            }    
        }

        #region Inspector

        public void InspectShortcut()
        {
            if (configurations.Count == 0)
            {
                if ("New {0}".F(typeof(T).ToPegiStringType()).PegiLabel().Click())
                    configurations.Add(new T());
            }
            else
            {
                var any = configurations[0];
                var active = any.ActiveConfiguration as T;

                if (pegi.Select(ref active, configurations))
                    any.ActiveConfiguration = active;

                if (active != null && Icon.Save.Click())
                    active.SaveCurrentState();
            }
        }

        public override void Inspect() => "Configurations".PegiLabel().Edit_List(configurations);

        public CfgEncoder Encode()
        {
            var cody = new CfgEncoder();
            var act = ActiveConfiguration;
            if (act != null)
                cody.Add("act", configurations.IndexOf(ActiveConfiguration));

            return cody;
        }

        public void DecodeTag(string key, CfgData data)
        {
            switch (key) 
            {
                case "act": ActiveConfiguration = configurations.TryGet(data.ToInt());  break;
            }
        }

        public void InspectInList(ref int edited, int index)
        {
            var ind = IndexOfActiveConfiguration;

            if (pegi.Select_Index(ref ind, configurations))
                ActiveConfiguration = configurations[ind];

            if (Icon.Enter.Click())
                edited = index;
        }

        #endregion
    }

    public abstract class ConfigurationsSO_Base : ScriptableObject, IPEGI
    {
        public virtual void Inspect() { }

        public static bool Inspect<T>(ref T configs) where T : ConfigurationsSO_Base
        {
            var changed = false;

            if (configs)
            {
                configs.Nested_Inspect().Nl();
            }
            else
            {
                "Configs".PegiLabel(90).Edit(ref configs);

                if (Icon.Create.Click("Create new Config"))
                    configs = QcUnity.CreateScriptableObjectAsset<T>("ScriptableObjects/Configs", "Config");

                pegi.Nl();
            }

            return changed;
        }
    }

    [Serializable]
    public abstract class Configuration : IPEGI_ListInspect, IGotName
    {
        public string name;
        public CfgData data;

        public Configuration ActiveConfiguration 
        {
            get => ActiveConfig_Internal;
            set 
            {
                if (ActiveConfig_Internal == value)
                    return;

                ActiveConfig_Internal = value;
            }
        }

        protected abstract Configuration ActiveConfig_Internal { get; set; }


        public void SetAsCurrent()
        {
            ActiveConfiguration = this;
        }

        public void SaveCurrentState() => data = EncodeData().CfgData;

        public abstract CfgEncoder EncodeData();

        #region Inspect

        public string NameForInspector
        {
            get { return name; }
            set { name = value; }
        }

        public virtual void InspectInList(ref int edited, int ind)
        {

            var active = ActiveConfiguration;

            bool isActive = this == active;

            bool allowOverride = active == null || isActive;

            if (isActive)
                pegi.SetBgColor(Color.green);

            if (!allowOverride && !data.IsEmpty && Icon.Clear.ClickConfirm(confirmationTag: "dlCfg"+ind, toolTip: "Delete this configuration?"))
                data.Clear();

            pegi.Edit(ref name);

            if (isActive)
            {
                if (Icon.Close.ClickUnFocus())
                    ActiveConfiguration = null;

                if (Icon.Save.ClickUnFocus())
                    SaveCurrentState();
            }
            else
            {
                if (!data.IsEmpty)
                {
                    if (Icon.Play.ClickUnFocus())
                        ActiveConfiguration = this;
                }
                else if (Icon.SaveAsNew.ClickUnFocus())
                    SaveCurrentState();
            }

        
            pegi.RestoreBGColor();
        }

        #endregion

      /*  #region Encode & Decode

        public CfgEncoder Encode() => new CfgEncoder()
            .Add_String("n", name)
            .Add_IfNotEmpty("d", data);

        public void Decode(string key, CfgData d)
        {
            switch (key)
            {
                case "n": name = d.ToString(); break;
                case "d": data = d.ToString(); break;
            }
        }

        #endregion*/

        public Configuration()
        {
            name = "New Config";
        }

        public Configuration(string name)
        {
            this.name = name;
        }

    }
}

