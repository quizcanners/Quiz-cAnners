using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                "Name".PegiLabel(40).edit(ref pageName);
                "#gid=".PegiLabel(40).edit(ref pageIndex);
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

            var lines = new System.IO.StringReader(content.downloadHandler.text);
            using (lines)
            {
                string line;
                line = lines.ReadLine();
                rows.Clear();
                if (line != null)
                {
                    _columns = line.Split(',').Select(v => v.Trim()).ToList();
                    int rowIndex = 0;
                    while ((line = lines.ReadLine()) != null)
                    {
                        var rawCells = new List<string>();
                        try
                        {
                            bool good = true;
                            string accumulatedCell = "";
                            for (int i = 0; i < line.Length; i++)
                            {
                                if (line[i] == ',' && good)
                                {
                                    rawCells.Add(accumulatedCell.Trim());
                                    accumulatedCell = "";
                                }
                                else if (line.Length > (i + 1) && line[i] == '"' && line[i + 1] == '"')
                                {
                                    accumulatedCell += '"';
                                    i++;
                                }
                                else if (line[i] == '"')
                                {
                                    good = !good;
                                }
                                else
                                    accumulatedCell += line[i];
                            }

                            rawCells.Add(accumulatedCell);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex.Message + ex.StackTrace + ", at row " + rowIndex);
                        }

                        List<CfgData> cellsInRow = new List<CfgData>();
                        try
                        {
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
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message + e.StackTrace + ", at row " + rowIndex);
                        }

                        rowIndex++;
                    }
                }
            }
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
                "url(.csv): ".PegiLabel(90).edit(ref url);

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
                        if ("gid=".PegiLabel(40).edit(ref tmp) | icon.Done.Click(toolTip: "The gid is 0"))
                        {
                            pages.Add(new SheetPage() { pageIndex = tmp, pageName = "Unnamed" });
                        }
                    }
                    else
                    {
                        if (SelectedPage == null)
                            "Sheet Page".PegiLabel(90).select_Index(ref _selectedPage, pages);
                        else
                        {
                            
                            "Name:".PegiLabel(30).edit(ref SelectedPage.pageName);
                            "gid=".PegiLabel(25).edit(ref SelectedPage.pageIndex);
                        }
                    }
                }
            }
        }

        //private int _inspectedStuff = -1;

        public void Inspect()
        {
            pegi.nl();

            if (request == null)
            {
                "Page:".PegiLabel(40).select_Index(ref _selectedPage, pages);

                if (pages.Count > _selectedPage && "Download".PegiLabel().Click().nl())
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
                    "Download finished".PegiLabel().nl();
                }
                else
                {
                    "Thread state: ".F(Mathf.FloorToInt(request.downloadProgress * 100).ToString()).PegiLabel().nl();

                    if ("Cancel Trhread".PegiLabel().Click().nl())
                        request.Dispose();

                }
            }
            
            pegi.nl();

         
            "Published CSV Url".PegiLabel(120).edit(ref url);

            if (url.IsNullOrEmpty())
            {
                if (pegi.CopyPasteBuffer.IsNullOrEmpty() == false && pegi.CopyPasteBuffer.Contains("spreadsheets") && "From Clipboard".PegiLabel().Click())
                    url = pegi.CopyPasteBuffer;
            }
            else if (icon.Copy.Click())
                pegi.SetCopyPasteBuffer(url);

            pegi.FullWindow.DocumentationClickOpen(() =>
                "GoogleSheet->File->Publish To Web-> Publish... Copy link for .csv document");

            pegi.nl();

            if (url.IsNullOrEmpty() == false)
            {
                InspectUrlEndingNeedsCleanup();
                pegi.nl();
            }

            "Pages".PegiLabel().edit_List(pages).nl();

            if ("Link".PegiLabel().isFoldout())
            {
                pegi.nl();
                "Sheet Edit Link)".PegiLabel(100).edit(ref editUrl);

                if ("Open".PegiLabel().Click())
                    Application.OpenURL(editUrl);

                pegi.nl();
            } else if (editUrl.IsNullOrEmpty() == false && "Edit Googl Sheet".PegiLabel().Click()) 
                Application.OpenURL(editUrl);
            

            pegi.nl();
            

        }
        #endregion

    }
}
