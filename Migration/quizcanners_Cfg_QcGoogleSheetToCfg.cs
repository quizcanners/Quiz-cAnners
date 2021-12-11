using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
//using UnityEngine.Networking;

namespace QuizCanners.Migration
{
#pragma warning disable IDE0044 // Add readonly modifier
    
    [Serializable]
    public class QcGoogleSheetToCfg : IPEGI, INeedAttention, IPEGI_ListInspect
    {
        private List<string> _columns;

        public List<string> Columns
        {
            get
            {
                ReadIfDirty();
                return _columns;
            }
        }

        private List<Row> rows = new List<Row>();

        public bool IsDownloaded => request != null && request.isDone;

        #region Downloading

        const string DEFAULT_URL = "https://docs.google.com/spreadsheets/d/e/XXX/pub?";

        [SerializeField] private string editUrl; // = "https://docs.google.com/spreadsheets/d/XXX/edit#gid=0";
        [SerializeField] private string url;

        [SerializeField] public List<SheetPage> pages = new List<SheetPage>();
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

        [Serializable]
        public class SheetPage : IGotReadOnlyName, IPEGI_ListInspect
        {
            public string pageName;
            public int pageIndex;

            public void InspectInList(ref int edited, int ind)
            {
                "Name".PegiLabel(40).Edit(ref pageName);
                "#gid=".PegiLabel(40).Edit(ref pageIndex);
            }

            public string GetReadOnlyName() => pageName;
        }

        [NonSerialized] private UnityEngine.Networking.UnityWebRequest request;

        public void StartDownload(SheetPage page)
        {
            if (url.IsNullOrEmpty()) 
            {
                Debug.LogError("URL not set");
                return;
            }
            request = UnityEngine.Networking.UnityWebRequest.Get("{0}gid={1}&single=true&output=csv".F(url, page.pageIndex.ToString()));
            request.SendWebRequest();
        }

        public bool IsDownloading() => request != null && request.result== UnityEngine.Networking.UnityWebRequest.Result.InProgress;

        public IEnumerator DownloadingCoro(Action onFinished = null)
        {
            if (!IsDownloading())
            {
                if (QcLog.IfNull(SelectedPage, nameof(DownloadingCoro))) 
                {
                    yield break;
                }
                StartDownload(SelectedPage);
            }

            while (IsDownloading())
            {
                yield return null;
            }

            onFinished?.Invoke();
        }

        #endregion

        #region Reading

        public void ToListOverride<T>(ref List<T> list, bool ignoreErrors = true) where T : ICfgDecode, new()
        {
            ReadIfDirty();

            list.ForceSetCount(rows.Count);

            for (int r = 0; r < rows.Count; r++) // var row in rows)
            {
                var row = rows[r];
                var el = list[r];

                if (el == null)
                    el = new T();

                if (ignoreErrors)
                {
                    for (int i = 0; i < _columns.Count; i++)
                    {
                        try
                        {
                            el.DecodeTag(_columns[i], row.data[i]);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }

                }
                else
                {

                    for (int i = 0; i < _columns.Count; i++)
                    {
                        el.DecodeTag(_columns[i], row.data[i]);
                    }
                }

                list[r] = el;
            }

        }

        private void ReadIfDirty()
        {
            if (IsDownloaded)
            {
                var tmp = request;
                request = null;
                Read(tmp);
            }
        }

        private void Read(UnityEngine.Networking.UnityWebRequest content)
        {
            rows.Clear();

            var reader = new StringReader(content.downloadHandler.text);

            using (reader)
            {
                string line;
                line = reader.ReadLine();

                var accumulatedCell = new StringBuilder();

                if (!line.IsNullOrEmpty())
                {
                    _columns = line.Split(',').Select(v => v.Trim()).ToList();
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

                                    if (!isAString)
                                    {
                                        if (symbol == ',')
                                        {
                                            rawCells.Add(accumulatedCell.ToString().Trim());
                                            accumulatedCell.Clear();
                                        }
                                        else BracketsOrAppend();
                                    }
                                    else BracketsOrAppend();
                                    
                                    void BracketsOrAppend() 
                                    {
                                        if (symbol == '"')
                                        {
                                            if (line.Length > (i + 1) && line[i + 1] == '"')
                                            {
                                                accumulatedCell.Append('"');
                                                i++;
                                            }
                                            else
                                                isAString = !isAString;
                                        }
                                        else
                                            accumulatedCell.Append(symbol);
                                    }
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
                } else 
                {
                    Debug.LogError("To COlumn headers found. List is empty");
                }
            }
        }

        private void AddCells(List<string> rawCells)
        {
            List<CfgData> cellsInRow = new List<CfgData>();
           
            int columnIndex = 0;
            foreach (string cell in rawCells)
            {
                if (columnIndex >= _columns.Count)
                    break;

                cellsInRow.Add(new CfgData(cell));
                columnIndex++;
            }

            rows.Add(new Row(cellsInRow));
        }


        internal class Row
        {
            public List<CfgData> data;

            public Row(List<CfgData> list)
            {
                data = list;
            }
        }

        #endregion

        #region Inspector

        public string NeedAttention()
        {
            return null;
        }

        private bool InspectUrlEndingNeedsCleanup() 
        {
            var ind = url.LastIndexOf("pub?", StringComparison.Ordinal);

            bool needsCleanup = (ind > 10 && ind < url.Length - 4);

            if (needsCleanup && "Clear Url Ending".PegiLabel().Click())
            {
                url = url.Substring(startIndex: 0, length: ind + 4);
            }

            return needsCleanup;
        }

        public void InspectInList(ref int edited, int index)
        {
            if (this.Click_Enter_Attention())
                edited = index;

            if (url.IsNullOrEmpty())
            {
                "url(.csv): ".PegiLabel(90).Edit(ref url);

                if (pegi.CopyPasteBuffer.IsNullOrEmpty() == false && pegi.CopyPasteBuffer.Contains("spreadsheets") && "From Clipboard".PegiLabel().Click())
                    url = pegi.CopyPasteBuffer;
            }
            else
            {
                if (!InspectUrlEndingNeedsCleanup())
                {
                    if (pages.Count == 0)
                    {
                        int tmp = 0;
                        if ("gid=".PegiLabel(40).Edit(ref tmp) | Icon.Done.Click(toolTip: "The gid is 0"))
                        {
                            pages.Add(new SheetPage() { pageIndex = tmp, pageName = "Unnamed" });
                        }
                    }
                    else
                    {
                        if (SelectedPage == null)
                            "Sheet Page".PegiLabel(90).Select_Index(ref _selectedPage, pages);
                        else
                        {
                            
                            "Name:".PegiLabel(30).Edit(ref SelectedPage.pageName);
                            "gid=".PegiLabel(25).Edit(ref SelectedPage.pageIndex);
                        }
                    }
                }
            }
        }

        private pegi.EnterExitContext context = new pegi.EnterExitContext();

        public void Inspect()
        {
            using (context.StartContext())
            {
                pegi.Nl();

                if (!context.IsAnyEntered)
                {
                    "Published CSV Url".PegiLabel(120).Edit(ref url);

                    if (url.IsNullOrEmpty())
                    {
                        if (pegi.CopyPasteBuffer.IsNullOrEmpty() == false && pegi.CopyPasteBuffer.Contains("spreadsheets") && "From Clipboard".PegiLabel().Click())
                            url = pegi.CopyPasteBuffer;
                    }
                    else if (Icon.Copy.Click())
                        pegi.SetCopyPasteBuffer(url);

                    pegi.FullWindow.DocumentationClickOpen(() =>
                        "GoogleSheet->File->Publish To Web-> Publish... Copy link for .csv document");

                    pegi.Nl();

                    if (url.IsNullOrEmpty() == false)
                    {
                        InspectUrlEndingNeedsCleanup();
                        pegi.Nl();
                    }

                    pegi.Nl();

                    if (request == null)
                    {
                        "Page:".PegiLabel(40).Select_Index(ref _selectedPage, pages);

                        if (pages.Count > _selectedPage && "Download".PegiLabel().Click().Nl())
                            StartDownload(pages[_selectedPage]);
                    }
                    else
                    {
                        if ("Clear Request".PegiLabel().Click())
                        {
                            request.Dispose();
                            request = null;
                        }
                        else
                        if (request.isDone)
                        {
                            "Download finished".PegiLabel().Nl();
                        }
                        else
                        {
                            "Thread state: ".F(Mathf.FloorToInt(request.downloadProgress * 100).ToString()).PegiLabel().Nl();

                            if ("Cancel Trhread".PegiLabel().Click().Nl())
                                request.Dispose();
                        }
                    }
                
                    pegi.Nl();
                }

                "Raw Data".PegiLabel().IsConditionally_Entered(canEnter: IsDownloaded).Nl().If_Entered(() =>
                {
                    request.downloadHandler.text.PegiLabel().Write_ForCopy_Big();
                });

                if ("Link".PegiLabel().IsEntered())
                {
                    pegi.Nl();
                    "Sheet Edit Link)".PegiLabel(100).Edit(ref editUrl);

                    if ("Open".PegiLabel().Click())
                        Application.OpenURL(editUrl);

                    pegi.Nl();
                }

                if (context.IsAnyEntered == false && editUrl.IsNullOrEmpty() == false && "Open in Browser".PegiLabel().Click())
                    Application.OpenURL(editUrl);

                pegi.Nl();

                "Pages".PegiLabel().Enter_List(pages).Nl();

            }

        }
        #endregion

    }
}
