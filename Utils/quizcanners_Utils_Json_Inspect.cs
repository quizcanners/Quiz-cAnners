using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using QuizCanners.Inspect;
using UnityEngine;

using StringBuilder = System.Text.StringBuilder;

namespace QuizCanners.Utils
{

#pragma warning disable IDE0019 // Use pattern matching
    
    public class EncodedJsonInspector : IPEGI
    {
        private readonly string _name;
        private string jsonDestination = "";

        protected JsonBase rootJson;



        protected static void TryDecode(ref JsonBase j)
        {
            if (j is not JsonString str) return;

            var tmp = str.TryDecodeString();
            if (tmp != null)
                j = tmp;
            else
                str.dataOnly = true;
        }

        protected static void DecodeOrInspectJson(ref JsonBase j, bool foldedOut, string name = "")
        {
            JsonString str = j.AsJsonString;

            if (str != null)
            {
                if (str.dataOnly)
                {
                    if (!foldedOut)
                    {
                        name.PL().Write_ForCopy();
                        //if (name.Length > 15 && Icon.Copy.Click("Copy name to clipboard", 20))
                        //  GUIUtility.systemCopyBuffer = name;
                        pegi.Edit(ref str.data);

                        if (!str.InlineJson_Checked) 
                        {
                            str.InlineJson_Checked = true;
                            str.InlineJson_Found = str.data.Contains("\\\"");
                        }

                        if (str.InlineJson_Found && Icon.Refresh.Click())
                        {
                            str.data = str.data.Substring(1, str.data.Length - 2);
                            str.data = str.data.Replace("\\\"", "\"").Replace("%20", " ");
                            TryDecode(ref j);
                        }
                    }
                }
                else if (foldedOut && "Decode 1 layer".PL().Click())
                    TryDecode(ref j);
                
                pegi.Nl();
            }

            if (!foldedOut)
                return;

            j.Inspect();
            pegi.Nl();
        }

        public EncodedJsonInspector() { rootJson = new JsonString(); }

        public EncodedJsonInspector(string data, string name = "") { rootJson = new JsonString(data); _name = name; }

        public bool triedToDecodeAll;

        public void TryToDecodeAll()
        {

            triedToDecodeAll = true;

            var rootAsString = rootJson.AsJsonString;

            if (rootAsString != null && !rootAsString.data.IsNullOrEmpty())
            {

                rootAsString.dataOnly = false;

                var sb = new StringBuilder();

                int index = 0;

                while (index < rootAsString.data.Length && rootAsString.data[index] != '{' && rootAsString.data[index] != '[')
                {
                    sb.Append(rootAsString.data[index]);
                    index++;
                }

                jsonDestination = sb.ToString();

                rootAsString.data = rootAsString.data[index..];
            }
            
            do { } while (rootJson.DecodeAll(ref rootJson));
        }

        #region Inspector

        public override string ToString() => "{0} Json: {1}".F(_name, rootJson);

        private static readonly List<string> inspectedPathForErrorDebug = new();

        void IPEGI.Inspect()
        {

            pegi.Nl();

            if (Icon.Delete.Click())
            {
                triedToDecodeAll = false;
                rootJson = new JsonString();
                jsonDestination = "";
            }

            if (!triedToDecodeAll && "Decode All".PL().Click())
                TryToDecodeAll();

            if (jsonDestination.Length > 5)
                jsonDestination.PL().Write();

            pegi.Nl();

            inspectedPathForErrorDebug.Clear();

            DecodeOrInspectJson(ref rootJson, true);
        }

        #endregion

        protected class PathAdd_ErrorDebug : IDisposable
        {
            public PathAdd_ErrorDebug(string name)
            {
                inspectedPathForErrorDebug.Add(name);
            }

            public void Dispose()
            {
                inspectedPathForErrorDebug.RemoveLast();
            }
        }

        #region Json Anatomy
        protected class JsonString : JsonBase
        {
            public bool InlineJson_Checked;
            public bool InlineJson_Found;

            public bool dataOnly;

            public override JsonString AsJsonString => this;

            public override bool HasNestedData => !dataOnly;

            public string data = "";

            public string Data
            {
                set
                {
                    data = Regex.Replace(value, @"\t|\n|\r", "");

                    foreach (var c in data)
                        if (c != ' ')
                        {
                            dataOnly = (c != '[' && c != '{');
                            break;
                        }

                    if (!dataOnly)
                    {
                        data = Regex.Replace(data, "{", "{" + Environment.NewLine);
                        data = Regex.Replace(data, ",", "," + Environment.NewLine);
                    }
                }
            }

            public override int GetCount() => data.Length;

            public override string ToString() => data.IsNullOrEmpty() ? "Empty" : data.FirstLine();

            public JsonString() { }

            public JsonString(string data) { Data = data; }

            public override void Inspect()
            {
                var changed = pegi.ChangeTrackStart();

                if (data.Length > 500)
                {
                    "String is too long to show: {0} chars".F(data.Length).PL().Write_Hint();

                    if (Icon.Copy.Click("TO Copy Paste Buffer"))
                    {
                        pegi.SetCopyPasteBuffer(data);
                    }

                    pegi.Nl();
                }
                else
                {
                    if (dataOnly)
                        pegi.Edit(ref data);
                    else
                        pegi.Edit_Big(ref data);
                }

                if (changed)
                    dataOnly = false;
            }

            private enum JsonDecodingStage { DataTypeDecision, ExpectingVariableName, ReadingVariableName, ExpectingTwoDots, ReadingData }

            public override bool DecodeAll(ref JsonBase thisJson)
            {
                if (dataOnly) return false;
                var tmp = TryDecodeString();
                if (tmp != null)
                {
                    thisJson = tmp;
                    return true;
                }

                dataOnly = true;

                return false;
            }

            private void LogError(string error)
            {
                StringBuilder sb = new();

                foreach (var path in inspectedPathForErrorDebug)
                {
                    sb.Append(path).Append("/");
                }

                sb.AppendLine(":");

                sb.AppendLine(error);

                Debug.LogError(sb.ToString());
            }

            public JsonBase TryDecodeString()
            {
                if (data.IsNullOrEmpty())
                {
                    //Debug.LogError("Data is null or empty");
                    return null;
                }

                data = Regex.Replace(data, @"\t|\n|\r", "");

                StringBuilder sb = new();
                int textIndex = 0;
                int openBrackets = 0;
                bool insideTextData = false;
                string variableName = "";

                List<JsonProperty> properties = new();

                List<JsonString> vals = new();

                var stage = JsonDecodingStage.DataTypeDecision;

                bool isaList = false;

                while (textIndex < data.Length)
                {
                    var c = data[textIndex];

                    switch (stage)
                    {
                        case JsonDecodingStage.DataTypeDecision:

                            if (c == ' ')
                                break;
                            
                            if (c == '{')
                                isaList = false;
                            else if (c == '[')
                                isaList = true;
                            else if (c == '"')
                            {
                                stage = JsonDecodingStage.ReadingVariableName;
                                sb.Clear();
                                break;
                            }
                            else
                            {
                                LogError("Is not collection. First symbol: " + c);
                                return null;
                            }

                            stage = isaList
                                ? JsonDecodingStage.ReadingData
                                : JsonDecodingStage.ExpectingVariableName;
                            
                            break;

                        case JsonDecodingStage.ExpectingVariableName:
                            if (c == ' ')
                                break;
                            
                            if (c == '}' || c == ']')
                            {

                                int left = data.Length - textIndex;

                                if (left > 5)
                                    LogError("End of collection detected a bit too early. Left {0} symbols: {1}".F(left, data[textIndex..]));
                                // End of collection instead of new element
                                break;
                            }

                            if (c == '"')
                            {
                                stage = JsonDecodingStage.ReadingVariableName;
                                sb.Clear();
                            }
                            else
                            {
                                LogError("Was expecting variable name: {0} ".F(data[textIndex..]));
                                return null;
                            }
                            
                            break;
                        case JsonDecodingStage.ReadingVariableName:

                            if (c != '"')
                                sb.Append(c);
                            else
                            {
                                variableName = sb.ToString();
                                stage = JsonDecodingStage.ExpectingTwoDots;
                            }

                            break;

                        case JsonDecodingStage.ExpectingTwoDots:

                            if (c == ':')
                            {
                                sb.Clear();
                                insideTextData = false;
                                stage = JsonDecodingStage.ReadingData;
                            }
                            else if (c != ' ')
                            {
                                LogError("Was Expecting two dots " + data[textIndex..]);
                                return null;
                            }


                            break;
                        case JsonDecodingStage.ReadingData:

                            bool needsClear = false;
                            
                            if (c == '"')
                            {
                                insideTextData = !insideTextData;
                               // break;
                            }

                            CheckForElementStartOrEnd();

                            void CheckForElementStartOrEnd()
                            {
                                if (insideTextData || (c == ' '))
                                    return;

                                if (c == '{' || c == '[')
                                {
                                    openBrackets++;
                                    return;
                                }
                               
                                var closed = c == '}' || c == ']';

                                if (closed)
                                    openBrackets--;

                                bool elementCompleted = (closed && openBrackets < 0) || ((c == ',') && openBrackets <= 0);

                                if (!elementCompleted)
                                    return;
                                
                                var dta = sb.ToString();

                                if (isaList)
                                {
                                    if (dta.Length > 0)
                                        vals.Add(new JsonString(dta));
                                }
                                else
                                    properties.Add(new JsonProperty(variableName, dta));

                                needsClear = true;

                                stage = isaList ? JsonDecodingStage.ReadingData : JsonDecodingStage.ExpectingVariableName;
                            }

                            if (!needsClear)
                                sb.Append(c);
                            else
                                sb.Clear();

                            break;
                    }

                    textIndex++;
                }

                if (isaList)
                    return new JsonList(vals);

                return properties.Count > 0 ? new JsonClass(properties) : null;
            }
        }

        protected class JsonProperty : JsonBase
        {
            public string name;

            public JsonBase data;

            public JsonProperty()
            {
                data = new JsonString();
            }

            public JsonProperty(string name, string data)
            {
                this.name = name;
                this.data = new JsonString(data);
            }

            public bool foldedOut;

            public override int GetCount() => 1;

            public override bool DecodeAll(ref JsonBase thisJson) => data.DecodeAll(ref data);

            public static JsonProperty inspected;

            public override string ToString() => name + (data.HasNestedData ? "{}" : data.GetNameForInspector());

            public override void Inspect()
            {
                inspected = this;

                pegi.Nl();

                if (data.GetCount() > 0)
                {
                    if (data.HasNestedData)
                        (name + " " + data.ToString()).PL().IsFoldout(ref foldedOut);

                    using (new PathAdd_ErrorDebug(ToString()))
                    {
                        DecodeOrInspectJson(ref data, foldedOut, name);
                    }
                }
                else
                    (name + " " + data.ToString()).PL().Write();

                pegi.Nl();

                inspected = null;
            }
        }

        protected class JsonList : JsonBase
        {

            private readonly List<JsonBase> elements;
            private readonly HashSet<int> foldedOut = new();

            private string previewValue = "";
            private bool previewFoldout;
            private int showElementsStart = 0;
            private const int MAX_ELEMENTS = 32;

            public override int GetCount() => elements.Count;

            public override string ToString() => "[{0}]".F(elements.Count);

            public override void Inspect()
            {

                using (new PathAdd_ErrorDebug(ToString()))
                {
                    if (elements.Count > 0)
                    {
                        var cl = elements[0] as JsonClass;
                        if (cl != null && cl.properties.Count > 0)
                        {
                            if (!previewFoldout && Icon.Config.Click(15).UnfocusOnChange())
                                previewFoldout = true;

                            if (previewFoldout)
                            {
                                "Select value to preview:".PL().Nl();

                                if (previewValue.Length > 0 && "NO PREVIEW VALUE".PL().Click().Nl())
                                {
                                    previewValue = "";
                                    previewFoldout = false;
                                }

                                foreach (var p in cl.properties)
                                {
                                    if (p.name.Equals(previewValue))
                                    {
                                        Icon.Next.Draw();
                                        if ("CURRENT: {0}".F(previewValue).PL().ClickUnFocus().Nl())
                                            previewFoldout = false;
                                    }
                                    else if (p.name.PL().Click().Nl())
                                    {
                                        previewValue = p.name;
                                        previewFoldout = false;
                                    }
                                }
                            }
                        }
                    }

                    pegi.Nl();

                    pegi.Indent();

                    string nameForElemenet = "";

                    var jp = JsonProperty.inspected;

                    if (jp != null)
                    {
                        string name = jp.name;
                        if (name[^1] == 's')
                        {
                            nameForElemenet = name[..^1];
                        }
                    }

                    if (elements.Count > MAX_ELEMENTS)
                    {
                        if (showElementsStart > 0)
                        {
                            if (Icon.Up.Click())
                                showElementsStart = Mathf.Max(0, showElementsStart - MAX_ELEMENTS);
                        }
                        else Icon.UpLast.Draw();

                        if (showElementsStart + MAX_ELEMENTS < elements.Count)
                        {
                            if (Icon.Down.Click())
                                showElementsStart += MAX_ELEMENTS;
                        }
                        else Icon.DownLast.Draw();

                        pegi.Nl();
                    }

                    int last = Math.Min(showElementsStart + MAX_ELEMENTS, elements.Count);

                    for (int i = showElementsStart; i < last; i++)
                    {

                        var val = elements[i];

                        bool contains = foldedOut.Contains(i);

                        if (val.HasNestedData)
                        {

                            var cl = val as JsonClass;

                            string preview = "";

                            if (cl != null && previewValue.Length > 0)
                            {
                                var p = cl.TryGetPropertByName(previewValue);

                                if (p != null)
                                    preview = p.data.GetNameForInspector(); //GetNameForInspector();
                                else
                                    preview = "missing";
                            }

                            ((preview.Length > 0 && !contains) ? "{1} ({0})".F(previewValue, preview) : "[{0} {1}]".F(nameForElemenet, i)).PL().IsFoldout(ref contains);
                            foldedOut.SetContains(i, contains: contains);
                        }

                        pegi.Nested_Inspect(() => DecodeOrInspectJson(ref val, contains)).Nl();
                        elements[i] = val;
                    }
                }

                pegi.UnIndent();
            }

            public override bool DecodeAll(ref JsonBase thisJson)
            {
                bool changes = false;

                for (int i = 0; i < elements.Count; i++)
                {
                    var val = elements[i];
                    if (val.DecodeAll(ref val))
                    {
                        elements[i] = val;
                        changes = true;
                    }
                }

                return changes;
            }

            public JsonList() { elements = new List<JsonBase>(); }

            public JsonList(List<JsonString> values) { this.elements = values.ToList<JsonBase>(); }
        }

        protected class JsonClass : JsonBase
        {
            public List<JsonProperty> properties;

            public JsonProperty TryGetPropertByName(string pname)
            {
                foreach (var p in properties)
                {
                    if (p.name.Equals(pname))
                        return p;
                }

                return null;
            }

            public override string ToString() => JsonProperty.inspected == null ? "  " :
                (JsonProperty.inspected.foldedOut ? "{" : (" {" + GetCount() + "} "));

            public override int GetCount() => properties.Count;

            public override void Inspect()
            {

                pegi.Indent();

                using (new PathAdd_ErrorDebug(ToString()))
                {
                    for (int i = 0; i < properties.Count; i++)
                        properties[i].Nested_Inspect();
                }

                pegi.UnIndent();
            }

            public JsonClass()
            {
                properties = new List<JsonProperty>();
            }

            public override bool DecodeAll(ref JsonBase thisJson)
            {

                bool changes = false;

                for (int i = 0; i < properties.Count; i++)
                {
                    var val = properties[i] as JsonBase;
                    changes |= val.DecodeAll(ref val);
                }

                return changes;
            }

            public JsonClass(List<JsonProperty> properties)
            {
                this.properties = properties;
            }
        }

        protected abstract class JsonBase : IPEGI, IGotCount
        {
            public JsonBase AsBase => this;

            public virtual JsonString AsJsonString => null;

            public abstract int GetCount();

            public abstract bool DecodeAll(ref JsonBase thisJson);

            public virtual bool HasNestedData => true;

            public abstract void Inspect();

        }
        #endregion

    }

}