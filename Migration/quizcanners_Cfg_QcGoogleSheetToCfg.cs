using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace QuizCanners.Migration
{
    [Serializable]
    public class QcCSVSheetToCfg : IPEGI, INeedAttention, IPEGI_ListInspect
    {
        const string CSV_URL = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&id={0}&gid={1}";

        [SerializeField] public string Url;

      //  const string DEFAULT_URL = "https://docs.google.com/spreadsheets/d/e/XXX/pub?";

        [SerializeField] private string editUrl; // = "https://docs.google.com/spreadsheets/d/XXX/edit#gid=0";
  
        [SerializeField] private int _selectedPage;

        #region Inspector

        public string NeedAttention()
        {
            return null;
        }

        private bool InspectUrlEndingNeedsCleanup()
        {
            var ind = Url.LastIndexOf("pub?", StringComparison.Ordinal);

            bool needsCleanup = (ind > 10 && ind < Url.Length - 4);

            if (needsCleanup && "Clear Url Ending".PL().Click())
            {
                Url = Url[..(ind + 4)];
            }

            if (!needsCleanup && ind==-1 && "Add /pub? ending".PL().Click()) 
            {
                Url += "/pub?";
            }

            return needsCleanup;
        }

        private static QcCSVSheetToCfg s_inspectedSheet;

        public override string ToString() => "Google Sheet";

        public void InspectInList(ref int edited, int index)
        {
            s_inspectedSheet = this;
            if (this.Click_Enter_Attention())
                edited = index;

            if (Url.IsNullOrEmpty())
            {
                "url(.csv): ".ConstL().Edit(ref Url);

                if (pegi.CopyPasteBuffer.IsNullOrEmpty() == false && pegi.CopyPasteBuffer.Contains("spreadsheets") && "From Clipboard".PL().Click())
                    Url = pegi.CopyPasteBuffer;
            }
            else
            {
                if (!InspectUrlEndingNeedsCleanup())
                {
                    if (pages.Count == 0)
                    {
                        int tmp = 0;
                        if ("gid=".ConstL().Edit(ref tmp) | Icon.Done.Click(toolTip: "The gid is 0"))
                        {
                            pages.Add(new SheetPage() { pageIndex = tmp, pageName = "Unnamed" });
                        }
                    }
                    else
                    {
                        if (SelectedPage == null)
                            "Sheet Page".ConstL().Select_Index(ref _selectedPage, pages);
                        else
                        {
                            "Name:".ConstL().Edit(ref SelectedPage.pageName);
                            "gid=".ConstL().Edit(ref SelectedPage.pageIndex);
                        }
                    }
                }
            }
        }

        private readonly pegi.EnterExitContext context = new();
        private readonly pegi.CollectionInspectorMeta _pagesMeta = new("Pages");

        void IPEGI.Inspect()
        {
            s_inspectedSheet = this;

            using (context.StartContext())
            {
                pegi.Nl();

                if (!context.IsAnyEntered)
                {
                    "Published CSV Url".ConstL().Edit(ref Url);

                    if (Url.IsNullOrEmpty())
                    {
                        if (pegi.CopyPasteBuffer.IsNullOrEmpty() == false && pegi.CopyPasteBuffer.Contains("spreadsheets") && "From Clipboard".PL().Click())
                            Url = pegi.CopyPasteBuffer;
                    }
                    else if (Icon.Copy.Click())
                        pegi.SetCopyPasteBuffer(Url);

                    pegi.FullWindow.DocumentationClickOpen(() =>
                        "GoogleSheet->File->Publish To Web-> Publish... Copy link for .csv document");

                    pegi.Nl();

                    if (Url.IsNullOrEmpty() == false)
                    {
                        InspectUrlEndingNeedsCleanup();
                        pegi.Nl();
                    }

                    pegi.Nl();

                    "Page:".ConstL().Select_Index(ref _selectedPage, pages);
                
                    pegi.Nl();
                }


                _pagesMeta.Enter_List(pages).Nl();

                if ("Link".PL().IsEntered())
                {
                    pegi.Nl();
                    "Sheet Edit Link".ConstL().Edit(ref editUrl);

                    if ("Open".PL().Click())
                        Application.OpenURL(editUrl);

                    pegi.Nl();
                }

                if (!context.IsAnyEntered && !editUrl.IsNullOrEmpty()  && "Open in Browser".PL().Click())
                    Application.OpenURL(editUrl);

                pegi.Nl();

               

            }

        }
        #endregion

      

        [SerializeField] public List<SheetPage> pages = new();
        public SheetPage SelectedPage
        {
            get
            {
                _selectedPage = Mathf.Min(_selectedPage, pages.Count - 1);
                return pages.TryGet(_selectedPage);
            }
            set
            {
                if (value == null)
                    return;

                var index = pages.IndexOf(value);
                if (index == -1)
                {
                    pages.Add(value);
                    _selectedPage = pages.Count - 1;
                }
                else
                    _selectedPage = index;
            }
        }
        [Serializable]
        public class SheetPage : GoogleSheetCSVBase, IPEGI, IPEGI_ListInspect
        {
            [SerializeField] private UnityEngine.Object _csvFile;
            public string pageName;
            public int pageIndex;

            [NonSerialized] private UnityEngine.Networking.UnityWebRequest request;

            public bool IsDownloaded => request != null && request.isDone;

            public bool IsDownloading() => request != null && request.result == UnityEngine.Networking.UnityWebRequest.Result.InProgress;

            protected override void OnExtracted()
            {
                
            }

            private void CheckWebRequest()
            {
                if (IsDownloaded)
                {
                    var tmp = request;
                    request = null;
                    Rows.Clear();
                    Split(tmp.downloadHandler.text);

                    tmp.Dispose();
                }
            }

            public override void ToICfg(ICfgDecode receiver, Action onRawParced = null)
            {
                CheckWebRequest();
                base.ToICfg(receiver, onRawParced);
            }

            public void StartDownload(QcCSVSheetToCfg parent)
            {
                if (parent.Url.IsNullOrEmpty())
                {
                    Debug.LogError("URL not set");
                    return;
                }

                request = UnityEngine.Networking.UnityWebRequest.Get("{0}gid={1}&single=true&output=csv".F(parent.Url, pageIndex.ToString()));
                request.SendWebRequest();
            }

            public IEnumerator DownloadingCoro(QcCSVSheetToCfg parent, Action onFinished = null)
            {
                if (!IsDownloading())
                {
                    StartDownload(parent);
                }

                while (IsDownloading())
                {
                    yield return null;
                }

                onFinished?.Invoke();
            }

            public override void ToListOverride<T>(ref List<T> list, bool ignoreErrors = true)
            {
                CheckWebRequest();
                base.ToListOverride(ref  list, ignoreErrors);
            }

            void ProcessCSV()
            {
                if (!QcUnity.TryGetFullPath(_csvFile, out var path))
                {
                    Debug.LogError("Failed to get path for " + _csvFile.ToString());
                    return;
                }

                Split(File.ReadAllText(path));
            }

            #region Inspector

            public void InspectInList(ref int edited, int ind)
            {
                if (Icon.Enter.Click())
                    edited = ind;

                "Sheet Name".ConstL().Edit(ref pageName);
                pegi.Edit(ref _csvFile);
                if (!_csvFile)
                    "#gid=".ConstL().Edit(ref pageIndex);
                else
                    "Split".PL().Click(ProcessCSV);
            }

            public override string ToString() => _csvFile ? "{0}.csv".F(_csvFile.name) : (pageName.IsNullOrEmpty() ? pageIndex.ToString() : pageName);

            private readonly pegi.EnterExitContext _context = new();
            private readonly pegi.CollectionInspectorMeta _rowsInspector = new("Records");

            public override void Inspect()
            {
                using (_context.StartContext())
                {
                    "CSV file".ConstL().Edit(ref _csvFile);

                    if (_csvFile)
                        "Split".PL().Click(ProcessCSV);

                    pegi.Nl();

                    if (!_csvFile)
                    {
                        InspectRequest();

                        "Raw Data".ConstL().IsConditionally_Entered(canEnter: IsDownloaded).Nl().If_Entered(() =>
                        {
                            request.downloadHandler.text.PL().Write_ForCopy_Big();
                        });
                    }

                    _rowsInspector.Edit_List(Rows).Nl();

                    return;

                    void InspectRequest()
                    {
                        if (request == null)
                        {
                            if ("Download".PL().Click().Nl())
                                StartDownload(s_inspectedSheet);
                            return;
                        }

                        if ("Clear Request".PL().Click())
                        {
                            request.Dispose();
                            request = null;
                            return;
                        }

                        if (!request.isDone)
                        {
                            "Thread state: ".F(Mathf.FloorToInt(request.downloadProgress * 100).ToString()).PL().Nl();

                            if ("Cancel Trhread".PL().Click().Nl())
                                request.Dispose();
                            return;
                        }

                        "Download finished".PL().Nl();
                    }
                }
            }

            #endregion

            public override void Clear()
            {
                Rows.Clear();
                Columns?.Clear();
                request?.Dispose();
                request = null;
            }
        }
    }

    [Serializable]
    public class GoogleSheetCSV_AnyoneWithLink : GoogleSheetCSVBase , IPEGI
    {
        
        public string spreadShitId;
        public int sheetId;

        const string CSV_URL = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&id={0}&gid={1}";
        const string OpenURL = "https://docs.google.com/spreadsheets/d/{0}/view?gid={1}";
        public bool IsValid => !spreadShitId.IsNullOrEmpty();

        [NonSerialized] private volatile string _dataRaw;
        [NonSerialized] private State _state;

        public enum State { None, NoLink, Loading, ReadyToProcess, FailedToLoad, FailedToParce,
            DataExtracted
        }

        public void OnChanged() => _state = State.None;

        public State Update(bool shouldLoad) 
        {
            switch (_state) 
            {
                case State.None:
                    if (spreadShitId.IsNullOrEmpty()) 
                    {
                        _state = State.NoLink;
                        break;
                    }

                    if (shouldLoad) 
                    {
                        StartLoad();
                    }
                    break;

                case State.Loading:

                    if (!_dataRaw.IsNullOrEmpty()) 
                    {
                        try 
                        {
                            _state = State.ReadyToProcess;
                            Split(_dataRaw);
                            _dataRaw = null;
                        } catch (Exception ex)                       {

                            _state = State.FailedToLoad;
                            Debug.LogException(ex);
                        }
                    }

                    break;
            }

            return _state;

        }

        protected override void OnExtracted()
        {
            Clear();
            _state = State.DataExtracted;
           // Debug.Log("Data Extracted");
        }

        protected void StartLoad()
        {
            _state = State.Loading;
            System.Threading.Tasks.Task.Run(() => GetData(CSV_URL.F(spreadShitId, sheetId)));
        }

        public bool TrySetFromLink(string url) 
        {
            Match match = Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
            if (!match.Success)
                return false;


            _state = State.None;

            spreadShitId = match.Groups[1].Value;
            
            Uri uri = new(url);
            string fragment = uri.Fragment;
            Match gidMatch = Regex.Match(fragment, @"gid=(\d+)");
            if (gidMatch.Success && int.TryParse(gidMatch.Groups[1].Value, out int res))
            {
                sheetId = res;
            }
            else
                sheetId = 0;

            return true;
        }

        async System.Threading.Tasks.Task GetData(string url)
        {
            using HttpClient client = new();

            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                _dataRaw = await response.Content.ReadAsStringAsync();
                //Debug.Log(_dataRaw);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("Failed to load "+ url);
            }
        }

        #region Inspector

        public override string ToString() => _state.ToString();

        private Gate.SystemTime _sinceFailedPaste;

        public pegi.ChangesToken Inspect_Short() 
        {
            var changes = pegi.ChangeTrackStart();

            if (!IsValid)
            {
                if (!pegi.CopyPasteBuffer.IsNullOrEmpty() && Icon.Paste.Click())
                {
                    if (!TrySetFromLink(pegi.CopyPasteBuffer))
                    {
                        _sinceFailedPaste = new Gate.SystemTime(Gate.InitialValue.StartArmed);
                        _sinceFailedPaste.Update();
                    }
                }

                if (_sinceFailedPaste != null && !_sinceFailedPaste.WillAllowIfTimePassed(10))
                    "Bad Link".PL().Write();
                else
                    "No Scene".ConstL().Write();

                return changes;
            }
          
            if (Icon.Delete.ClickConfirm("DeleteSpreadsheet"))
            {
                spreadShitId = null;
                sheetId = 0;
                _state = State.None;
            }

            if (Icon.Copy.Click())//spreadShitId.PL(pegi.Styles.Text.ClickableText).ClickLabel())
                pegi.CopyPasteBuffer = OpenURL.F(spreadShitId, sheetId);
            
            
            return changes;
        }

        public override void Inspect()
        {
            string tmp = "";

            var changes = pegi.ChangeTrackStart();

            "Anyone with a link".ConstL().Edit_Delayed(ref tmp).Nl(() => TrySetFromLink(tmp));
            "Spread Sheet".ConstL().Edit(ref spreadShitId).Nl();
            "Sheet Id".ConstL().Edit(ref sheetId).Nl();

            if (changes) 
            {
                if (_state == State.NoLink)
                    _state = State.None;
            }

            if ("Download".PL().Click().Nl())
            {
                StartLoad();
            }

            "State: {0}".F(_state).PL().Nl();

            switch (_state) 
            {
                case State.Loading:
                    Update(true);
                    break;


            }

            base.Inspect();
        }

 

        #endregion
    }

    public abstract class GoogleSheetCSVBase : IEnumerable<GoogleSheetCSVBase.Row>, IGotCount, IPEGI
    {
        [NonSerialized] internal List<string> Columns;
        [NonSerialized] public List<Row> Rows = new();

        public Row this[int index] => Rows[index];

        public int GetCount() => Rows == null ? 0 : Rows.Count;

        public IEnumerator<Row> GetEnumerator()
        {
            foreach (var r in Rows)
                yield return r;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public virtual void ToICfg(ICfgDecode receiver, Action onRawParced = null)
        {
            for (int r = 0; r < Rows.Count; r++) // var row in rows)
            {
                var row = Rows[r];

                for (int i = 0; i < Columns.Count; i++)
                {
                    try
                    {
                        receiver.DecodeTag(Columns[i], row.Data[i]);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                onRawParced?.Invoke();
            }

            OnExtracted();
        }


        public virtual void ToListOverride<T>(ref List<T> list, bool ignoreErrors = true) where T : ICfgDecode, new()
        {
            list.ForceSetCount(Rows.Count);

            for (int r = 0; r < Rows.Count; r++) // var row in rows)
            {
                var row = Rows[r];
                var el = list[r];

                el ??= new T();

                if (ignoreErrors)
                {
                    for (int i = 0; i < Columns.Count; i++)
                    {
                        try
                        {
                            el.DecodeTag(Columns[i], row.Data[i]);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }

                }
                else
                {
                    for (int i = 0; i < Columns.Count; i++)
                    {
                        el.DecodeTag(Columns[i], row.Data[i]);
                    }
                }

                list[r] = el;
            }

            OnExtracted();
        }

        protected abstract void OnExtracted();

        protected void Split(string text)
        {
            StringReader reader = new(text);

            Rows.Clear();

            using (reader)
            {
                string line;
                line = reader.ReadLine();

                if (line.IsNullOrEmpty())
                {
                    Debug.LogError("No Column headers found. List is empty");
                    return;
                }

                var accumulatedCell = new StringBuilder();
                Columns = line.Split(',').Select(v => v.Trim()).ToList();
                int rowIndex = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    var rawCells = new List<string>();
                    try
                    {
                        bool parcingALine = true;
                        bool isAString = false;

                        while (parcingALine)
                        {
                            parcingALine = false;

                            for (int i = 0; i < line.Length; i++)
                            {
                                var symbol = line[i];

                                if (!isAString && symbol == ',')
                                {
                                    rawCells.Add(accumulatedCell.ToString().Trim());
                                    accumulatedCell.Clear();
                                    continue;
                                }

                                if (symbol == '"')
                                {
                                    if (line.Length > (i + 1) && line[i + 1] == '"')
                                    {
                                        accumulatedCell.Append('"');
                                        i++;
                                    }
                                    else
                                        isAString = !isAString;

                                    continue;
                                }

                                accumulatedCell.Append(symbol);
                            }

                            if (isAString && ((line = reader.ReadLine()) != null))
                            {
                                parcingALine = true;
                                accumulatedCell.Append(pegi.EnvironmentNl);
                            }
                        }

                        rawCells.Add(accumulatedCell.ToString());
                        accumulatedCell.Clear();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex.Message + ex.StackTrace + ", at row " + rowIndex);
                    }

                    try
                    {
                        AddCells(rawCells);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.LogError("at row:" + rowIndex);
                    }

                    rowIndex++;
                }
            }
        }

        private void AddCells(List<string> rawCells)
        {
            List<CfgData> cellsInRow = new();

            int columnIndex = 0;
            foreach (string cell in rawCells)
            {
                if (columnIndex >= Columns.Count)
                    break;

                cellsInRow.Add(new CfgData(cell));
                columnIndex++;
            }

            Rows.Add(new Row(cellsInRow));
        }

        public virtual void Clear()
        {
            Rows.Clear();
            Columns?.Clear();
        }

        #region Inspector

        public virtual void Inspect()
        {
            "Columnst".ConstL().Edit_List(Columns).Nl();
            "Rows".ConstL().Edit_List(Rows).Nl();
        }

        #endregion

        public class Row : IPEGI
        {
            public List<CfgData> Data;

            public CfgData this[int index] => Data[index];

    

            #region Inspector

            public override string ToString()
            {
                if (Data.IsNullOrEmpty())
                    return "Empty";

                return Data.Count switch
                {
                    1 => Data[0].ToString(),
                    2 => "{0} - {1}".F(Data[0], Data[1]),
                    3 => "{0}, {1}, {2}".F(Data[0].ToString(), Data[1].ToString(), Data[2].ToString()),
                    _ => "{0}, {1}, {2} ... +{3}{4}".F(Data[0].ToString(), Data[1].ToString(), Data[2].ToString(), pegi.SYMBOLS.X_SYMBOL, Data.Count - 3),
                };
            }

            private readonly pegi.CollectionInspectorMeta _cellsMeta = new("Cells");
            public void Inspect()
            {
                _cellsMeta.Edit_List(Data).Nl();
            }

            #endregion

            public Row(List<CfgData> list)
            {
                Data = list;
            }
        }

    }

}
