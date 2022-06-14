using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using UnityEngine;
using Object = UnityEngine.Object;
using QuizCanners.Utils;

namespace QuizCanners.Migration
{

#pragma warning disable IDE0018 // Inline variable declaration

   

    #region Saved Cfg

    [Serializable]
    public class ICfgObjectExplorer : IGotCount
    {
        private readonly List<CfgState> states = new List<CfgState>();
        private string fileFolderHolder = "STDEncodes";
        private static ICfg inspectedCfg;

        #region Inspector

        [NonSerialized] private int inspectedState = -1;

        public int GetCount() => states.Count;
        
        public static bool PEGI_Static(ICfgCustom target)
        {
            inspectedCfg = target;

            var changed = false;

            "Load File:".PegiLabel(90).Write();
            target.LoadCfgOnDrop(); pegi.Nl();

            if (Icon.Copy.Click("Copy Component Data").Nl())
                ICfgExtensions.copyBufferValue = target.Encode().ToString();

            pegi.Nl();

            return changed;
        }

        public static ICfgObjectExplorer inspected;

        public bool Inspect(ICfg target)
        {
            var changed = pegi.ChangeTrackStart();
            inspectedCfg = target;
            inspected = this;

            CfgState added; 

            "Saved CFGs:".PegiLabel().Edit_List(states, ref inspectedState, out added);

            if (added != null && target != null)
            {
                added.dataExplorer.data = target.Encode().CfgData;
                added.NameForInspector = target.GetNameForInspector();
                added.comment = DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (inspectedState == -1)
            {
                Object myType = null;
                
                if ("From File:".PegiLabel(65).Edit(ref myType))
                {
                    added = new CfgState();

                    string path = QcFile.Explorer.TryGetFullPathToAsset(myType);

                    Debug.Log(path);

                    added.dataExplorer.data = new CfgData(QcFile.Load.TryLoadAsTextAsset(myType));

                    added.NameForInspector = myType.name;
                    added.comment = DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    states.Add(added);
                }
                /*
                var selfStd = target as IKeepMyCfg;

                if (selfStd != null)
                {
                    if (icon.Save.Click("Save itself (IKeepMySTD)"))
                        selfStd.SaveCfgData();
                    var slfData = selfStd.ConfigStd;
                    if (!string.IsNullOrEmpty(slfData)) {

                        if (icon.Load.Click("Use IKeepMySTD data to create new CFG")) {
                            var ss = new CfgState();
                            states.Add(ss);
                            ss.dataExplorer.data = slfData;
                            ss.NameForPEGI = "from Keep my STD";
                            ss.comment = DateTime.Now.ToString(CultureInfo.InvariantCulture);
                        }

                      if (icon.Refresh.Click("Load from itself (IKeepMySTD)"))
                        target.Decode(slfData);
                    }
                }
                */
                pegi.Nl();
            }

            inspectedCfg = null;

            return changed;
        }

        #endregion

        [Serializable]
        private class ICfgProperty : ICfgCustom, IPEGI, IGotName, IPEGI_ListInspect, IGotCount
        {

            public string tag;
            public CfgData data;
            public bool dirty;

            public void UpdateData()
            {
                if (_tags != null)
                    foreach (var t in _tags)
                        t.UpdateData();

                dirty = false;
                if (_tags != null)
                    data = Encode().CfgData;
            }

            public int inspectedTag = -1;
            [NonSerialized] private List<ICfgProperty> _tags;

            public ICfgProperty() { tag = ""; data = new CfgData(); }

            public ICfgProperty(string nTag, CfgData nData)
            {
                tag = nTag;
                data = nData;
            }

            #region Inspector

            public int GetCount() => _tags.IsNullOrEmpty() ? data.ToString().Length : _tags.CountForInspector();

            public string NameForInspector
            {
                get { return tag; }
                set { tag = value; }
            }

            public void Inspect()
            {
                if (_tags == null && data.ToString().Contains("|"))
                    this.Decode(data);

                var changes = pegi.ChangeTrackStart();

                if (_tags != null)
                    tag.PegiLabel().Edit_List(_tags, ref inspectedTag);

                dirty |= changes;

                if (inspectedTag == -1)
                {
                   
                    //"data".PegiLabel().edit(40, ref data).changes(ref dirty);
                    data.Inspect();

                    dirty |= changes;
                   /* UnityEngine.Object myType = null;

                    if (pegi.edit(ref myType))
                    {
                        dirty = true;
                        data = QcFile.LoadUtils.TryLoadAsTextAsset(myType);
                    }*/

                    if (dirty)
                    {
                        if (Icon.Refresh.Click("Update data string from tags"))
                            UpdateData();

                        if (Icon.Load.Click("Load from data String").Nl())
                        {
                            _tags = null;
                            this.Decode(data);//.DecodeTagsFor(this);
                            dirty = false;
                        }
                    }

                    pegi.Nl();
                }


                pegi.Nl();
            }

            public void InspectInList(ref int edited, int ind)
            {

                GetCount().ToString().PegiLabel(50).Write();

                if (data.IsEmpty == false && data.ToString().Contains("|"))
                {
                    pegi.Edit(ref tag);

                    if (Icon.Enter.Click("Explore data"))
                        edited = ind;
                }
                else
                {
                    if (pegi.Edit(ref tag))
                        dirty = true;

                    data.Inspect(); //.changes(ref dirty);
                    //pegi.edit(ref data).changes(ref dirty);
                }

                if (Icon.Copy.Click("Copy " + tag + " data to buffer."))
                {
                    ICfgExtensions.copyBufferValue = data.ToString();
                    ICfgExtensions.copyBufferTag = tag;
                }

                if (ICfgExtensions.copyBufferValue != null && Icon.Paste.Click("Paste " + ICfgExtensions.copyBufferTag + " Data").Nl())
                {
                    dirty = true;
                    data = new CfgData(ICfgExtensions.copyBufferValue);
                }

            }

            #endregion

            #region Encode & Decode

            public void DecodeInternal(CfgData dataToDecode)=>
                new CfgDecoder(dataToDecode).DecodeTagsIgnoreErrors(this);
            

            public CfgEncoder Encode()
            {
                var cody = new CfgEncoder();

                if (_tags == null) return cody;

                foreach (var t in _tags)
                    cody.Add_String(t.tag, t.data.ToString());

                return cody;

            }

            public void DecodeTag(string key, CfgData dta)
            {
                if (_tags == null)
                    _tags = new List<ICfgProperty>();

                _tags.Add(new ICfgProperty(key, dta));
            }
            #endregion

        }

        [Serializable]
        private class CfgState : IPEGI, IGotName, IPEGI_ListInspect, IGotCount
        {
            private static ICfg Cfg => inspectedCfg;

            public string comment;
            public ICfgProperty dataExplorer = new ICfgProperty("", new CfgData());

            #region Inspector
            public string NameForInspector { get { return dataExplorer.tag; } set { dataExplorer.tag = value; } }

            public static ICfgObjectExplorer Mgmt => inspected;

            public int GetCount() => dataExplorer.GetCount();

            public void Inspect()
            {

                if (dataExplorer.inspectedTag == -1)
                {
                    this.inspect_Name();
                    if (dataExplorer.tag.Length > 0 && Icon.Save.Click("Save To Assets"))
                    {
                        QcFile.Save.ToAssets(Mgmt.fileFolderHolder, filename: dataExplorer.tag, data: dataExplorer.data.ToString(), asBytes: true);
                        QcUnity.RefreshAssetDatabase();
                    }

                    pegi.Nl();

                    if (Cfg != null)
                    {
                        if (dataExplorer.tag.Length == 0)
                            dataExplorer.tag = Cfg.GetNameForInspector() + " config";

                        "Save To:".PegiLabel(50).Edit(ref Mgmt.fileFolderHolder);

                        var uObj = Cfg as Object;

                        if (uObj && Icon.Done.Click("Use the same directory as current object."))
                            Mgmt.fileFolderHolder = QcUnity.GetAssetFolder(uObj);

                        pegi.ClickHighlight(uObj).Nl();
                    }

                    if ("Description".PegiLabel().IsFoldout().Nl())
                    {
                        pegi.Edit_Big(ref comment).Nl();
                    }
                }

                dataExplorer.Nested_Inspect();
            }

            public void InspectInList(ref int edited, int ind)
            {

                if (dataExplorer.data.ToString().IsNullOrEmpty() == false && Icon.Copy.Click())
                    pegi.SetCopyPasteBuffer(dataExplorer.data.ToString());
                
                GetCount().ToString().PegiLabel(60).Edit(ref dataExplorer.tag);

                if (Cfg != null)
                {
                    if (Icon.Load.ClickConfirm("sfgLoad", "Decode Data into " + Cfg.GetNameForInspector()))
                    {
                        dataExplorer.UpdateData();
                        Cfg.Decode(dataExplorer.data);
                    }
                    if (Icon.Save.ClickConfirm("cfgSave", "Save data from " + Cfg.GetNameForInspector()))
                        dataExplorer = new ICfgProperty(dataExplorer.tag, Cfg.Encode().CfgData);
                }

                if (Icon.Enter.Click(comment))
                    edited = ind;
            }

            #endregion
        }


    }
    #endregion
}