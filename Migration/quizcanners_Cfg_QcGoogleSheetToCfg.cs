using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QuizCanners.Migration
{
    [Serializable]
    public class QcCSVSheetToCfg : IPEGI, INeedAttention, IPEGI_ListInspect
    {
        [SerializeField] public string Url;

      //  const string DEFAULT_URL = "https://docs.google.com/spreadsheets/d/e/XXX/pub?";

        [SerializeField] private string editUrl; // = "https://docs.google.com/spreadsheets/d/XXX/edit#gid=0";
  

        [SerializeField] public List<SheetPage> pages = new();
        [SerializeField] private int _selectedPage;

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

        [Serializable]
        public class SheetPage : IPEGI_ListInspect, IPEGI, IEnumerable<Row>, IGotCount
        {
            [SerializeField] private UnityEngine.Object _csvFile;
            public string pageName;
            public int pageIndex;

            internal List<string> Columns;

            public List<Row> Rows = new();

            public Row this[int index] => Rows[index];

            [NonSerialized] private UnityEngine.Networking.UnityWebRequest request;

            public bool IsDownloaded => request != null && request.isDone;

            public bool IsDownloading() => request != null && request.result == UnityEngine.Networking.UnityWebRequest.Result.InProgress;

            public void Clear() 
            {
                Rows.Clear();
                if (Columns != null)
                    Columns.Clear();
                request?.Dispose();
                request = null;
            }

            public void ToICfg(ICfgDecode receiver, Action onRawParced = null)
            {
                CheckWebRequest();

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
            }


            public void ToListOverride<T>(ref List<T> list, bool ignoreErrors = true) where T : ICfgDecode, new()
            {
                CheckWebRequest();

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
            }

            void Process(string text)
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

            private void CheckWebRequest()
            {
                if (IsDownloaded)
                {
                    var tmp = request;
                    request = null;
                    Rows.Clear();
                    Process(tmp.downloadHandler.text);

                    tmp.Dispose();
                }
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

            #region Inspector

            public override string ToString() => _csvFile ? "{0}.csv".F(_csvFile.name) : (pageName.IsNullOrEmpty() ? pageIndex.ToString() : pageName);

            public void InspectInList(ref int edited, int ind)
            {
                if (Icon.Enter.Click())
                    edited = ind;

                "Sheet Name".ConstL().Edit(ref pageName);
                pegi.Edit(ref _csvFile);
                if (!_csvFile)
                    "#gid=".ConstL().Edit(ref pageIndex);
                else
                    "Process".PL().Click(ProcessCSV);
            }

            private readonly pegi.EnterExitContext _context = new();
            private readonly pegi.CollectionInspectorMeta _rowsInspector = new("Records");

            void ProcessCSV() 
            {
                if (!QcUnity.TryGetFullPath(_csvFile, out var path))
                {
                    Debug.LogError("Failed to get path for " + _csvFile.ToString());
                    return;
                }

                Process(File.ReadAllText(path));
            }

            public void Inspect()
            {
                using (_context.StartContext())
                {
                    "CSV file".ConstL().Edit(ref _csvFile);

                    if (_csvFile)
                        "Process".PL().Click(ProcessCSV);
                    
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

            public int GetCount() => Rows == null ? 0 : Rows.Count;

            public IEnumerator<Row> GetEnumerator()
            {
                foreach (var r in Rows)
                    yield return r;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

         

            #endregion
        }

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
                    3 => "{0}, {1}, {2}".F( Data[0].ToString(), Data[1].ToString(), Data[2].ToString()),
                    _ => "{0}, {1}, {2} ... +{3}{4}".F(Data[0].ToString(), Data[1].ToString(), Data[2].ToString(), pegi.X_SYMBOL, Data.Count-3),
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
