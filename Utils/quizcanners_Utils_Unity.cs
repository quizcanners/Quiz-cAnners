using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Graphic = UnityEngine.UI.Graphic;
using Object = UnityEngine.Object;
using System.Reflection;
using UnityEngine.SceneManagement;
using QuizCanners.Inspect;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;


#if UNITY_EDITOR
using  UnityEditor;

#endif

namespace QuizCanners.Utils {

#pragma warning disable IDE0019 // Use pattern matching

    public static partial class QcUnity {

        public const string SO_CREATE_MENU = "Quiz Canners/";

        public const string SO_CREATE_MENU_MODULES = "Quiz Canners/Modules/";

        public static T Instantiate<T>(string name = null) where T : MonoBehaviour
        {

            var go = new GameObject(name.IsNullOrEmpty() ? typeof(T).ToPegiStringType() : name);
            return go.AddComponent<T>();
        }

        #region Lists
        public static void RemoveEmpty<T>(List<T> list) where T : Object {

            for (var i = list.Count - 1; i >= 0; i--)
                if (!list[i])
                    list.RemoveAt(i);

        }

        #endregion

        #region Scriptable Objects

        private const string ScrObjExt = ".asset";


        public static T CreateScriptableObjectInTheSameFolder<T>(ScriptableObject el, string name, bool refreshDatabase = true) where T : ScriptableObject
        {

            T added;

#if UNITY_EDITOR

            var path = AssetDatabase.GetAssetPath(el);

            if (path.IsNullOrEmpty()) return null;

            added = ScriptableObject.CreateInstance<T>();

            var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path.Replace(Path.GetFileName(path), name + ScrObjExt));

            AssetDatabase.CreateAsset(added, assetPathAndName);

            added.name = name;

            if (!refreshDatabase)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#else
            added = ScriptableObject.CreateInstance<T>();
#endif

            return added;
        }

        public static T DuplicateScriptableObject<T>(T el, bool refreshDatabase = true) where T : ScriptableObject
        {
            T added;

#if UNITY_EDITOR

            var path = AssetDatabase.GetAssetPath(el);

            if (path.IsNullOrEmpty()) return null;

            added = ScriptableObject.CreateInstance(el.GetType()) as T;

            var oldName = Path.GetFileName(path);

            if (oldName.IsNullOrEmpty())
                return added;

            int len = oldName.Length;

            var assetPathAndName =
                AssetDatabase.GenerateUniqueAssetPath(
                    Path.Combine(
                        path[..^len],
                        oldName[..(len - ScrObjExt.Length)] + ScrObjExt));

            AssetDatabase.CreateAsset(added, assetPathAndName);

            var newName = Path.GetFileName(assetPathAndName);

            if (newName != null)
            {
                added.name = newName[..^ScrObjExt.Length];
            }

            if (refreshDatabase)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

#else
            added = ScriptableObject.CreateInstance(el.GetType()) as T;
#endif

            return added;
        }

        public static T CreateAndAddScriptableObjectAsset<T>(List<T> objs, string path, string name)
            where T : ScriptableObject => CreateScriptableObjectAsset<T, T>(path, name, objs);

        public static T CreateScriptableObjectAsset<T>(List<T> list, string path, string name, Type t) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance(t) as T;

            SaveScriptableObjectAsAsset(asset, path, name, list);

            return asset;
        }

        public static T CreateScriptableObjectAsset<T>(string name, bool addSceneName) where T : ScriptableObject
        {
            if (!TryGetActiveScenePath(out var path)) 
            {
                Debug.LogError("Failed to get active path");
                return null;
            }

            var asset = ScriptableObject.CreateInstance<T>();

            if (addSceneName)
                name += "_" + SceneManager.GetActiveScene().name;

            SaveScriptableObjectAsAsset<T, T>(asset, path, name);

            return asset;
        }

        public static T CreateScriptableObjectAsset<T>(string path, string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();

            SaveScriptableObjectAsAsset<T, T>(asset, path, name);

            return asset;
        }

        public static T CreateScriptableObjectAsset<T, TG>(string path, string name, List<TG> optionalList = null) where T : TG where TG : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();

            SaveScriptableObjectAsAsset(asset, path, name, optionalList);

            return asset;
        }

        private static void SaveScriptableObjectAsAsset<T, TG>(T asset, string path, string name, List<TG> optionalList = null)
            where T : TG where TG : ScriptableObject {


            optionalList?.Add(asset);

#if UNITY_EDITOR

            if (!path.Contains("Assets"))
                path = Path.Combine("Assets", path);

            var fullPath = Path.Combine(QcFile.OutsideOfAssetsFolder, path);

            try
            {
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Couldn't create Directory {0} : {1}", fullPath, ex));
                return;
            }

            var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, name + ".asset"));

            try
            {
                AssetDatabase.CreateAsset(asset, assetPathAndName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("Couldn't create Scriptable Object {0} : {1}", assetPathAndName, ex));
            }
#endif
        }

#endregion

        #region External Communications

        public static void SendEmail(string to) => Application.OpenURL("mailto:" + to);

        public static void SendEmail(string email, string subject, string body) =>
            Application.OpenURL(string.Format("mailto:{0}?subject={1}&body={2}", email, subject.MyEscapeUrl(), body.MyEscapeUrl()));

        private static string MyEscapeUrl(this string url) => System.Net.WebUtility.UrlEncode(url).Replace("+", "%20");

        public static void OpenBrowser(string address) => Application.OpenURL(address);

        #endregion

        #region Timing

        public static double TimeSinceStartup() =>
#if UNITY_EDITOR
            EditorApplication.timeSinceStartup;
#else
            Time.realtimeSinceStartup;
#endif

        #endregion

        public static float GetLength(this NavMeshPath path) 
        {
            float totalLength = 0;
            Vector3 from = path.corners[0];

            for (int i=1; i<path.corners.Length; i++) 
            {
                var next = path.corners[i];
                totalLength += Vector3.Distance(from, next);
                from = next;
            }

            return totalLength;
        }

        public static void RefreshLayoutHack(MonoBehaviour tf)
        {
            if (!tf)
                Debug.LogError("Transform is null");
            else if (!tf.gameObject.activeInHierarchy)
                Debug.LogWarning("{0} is not active".F(tf.gameObject.name));
            else
                tf.StartCoroutine(RefreshLayoutHack(tf.gameObject));
        }

        private static System.Collections.IEnumerator RefreshLayoutHack(GameObject refresh)
        {
            yield return null;
            refresh.SetActive(false);
            refresh.SetActive(true);
        }

        #region Scenes 

        public static bool TryGetActiveScenePath(out string path) 
        {
#if UNITY_EDITOR
            path = SceneManager.GetActiveScene().path;

            var namePart = path.LastIndexOfAny(new char[] {'/', '\\'});

            if (namePart > 0)
                path = path[..namePart];

            return true;
#else
            path = "";
            return false;
#endif
        }

        #endregion

        #region Rendering

        private static readonly Gate.Frame _cameraCullingCache = new(Gate.InitialValue.StartArmed);

        private static readonly Dictionary<Camera, Dictionary<Vector3, bool>> s_camsCulling = new();

        public static bool IsMouseOutsideViewArea(this Camera cam, Vector3 mousePos) 
        {
            var view = cam.ScreenToViewportPoint(mousePos);
            return view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;
        }

        public static bool IsInCameraViewArea(this Camera cam, Vector3 worldPosition, float objectSize = 1, float maxDistance = -1)
        {
            if (_cameraCullingCache.TryEnter()) 
            {
                s_camsCulling.Clear();
            }

            if (!cam)
            {
                return true;
            }

            var camTf = cam.transform;

            float distanceToCamera = Vector3.Distance(camTf.position + camTf.forward * cam.nearClipPlane, worldPosition);

            if (distanceToCamera < objectSize + 10)
            {
                return true;
            }

            if (maxDistance > 0 && distanceToCamera > maxDistance)
            {
                return false;
            }    

            var dic = s_camsCulling.GetOrCreate(cam);

            if (dic.TryGetValue(worldPosition, out var resul))
                return resul;

            var pos = cam.WorldToViewportPoint(worldPosition);

            bool isVisible = (pos.x >= -0.1f && pos.x <= 1.1f && pos.y >= -0.1f && pos.y <= 1.1f);
                
            dic[worldPosition] = isVisible;
            return isVisible;
        }

        #endregion

        #region Raycasts

        public static Ray RaySegment(Vector3 from, Vector3 to, out float distance)
        {
            var vec = to - from;
            distance = vec.magnitude;
            return new Ray(from, direction: vec);
        }



        #endregion

        #region Color 

        public static Color Alpha(this Color col, float alpha)
        {
            col.a = alpha;
            return col;
        }

        public static Color ScaleColor(this Color col, float brightness)
        {
            col.r *= brightness;
            col.g *= brightness;
            col.b *= brightness;
            return col;
        }

        #endregion

        #region UI


        #region Rect Transform

        public static void SetAnchorsKeepPosition(this RectTransform rectTransform, Vector2 min, Vector2 max)
        {

            Vector3 tempPos = rectTransform.position;

            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;

            rectTransform.position = tempPos;
        }

        public static void SetPivotTryKeepPosition(this RectTransform rectTransform, float pivotX, float pivotY) =>
            rectTransform.SetPivotTryKeepPosition(new Vector2(pivotX, pivotY));

        public static void SetPivotTryKeepPosition(this RectTransform rectTransform, Vector2 pivot)
        {
            if (!rectTransform) return;
            var size = rectTransform.rect.size;
            var deltaPivot = rectTransform.pivot - pivot;
            var deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y) * rectTransform.localScale.x;
            rectTransform.pivot = pivot;
            rectTransform.localPosition -= deltaPosition;
        }

        public static Rect TryGetAtlasedAtlasedUvs(this Sprite sprite)
        {

            if (!Application.isPlaying || !sprite)
                return Rect.MinMaxRect(0, 0, 1, 1);

            var tex = sprite.texture;

            var rect = (sprite.packed && sprite.packingMode != SpritePackingMode.Tight) ? sprite.textureRect : sprite.rect;

            var scaler = new Vector2(1f / tex.width, 1f / tex.height);

            rect.size *= scaler;
            rect.position *= scaler;

            return rect;
        }

        public static void SetVisibleAndInteractable(this CanvasGroup canvasGroup, bool value)
        {
            canvasGroup.alpha = value ? 1 : 0;
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        public static void SetSizeDeltaX(this RectTransform rectTransform, float x)
        {
            var sd = rectTransform.sizeDelta;
            sd.x = x;
            rectTransform.sizeDelta = sd;
        }

        public static void SetSizeDeltaY(this RectTransform rectTransform, float y)
        {
            var sd = rectTransform.sizeDelta;
            sd.y = y;
            rectTransform.sizeDelta = sd;
        }

        public static void SetAnchoredPositionX(this RectTransform rectTransform, float x)
        {
            var sd = rectTransform.anchoredPosition;
            sd.x = x;
            rectTransform.anchoredPosition = sd;
        }

        public static void SetAnchoredPositionY(this RectTransform rectTransform, float y)
        {
            var sd = rectTransform.anchoredPosition;
            sd.y = y;
            rectTransform.anchoredPosition = sd;
        }

        #endregion

        public static void TrySet(this List<UnityEngine.UI.Image> list, Sprite to)
        {
            if (!list.IsNullOrEmpty())
                foreach (var e in list)
                    if (e)
                        e.sprite = to;
        }

        public static List<T> CreateUiElement<T>(GameObject[] targets = null, Action<T> onCreate = null) where T : Component
        {

            List<T> created = new();

            bool createdForSelection = false;

            if (targets.Length > 0)
            {
                foreach (var go in targets)
                {
                    if (go.GetComponentInParent<Canvas>())
                    {
                        var el = CreateUiElement<T>(go);
                        onCreate?.Invoke(el);
                        created.Add(el);
                        createdForSelection = true;
                    }
                }
            }

            if (!createdForSelection)
            {
                var canvas = Object.FindFirstObjectByType<Canvas>();

                if (!canvas)
                    canvas = new GameObject("Canvas").AddComponent<Canvas>();

                created.Add(CreateUiElement<T>(canvas.gameObject));
            }

            return created;
        }

        private static T CreateUiElement<T>(GameObject parent) where T : Component
        {
            var rg = new GameObject(typeof(T).ToString().SimplifyTypeName()).AddComponent<T>();
            var go = rg.gameObject;
            var canvRend = go.GetComponent<CanvasRenderer>();
            if (!canvRend)
                canvRend = go.AddComponent<CanvasRenderer>();

            canvRend.cullTransparentMesh = true;

#if UNITY_EDITOR
            GameObjectUtility.SetParentAndAlign(go, parent);
            Undo.RegisterCreatedObjectUndo(go, "Created " + go.name);
            Selection.activeObject = go;
#endif

            return rg;
        }

        public static bool TrySetAlpha_DisableGameObjectIfZero(this Graphic graphic, float alpha)
        {
            if (!graphic) 
                return false;

            var ret = graphic.TrySetAlpha(alpha);

            graphic.gameObject.SetActive(alpha > 0.01f);

            return ret;

        }

        public static bool TrySetAlpha(this Graphic graphic, float alpha)
        {
            if (!graphic) 
                return false;

            var col = graphic.color;

            col.a = alpha;
            graphic.color = col;
            return true;

        }

        public static void TrySetAlpha_DisableGameObjectIfZero<T>(this List<T> graphics, float alpha) where T : Graphic
        {
            if (graphics.IsNullOrEmpty()) 
                return;

            foreach (var g in graphics)
                g.TrySetAlpha_DisableGameObjectIfZero(alpha);
        }

        public static void TrySetAlpha<T>(this List<T> graphics, float alpha) where T : Graphic
        {
            if (graphics.IsNullOrEmpty()) 
                return;

            foreach (var g in graphics)
                g.TrySetAlpha(alpha);
        }

        public static bool TrySetColor_RGB(this Graphic graphic, Color color)
        {
            if (!graphic) 
                return false;

            color.a = graphic.color.a;
            graphic.color = color;
            return true;
        }

        public static void TrySetColor_RGB<T>(this List<T> graphics, Color color) where T : Graphic
        {
            if (graphics.IsNullOrEmpty()) 
                return;

            foreach (var g in graphics)
                g.TrySetColor_RGB(color);
        }

        public static bool TrySetColor_RGBA(this Graphic graphic, Color color)
        {
            if (!graphic) 
                return false;
            graphic.color = color;
            return true;
        }

        public static void TrySetColor_RGBA<T>(this List<T> graphics, Color color) where T : Graphic
        {
            if (graphics.IsNullOrEmpty()) 
                return;

            foreach (var g in graphics)
                g.TrySetColor_RGBA(color);
        }

        #endregion

        #region Components & GameObjects

        public static bool UnitBoundsContainsPoint(this Transform posNSize, Vector3 pos, float expand = 0) 
        {
            return UnitBoundsContainsPoint(posNSize.position, posNSize.localScale, pos, expand);
        }

        public static bool UnitBoundsContainsPoint(Vector3 boundCenter, Vector3 boundSize, Vector3 pos, float expand = 0)
        {
            var halfSize = boundSize * 0.5f;
            var max = boundCenter + halfSize;
            var min = boundCenter - halfSize;

            if (expand != 0)
            {
                var gap = expand * Vector3.one;
                min -= gap;
                max += gap;
            }

            return
                    pos.x > min.x && pos.y > min.y && pos.z > min.z
                 && pos.x < max.x && pos.y < max.y && pos.z < max.z;
        }

        public static bool TryFindNearest<T>(this List<T> elements, Vector3 targetPosition, out T nearest) where T : Component
        {
            if (elements.IsNullOrEmpty())
            {
                nearest = null;
                return false;
            }

            T nearestElement = elements[0];

            if (elements.Count == 1)
            {
                nearest = nearestElement;
                return true;
            }

            var closesDistance = Vector3.Distance(targetPosition, nearestElement.transform.position);

            for (int i = 1; i < elements.Count; i++)
            {
                var evaluatedElement = elements[i];
                var evaluatedDistance = Vector3.Distance(evaluatedElement.transform.position, targetPosition);
                if (evaluatedDistance < closesDistance)
                {
                    closesDistance = evaluatedDistance;
                    nearestElement = evaluatedElement;
                }
            }

            nearest = nearestElement;
            return true;
        }


        public static T AddOrCopyComponent<T>(this GameObject go, GameObject originalParent) where T : Component 
        {
            if (!originalParent)
                return null;

            return go.AddOrCopyComponent(originalParent.GetComponent<T>());
        }

        public static T AddOrCopyComponent<T>(this GameObject go, T original) where T : Component
        {
            if (!original)
                return null;

            T comp = go.GetComponent<T>();
                
            if (!comp) 
            {
                comp = go.AddComponent<T>();
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = typeof(T).GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(original, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = typeof(T).GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(original));
            }
            return comp;
        }
        
        public static void SetActive_List<T>(this List<T> list, bool to) where T : Component {
            if (!list.IsNullOrEmpty())
                foreach (var e in list)
                    if (e) e.gameObject.SetActive(to);
        }

        public static void SetActive_List(this List<GameObject> list, bool to)
        {
            if (!list.IsNullOrEmpty())
                foreach (var go in list)
                    if (go) go.SetActive(to);
        }

        public static GameObject TryGetGameObjectFromObj(object obj)
        {
            var go = obj as GameObject;

            if (go) return go;

            var cmp = obj as Component;
            if (cmp)
                go = cmp.gameObject;

            return go;
        }

        public static T TryGetInterfaceFrom<T>(object obj) where T : class
        {
            if (IsNullOrDestroyed_Obj(obj))
                return null;

            var pgi = obj as T;

            if (pgi != null)
                return pgi;

            var go = TryGetGameObjectFromObj(obj);

#pragma warning disable UNT0014 // Can be component
            return go ? go.GetComponent<T>() : null;
#pragma warning restore UNT0014 // Invalid type for call to GetComponent
        }

        public static bool IsNullOrDestroyed_Obj(object obj)
        {
            if (obj == null)
                return true;

            if (typeof(Object).IsAssignableFrom(obj.GetType()))
                return !(obj as Object);

             return false;
        }

        public static bool IsUnityObject(this System.Type t) => typeof(Object).IsAssignableFrom(t);

        public static GameObject GetFocusedGameObject()
        {

#if UNITY_EDITOR
            var tmp = Selection.objects;
            return !tmp.IsNullOrEmpty() ? TryGetGameObjectFromObj(tmp[0]) : null;
#else
            return null;
#endif

        }

        public static void DestroyWhateverUnityObject(this Object obj)
        {
            if (!obj) return;

            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }

        public static void DestroyWhatever(this Texture tex)
        {
            if (!tex) 
                return;

            if (tex is RenderTexture rtTex) 
                rtTex.Release();
            
            tex.DestroyWhateverUnityObject();
        }
        public static void DestroyWhatever(this GameObject go) => go.DestroyWhateverUnityObject();

        public static void DestroyAndClear<T>(this List<T> gos) where T : Component
        {
            if (gos.IsNullOrEmpty())
                return;

            foreach (var go in gos)
            {
                if (go)
                    go.gameObject.DestroyWhatever();
            }

            gos.Clear();
        }

        public static void DestroyWhateverComponent(this Component cmp) => cmp.DestroyWhateverUnityObject();

        #endregion

        #region Audio 

        /*
                private static Type audioUtilClass;

#if UNITY_EDITOR
                private static Type AudioUtilClass
                {
                    get
                    {
                        if (audioUtilClass == null)
                            audioUtilClass = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

                        return audioUtilClass;
                    }
                }
#endif*/

        // private static MethodInfo playClipMethod;

        // private static MethodInfo setClipSamplePositionMethod;

        private static AudioSource _editorAudioSource;

        public static float Volume01ToDecebels(float volume) 
        {
            return Mathf.Log10(volume + 0.0001f) * 20;
        }

        public static void Play(this AudioClip clip, float volume = 1) =>
            Play(clip, Vector3.zero, volume);

        public static void Play(this AudioClip clip, Vector3 position, float volume = 1)
        {

            //  var rqst = new EditorAudioPlayRequest(clip);
            /*
            if (!clip) return rqst;

#if UNITY_EDITOR
            if (playClipMethod == null)
            {
                playClipMethod = AudioUtilClass.GetMethod("PlayClip",
                    BindingFlags.Static | BindingFlags.Public,
                    null, new[] { typeof(AudioClip) }, null
                );
            }

            if (playClipMethod != null)
                playClipMethod.Invoke(null, new object[] { clip });
            else
                Debug.LogError("Play Clip Meshod not found");

#else*/

            if (Application.isPlaying)
                AudioSource.PlayClipAtPoint(clip, position, volume);
            else
            {
                if (!_editorAudioSource)
                {
                    _editorAudioSource = new GameObject("INSPECTOR AUDIO (CAN DELETE)").AddComponent<AudioSource>();
                    _editorAudioSource.hideFlags = HideFlags.DontSave;
                }

                _editorAudioSource.transform.position = position;

                _editorAudioSource.PlayOneShot(clip);
            }


            //#endif



            // return rqst;
        }


        /// The clip cut function group below is my addaptation of code originally wrote by DeadlyFingers (GitHub link below)
        /// https://github.com/deadlyfingers/UnityWav

        public static AudioClip Cut(AudioClip clip, float _cutPoint)
        {
            if (!clip)
                return clip;

            return Cut(clip, _cutPoint, clip.length - _cutPoint);
        }

        public static AudioClip Cut(AudioClip sourceClip, float _cutPoint, float duration)
        {

            int targetCutPoint = Mathf.RoundToInt(_cutPoint * sourceClip.frequency) * sourceClip.channels;

            int newSampleCount = sourceClip.samples - targetCutPoint;
            float[] newSamples = new float[newSampleCount];
            sourceClip.GetData(newSamples, targetCutPoint);

            int croppedSampleCount = Mathf.Min(newSampleCount,
                Mathf.RoundToInt(duration * sourceClip.frequency) * sourceClip.channels);
            float[] croppedSamples = new float[croppedSampleCount];

            Array.Copy(newSamples, croppedSamples, croppedSampleCount);

            AudioClip newClip = AudioClip.Create(sourceClip.name, croppedSampleCount, sourceClip.channels,
                sourceClip.frequency, false);

            newClip.SetData(croppedSamples, 0);

            return newClip;
        }

        public static AudioClip Override(AudioClip newClip, AudioClip oldClip)
        {
#if UNITY_EDITOR

            const int headerSize = 44;
            ushort bitDepth = 16;

            MemoryStream stream = new();

            Write(ref stream, System.Text.Encoding.ASCII.GetBytes("RIFF")); //, "ID");


            const int BlockSize_16Bit = 2; // BlockSize (bitDepth)
            int chunkSize = newClip.samples * BlockSize_16Bit + headerSize - 8;
            Write(ref stream, chunkSize); //, "CHUNK_SIZE");

            Write(ref stream, System.Text.Encoding.ASCII.GetBytes("WAVE")); //, "FORMAT");

            byte[] id = System.Text.Encoding.ASCII.GetBytes("fmt ");
            Write(ref stream, id); //, "FMT_ID");

            int subchunk1Size = 16; // 24 - 8
            Write(ref stream, subchunk1Size); //, "SUBCHUNK_SIZE");

            ushort audioFormat = 1;
            Write(ref stream, audioFormat); //, "AUDIO_FORMAT");

            var channels = newClip.channels;
            Write(ref stream, Convert.ToUInt16(channels)); //, "CHANNELS");

            var sampleRate = newClip.frequency;
            Write(ref stream, sampleRate); //, "SAMPLE_RATE");

            Write(ref stream, sampleRate * channels * bitDepth / 8); //, "BYTE_RATE");

            ushort blockAlign = Convert.ToUInt16(channels * bitDepth / 8);
            Write(ref stream, blockAlign); //, "BLOCK_ALIGN");

            Write(ref stream, bitDepth); //, "BITS_PER_SAMPLE");

            Write(ref stream, System.Text.Encoding.ASCII.GetBytes("data")); //, "DATA_ID");

            Write(ref stream, Convert.ToInt32(newClip.samples * BlockSize_16Bit)); //, "SAMPLES");

            float[] data = new float[newClip.samples * newClip.channels];
            newClip.GetData(data, 0);

            MemoryStream dataStream = new();
            int x = sizeof(short);
            short maxValue = short.MaxValue;
            int i = 0;
            while (i < data.Length)
            {
                dataStream.Write(BitConverter.GetBytes(Convert.ToInt16(data[i] * maxValue)), 0, x);
                ++i;
            }

            Write(ref stream, dataStream.ToArray()); //, "DATA");

            dataStream.Dispose();


            var path = AssetDatabase.GetAssetPath(oldClip);

            File.WriteAllBytes(path, stream.ToArray());

            stream.Dispose();

            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
#else

            return newClip;
#endif

        }

        //private static int Write(ref MemoryStream stream, short val) => Write(ref stream, BitConverter.GetBytes(val));

        private static void Write(ref MemoryStream stream, int val) => Write(ref stream, BitConverter.GetBytes(val));

        private static void Write(ref MemoryStream stream, ushort val) => Write(ref stream, BitConverter.GetBytes(val));

        private static void Write(ref MemoryStream stream, byte[] bytes)
        {
            int count = bytes.Length;
            stream.Write(bytes, 0, count);
        }

        /* public class EditorAudioPlayRequest
         {

             public AudioClip clip;

             public void FromTimeOffset(float timeOff)
             {

                 if (!clip)
                     return;

#if UNITY_EDITOR
                 if (!Application.isPlaying)
                 {
                     if (setClipSamplePositionMethod == null)
                         setClipSamplePositionMethod = AudioUtilClass.GetMethod("SetClipSamplePosition",
                             BindingFlags.Static | BindingFlags.Public);

                     int pos = (int)(clip.samples * Mathf.Clamp01(timeOff / clip.length));

                     setClipSamplePositionMethod.Invoke(null, new object[] { clip, pos });
                 }
#endif
             }

             public EditorAudioPlayRequest(AudioClip clip)
             {
                 this.clip = clip;
             }
         }*/

        public static float GetLoudestPointInSeconds(this AudioClip clip)
            => clip.GetFirstLoudPointInSeconds(1);

        public static float GetFirstLoudPointInSeconds(this AudioClip clip, float increase = 3f)
        {
            if (!clip)
                return 0;

            int length = clip.samples;
            float[] data = new float[length];
            clip.GetData(data, 0);

            int maxSample = 0;
            float maxVolume = 0;

            for (int i = 0; i < length; i++)
            {

                var volume = Mathf.Abs(data[i]);

                if (volume > maxVolume)
                {

                    maxVolume = volume * increase;
                    maxSample = i;
                }
            }

            return maxSample / ((float)(clip.frequency * clip.channels));
        }

#endregion

        #region Unity Editor MGMT

        public static string RemoveAssetsFromPath(string s)
        {
            const string ASSETS_FOLDER = "Assets";

            int len = ASSETS_FOLDER.Length;

            var start = s.IndexOf(ASSETS_FOLDER, StringComparison.Ordinal);

            if (start < 0)
                return s;

            //if (start < 2) 
            return s[(start + len + 1)..];

            //return s.Substring(start+len + 1);
        }

        public static bool Contains(this LayerMask layermask, int layer) => layermask == (layermask | (1 << layer));

        public static bool GetPlatformDirective(string define)
        {

#if UNITY_EDITOR
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget).Split(';'); 
            //GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');

            foreach (var s in defines)
            {
                if (s.Equals(define))
                    return true;
            }

            return false;
            //return defines.Contains(define);
#else
                return true;
#endif
        }

        public static void SetPlatformDirective(string val, bool to)
        {

#if UNITY_EDITOR

            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var definesUnsplit = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            var defines = definesUnsplit.Split(';');

            //  var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            // var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

      
            if (Contains(val) == to)
                return;

            if (to)
                definesUnsplit += ";" + val;
            else
            {
                definesUnsplit = definesUnsplit.Replace(val, "").Replace(";;", ";");
            }

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, definesUnsplit);
            //  PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);

            return;

            bool Contains(string valToSearch)
            {
                foreach (var s in defines)
                {
                    if (s.Equals(valToSearch))
                        return true;
                }

                return false;
            }


#endif
        }

        public static bool ApplicationIsAboutToEnterPlayMode()
        {
#if UNITY_EDITOR
            return EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying;
#else
        return false;
#endif
        }

        public static void RepaintViews(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            pegi.Handle.SceneSetDirty(obj);
            //SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
        }

        public static List<Object> SetToDirty(this List<Object> objs)
        {
#if UNITY_EDITOR
            if (objs.IsNullOrEmpty()) return objs;

            foreach (var o in objs)
                o.SetToDirty();
#endif
            return objs;

        }

        public static void SetToDirty(this Object obj)
        {
#if UNITY_EDITOR
            if (!obj)
                return;

            EditorUtility.SetDirty(obj);

            if (PrefabUtility.IsPartOfAnyPrefab(obj))
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
#endif
        }

        public static void FocusOn(Object go)
        {
#if UNITY_EDITOR
            //Debug.Log("Refocusing on " + go);
            var tmp = new Object[1];
            tmp[0] = go;
            Selection.objects = tmp;
#endif
        }

        public static void RenamingLayer(int index, string name)
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            var tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogWarning(
                    "Can't set up the layers.  It's possible the format of the layers and tags data has changed in this version of Unity.");
                Debug.LogWarning("Layers is null: " + (layers == null));
                return;
            }

            var layerSp = layers.GetArrayElementAtIndex(index);

            if (layerSp.stringValue.IsNullOrEmpty() || !layerSp.stringValue.SameAs(name))
            {
                Debug.Log("Changing layer name.  " + layerSp.stringValue + " to " + name);
                layerSp.stringValue = name;
            }

            tagManager.ApplyModifiedProperties();
#endif
        }

        #endregion

        #region Assets Management

        public static T Duplicate<T>(T obj, string folder, string extension, string newName = null) where T : Object {

#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);

            if (path.IsNullOrEmpty())
            {
                obj = Object.Instantiate(obj);
                if (!newName.IsNullOrEmpty())
                    obj.name = newName;

                QcFile.Save.Asset(obj, folder, extension, true);
            }
            else
            {
                var newPath =
                    AssetDatabase.GenerateUniqueAssetPath(newName.IsNullOrEmpty()
                        ? path
                        : path.Replace(obj.name, newName));

                AssetDatabase.CopyAsset(path, newPath);
                obj = AssetDatabase.LoadAssetAtPath<T>(newPath);
            }
#else
           obj = Object.Instantiate(obj);
#endif
            return obj;
        }

        public static List<T> FindAssetsByName<T>(string name, string path = null) where T : Object {

            List<T> assets = new();

#if UNITY_EDITOR

            string searchBy = "{0} t:{1}".F(name, typeof(T).ToPegiStringType());

            var guids = path.IsNullOrEmpty() ? AssetDatabase.FindAssets(searchBy) : AssetDatabase.FindAssets(searchBy, new[] { path });

            foreach (var guid in guids) {
                var tmp = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (tmp)
                    assets.Add(tmp);
            }

#endif

            return assets;

        }

        public static List<T> FindAssetsByType<T>() where T : Object
        {
            if (Application.isEditor == false)
            {
                return new List<T>(Resources.FindObjectsOfTypeAll(typeof(T)) as T[]);
            }

            List<T> assets = new();
#if UNITY_EDITOR

            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:prefab"))
                {
                    T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset)
                    {
                        assets.Add(asset);
                    }
                }
            }
            else
            {
                string typeName = "t:{0}".F(typeof(T).Name);

                foreach (var guid in AssetDatabase.FindAssets(typeName))
                {
                    T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset)
                        assets.Add(asset);
                }
            }
#endif
            return assets;
        }

        public static bool FocusOnAsset<T>() where T : Object
        {
#if UNITY_EDITOR

            var ass = AssetDatabase.FindAssets("t:" + typeof(T));
            if (ass.Length > 0) {

                var all = new Object[ass.Length];

                for (int i = 0; i < ass.Length; i++)
                    all[i] = GuidToAsset<T>(ass[i]);

                Selection.objects = all;

                return true;
            }
#endif
            return false;
        }

        public static void RefreshAssetDatabase()
        {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public static Object GetPrefab(Object obj) =>
#if UNITY_EDITOR
            PrefabUtility.GetCorrespondingObjectFromSource(obj);
#else
             null;
#endif

        public static void UpdatePrefab(GameObject gameObject)
        {
#if UNITY_EDITOR
            var pf = IsPartOfAPrefab(gameObject) ? gameObject : PrefabUtility.GetPrefabInstanceHandle(gameObject);

            if (pf)
            {

                if (!pf)
                    Debug.LogError("Handle is null");
                else
                {
                    var path = AssetDatabase
                        .GetAssetPath(pf); //PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pf);

                    if (path.IsNullOrEmpty())
                        Debug.LogError("Path is null, Update prefab manually");
                    else
                        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, InteractionMode.AutomatedAction);
                }
            }
            else
            {
                Debug.LogError(gameObject.name + " Not a prefab");
            }

            gameObject.SetToDirty();
#endif
        }

        public static bool IsPartOfAPrefab(GameObject go)
        {
#if UNITY_EDITOR

            if (!go)
                return false;

            while (go.transform.parent)
                go = go.transform.parent.gameObject;

            if (PrefabUtility.IsPartOfAnyPrefab(go)  
                || (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null))
                return true;


#endif
            return false;
        }

        public static string SetUniqueObjectName(Object obj, string folderName, string extension)
        {

            folderName = Path.Combine("Assets", folderName); //.AddPreSlashIfNotEmpty());
            var name = obj.name;
            var fullPath =
#if UNITY_EDITOR
                AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderName, name) + extension);
#else
            Path.Combine(folderName,  name) + extension;
#endif
            name = fullPath[folderName.Length..];
            name = name[..^extension.Length];
            obj.name = name;

            return fullPath;
        }

        public static bool TryGetFullPath(Object obj, out string path) 
        {
#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(obj);

            path = System.IO.Path.GetFullPath(assetPath);

            return true;
#else 
            path = "";
            return false;
#endif
        }

        public static string GetAssetFolder(Object obj)
        {
#if UNITY_EDITOR

            var parentObject = GetPrefab(obj);
            if (parentObject)
                obj = parentObject;

            var path = AssetDatabase.GetAssetPath(obj);

            if (path.IsNullOrEmpty()) return "";

            var ind = path.LastIndexOf("/", StringComparison.Ordinal);

            if (ind > 0)
                path = path[..ind];

            return path;

#else
            return "";
#endif
        }

        public static bool IsSavedAsAsset(Object obj) =>
#if UNITY_EDITOR
            obj && (!AssetDatabase.GetAssetPath(obj).IsNullOrEmpty());
#else
            obj;
#endif

        public static string GetGuid(Object obj, string current)
        {
            if (!obj)
                return current;

#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            if (!path.IsNullOrEmpty())
                current = AssetDatabase.AssetPathToGUID(path);
#endif
            return current;
        }

        public static T GuidToAsset<T>(string guid) where T : Object
#if UNITY_EDITOR
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return path.IsNullOrEmpty() ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }
#else
               => null;
#endif

        public static string GetGuid(Object obj) => GetGuid(obj, null);

        public static void RenameAsset<T>(T obj, string newName) where T : Object
        {

            if (newName.IsNullOrEmpty() || !obj) return;

#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            if (!path.IsNullOrEmpty())
                AssetDatabase.RenameAsset(path, newName);
#endif

            obj.name = newName;

        }

#endregion

        #region Input MGMT

        public static int NumericKeyDown(this Event e)  {

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Application.isPlaying && (!Input.anyKeyDown)) 
                return -1;
#endif

            if (!Application.isPlaying && (e.type != UnityEngine.EventType.KeyDown)) return -1;

            if (Application.isPlaying) 
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.Alpha0)) return 0;
                if (Input.GetKeyDown(KeyCode.Alpha1)) return 1;
                if (Input.GetKeyDown(KeyCode.Alpha2)) return 2;
                if (Input.GetKeyDown(KeyCode.Alpha3)) return 3;
                if (Input.GetKeyDown(KeyCode.Alpha4)) return 4;
                if (Input.GetKeyDown(KeyCode.Alpha5)) return 5;
                if (Input.GetKeyDown(KeyCode.Alpha6)) return 6;
                if (Input.GetKeyDown(KeyCode.Alpha7)) return 7;
                if (Input.GetKeyDown(KeyCode.Alpha8)) return 8;
                if (Input.GetKeyDown(KeyCode.Alpha9)) return 9;
#endif
            }
            else
            {
                if (Event.current != null && Event.current.isKey && Event.current.type == UnityEngine.EventType.KeyDown) {

                    var code = (int)Event.current.keyCode - ((int)KeyCode.Alpha0);
                    
                    if (code >= 0 && code <= 9)
                        return code;
                }
            }

            return -1;
        }

        public static bool IsDown(this KeyCode k)
        {
            var down = k.EventType(UnityEngine.EventType.KeyDown);

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Application.isPlaying)
                down |= Input.GetKeyDown(k);
#endif

            return down;
        }

        public static bool IsUp(this KeyCode k) {

            var up = k.EventType(UnityEngine.EventType.KeyUp);

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Application.isPlaying)
                up |= Input.GetKeyUp(k);
#endif

            return up;
        }

        public static bool EventType(this KeyCode k, EventType type) {
            
#if UNITY_EDITOR
            return (Event.current != null && Event.current.isKey && Event.current.type == type && Event.current.keyCode == k);
#else
            return false;
#endif
        }

        #endregion

        #region Material MGMT

        public static bool HasTag(this Material mat, string tag, bool searchFallbacks = false, string defaultValue = "") =>
            mat && !mat.GetTag(tag, searchFallbacks, defaultValue).IsNullOrEmpty();

        public static Material MaterialWhatever(this Renderer renderer) =>
            !renderer ? null : (Application.isPlaying ? renderer.material : renderer.sharedMaterial);

        public static List<string> GetColorProperties(this Material m) =>
#if UNITY_EDITOR
            m.GetProperties(MaterialProperty.PropType.Color);
#else
            new List<String>();
#endif

        public static List<string> MyGetTexturePropertiesNames(this Material m) =>
#if UNITY_EDITOR
             m.GetProperties(MaterialProperty.PropType.Texture);
#else
            new List<String>();
#endif
 
        public static List<string> GetFloatProperties(this Material m)
        {
#if UNITY_EDITOR
            var l = m.GetProperties(MaterialProperty.PropType.Float);
            l.AddRange(m.GetProperties(MaterialProperty.PropType.Range));
            return l;
#else
            return new List<string>();
#endif
        }
        
      

#if UNITY_EDITOR
        public static List<string> GetProperties(this Material m, MaterialProperty.PropType type)
        {
            var fNames = new List<string>();


#if UNITY_EDITOR
            if (!m)
                return fNames;

            Object[] mat = new Object[1];
            mat[0] = m;
            MaterialProperty[] props;

            try {
                props = MaterialEditor.GetMaterialProperties(mat);
            }
            catch {
                return fNames = new List<string>();
            }

            if (props == null) return fNames;

            foreach (var p in props)
            {
                if (p.type == type)
                    fNames.Add(p.name);
            }
            
#endif

            return fNames;
        }
#endif

        #endregion

        #region Textures

        #region Texture MGMT



        public static Color[] GetPixels(this Texture2D tex, int width, int height)
        {

            if ((tex.width == width) && (tex.height == height))
                return tex.GetPixels();

            var dst = new Color[width * height];

            var src = tex.GetPixels();

            var dX = tex.width / (float)width;
            var dY = tex.height / (float)height;

            for (var y = 0; y < height; y++)
            {
                var dstIndex = y * width;
                var srcIndex = ((int)(y * dY)) * tex.width;
                for (var x = 0; x < width; x++)
                    dst[dstIndex + x] = src[srcIndex + (int)(x * dX)];

            }


            return dst;
        }

        public static Color32[] GetPixels32(this Texture2D tex, int width, int height)
        {

            if ((tex.width == width) && (tex.height == height))
                return tex.GetPixels32();

            var dst = new Color32[width * height];

            var src = tex.GetPixels32();

            var dX = tex.width / (float)width;
            var dY = tex.height / (float)height;

            for (var y = 0; y < height; y++)
            {
                var dstIndex = y * width;
                var srcIndex = ((int)(y * dY)) * tex.width;
                for (var x = 0; x < width; x++)
                    dst[dstIndex + x] = src[srcIndex + (int)(x * dX)];
            }


            return dst;
        }


        public static Texture2D CopyFrom(this Texture2D tex, RenderTexture rt)
        {
            if (!rt || !tex)
            {
#if UNITY_EDITOR
                Debug.Log("Texture is null");
#endif
                return tex;
            }

            var curRT = RenderTexture.active;

            RenderTexture.active = rt;

            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            RenderTexture.active = curRT;

            return tex;
        }

        public static bool TextureHasAlpha(this Texture2D tex) {

            if (!tex) return false;

            return tex.format switch
            {
                TextureFormat.ARGB32 => true,
                TextureFormat.RGBA32 => true,
                TextureFormat.ARGB4444 => true,
                TextureFormat.BGRA32 => true,
                TextureFormat.PVRTC_RGBA4 => true,
                TextureFormat.RGBAFloat => true,
                TextureFormat.RGBAHalf => true,
                TextureFormat.Alpha8 => true,
                _ => false,
            };
        }

        public static Texture2D TryGeTexture(this UnityEngine.U2D.SpriteAtlas atlas)
        {
            if (!atlas)
                return null;

            var cnt = atlas.spriteCount;

            if (cnt == 0)
                return null;

            Sprite[] sAr = new Sprite[cnt];
            atlas.GetSprites(sAr);

            return sAr[0].texture;
        } 

#endregion

        #region Texture Import Settings

        public static Color[] GetPixelsFromNonReadableTexture(this Texture2D texture) 
        {
            RenderTexture tmp = RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            using (QcSharp.DisposableAction(() => RenderTexture.ReleaseTemporary(tmp)))
            {
                Graphics.Blit(texture, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;

                Texture2D myTexture2D = new(texture.width, texture.height);

                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                RenderTexture.active = previous;
                var pixels = myTexture2D.GetPixels();
                myTexture2D.DestroyWhatever();

                return pixels;
            }
        }

        public static bool IsColorTexture(this Texture tex)
        {
#if UNITY_EDITOR
            if (!tex) return true;

            TextureImporter importer = tex.GetTextureImporter_Editor();

            if (importer != null)
                return importer.sRGBTexture;
#endif
            return true;
        }

        public static Texture2D CopyImportSettingFrom(this Texture2D dest, Texture2D original)
        {
#if UNITY_EDITOR
            var dst = dest.GetTextureImporter_Editor();
            var org = original.GetTextureImporter_Editor();

            if (!dst || !org) return dest;

            var maxSize = Mathf.Max(original.width, org.maxTextureSize);

            var needReimport = (dst.wrapMode != org.wrapMode) ||
                               (dst.sRGBTexture != org.sRGBTexture) ||
                               (dst.textureType != org.textureType) ||
                               (dst.alphaSource != org.alphaSource) ||
                               (dst.maxTextureSize < maxSize) ||
                               (dst.isReadable != org.isReadable) ||
                               (dst.textureCompression != org.textureCompression) ||
                               (dst.alphaIsTransparency != org.alphaIsTransparency);

            if (!needReimport)
            {
                dst.wrapMode = org.wrapMode;
                dst.sRGBTexture = org.sRGBTexture;
                dst.textureType = org.textureType;
                dst.alphaSource = org.alphaSource;
                dst.alphaIsTransparency = org.alphaIsTransparency;
                dst.maxTextureSize = maxSize;
                dst.isReadable = org.isReadable;
                dst.textureCompression = org.textureCompression;
                dst.SaveAndReimport();
            }
#endif

            return dest;
        }
        
#if UNITY_EDITOR

        public static TextureImporter GetTextureImporter_Editor(this Texture tex) =>
            AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;

        public static bool HadNoMipmaps_Editor(this TextureImporter importer)
        {

            var needsReimport = false;

            if (importer.mipmapEnabled == false)
            {
                importer.mipmapEnabled = true;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfMarkedAsNOrmal_Editor(this Texture2D tex)
        {
            if (!tex) return;

            var importer = tex.GetTextureImporter_Editor();

            if ((importer != null) && (importer.WasMarkedAsNormal_Editor()))
                importer.SaveAndReimport();
        }

        public static bool WasMarkedAsNormal_Editor(this TextureImporter importer, bool convertToNormal = false)
        {

            var needsReimport = false;

            if ((importer.textureType == TextureImporterType.NormalMap) != convertToNormal)
            {
                importer.textureType = convertToNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfClamped_Editor(this Texture2D tex)
        {
            if (!tex) return;

            var importer = tex.GetTextureImporter_Editor();

            if ((importer != null) && (importer.WasClamped_Editor()))
                importer.SaveAndReimport();
        }

        public static bool WasClamped_Editor(this TextureImporter importer)
        {

            var needsReimport = false;


            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfNotReadale_Editor(this Texture2D tex)
        {
            if (!tex) return;

            var importer = tex.GetTextureImporter_Editor();

            if (importer != null && importer.WasNotReadable_Editor())
            {
                importer.SaveAndReimport();
            }
        }

        public static bool WasNotReadable_Editor(this TextureImporter importer)
        {

            var needsReimport = false;

            if (importer.isReadable == false)
            {
                importer.isReadable = true;
                needsReimport = true;
            }

            if (importer.textureType == TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Default;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }

            return needsReimport;


        }

        public static void Reimport_SetIsColorTexture_Editor(this Texture2D tex, bool value)
        {
            if (!tex) return;

            var importer = tex.GetTextureImporter_Editor();

            if (importer && (importer.WasWrongIsColor_Editor(value)))
                importer.SaveAndReimport();
        }

        public static bool WasWrongIsColor_Editor(this TextureImporter importer, bool targetIsColor)
        {

            var needsReimport = false;

            if (importer.sRGBTexture != targetIsColor)
            {
                importer.sRGBTexture = targetIsColor;
                needsReimport = true;
            }

            return needsReimport;
        }

        public static void Reimport_IfNotSingleChanel_Editor(this Texture2D tex)
        {
            if (!tex) return;

            var importer = tex.GetTextureImporter_Editor();

            if (importer  && importer.WasNotSingleChanel_Editor())
                importer.SaveAndReimport();

        }

        public static bool WasNotSingleChanel_Editor(this TextureImporter importer)
        {

            var needsReimport = false;


            if (importer.textureType != TextureImporterType.SingleChannel)
            {
                importer.textureType = TextureImporterType.SingleChannel;
                needsReimport = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.FromGrayScale)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                needsReimport = true;
            }

            if (importer.alphaIsTransparency == false)
            {
                importer.alphaIsTransparency = true;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfAlphaIsNotTransparency_Editor(this Texture2D tex)
        {

            if (!tex) return;

            var importer = tex.GetTextureImporter_Editor();

            if ((importer != null) && (importer.WasWrongAlphaIsTransparency_Editor()))
                importer.SaveAndReimport();


        }

        public static bool WasWrongAlphaIsTransparency_Editor(this TextureImporter importer, bool isTransparency = true)
        {

            var needsReimport = false;

            if (importer.alphaIsTransparency != isTransparency)
            {
                importer.alphaIsTransparency = isTransparency;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }

            if (isTransparency && importer.alphaSource != TextureImporterAlphaSource.FromInput)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfWrongMaxSize_Editor(this Texture2D tex, int width)
        {
            if (!tex) return;

            var importer = tex.GetTextureImporter_Editor();

            if ((importer != null) && (importer.WasWrongMaxSize_Editor(width)))
                importer.SaveAndReimport();

        }

        public static bool WasWrongMaxSize_Editor(this TextureImporter importer, int width)
        {

            var needsReimport = false;

            if (importer.maxTextureSize < width)
            {
                importer.maxTextureSize = width;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static bool WasReadable_Editor(this TextureImporter importer)
        {
            var needsReimport = false;

            if (importer.isReadable)
            {
                importer.isReadable = false;
                needsReimport = true;
            }

            return needsReimport;
        }

        public static bool WasWrong_TextureImporterType(this TextureImporter importer, TextureImporterType targetType)
        {
            if (importer.textureType != targetType)
            {
                importer.textureType = targetType;
                return true;
            }

            return false;
        }

#endif

        #endregion

        #region Texture Saving

        private static string GetPathWithout_Assets_Word(Object tex)
        {
#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(tex);
            return string.IsNullOrEmpty(path) ? null : RemoveAssetsFromPath(path);//path.Replace("Assets", "");
#else
                    return null;
#endif
        }

        public static bool TrySaveTexture(ref Texture2D tex)
        {
#if UNITY_EDITOR

            if (!tex)
            {
                Debug.LogError("Texture is NULL. Can't save.");
            }

            var dest = GetPathWithout_Assets_Word(tex);

            if (dest.IsNullOrEmpty())
            {
                Debug.LogError("Destination path for {0} is Empty".F(tex.ToString()));
                return false;
            }

            var bytes = tex.EncodeToPNG();

            File.WriteAllBytes(Path.Combine(Application.dataPath, dest), bytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

            var result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + dest, typeof(Texture2D));

            result.CopyImportSettingFrom(tex);

            tex = result;

            return true;
#else
return false;
#endif
        }


#if UNITY_EDITOR
        public static void SaveChangesToPixels(Texture2D tex)
        {
            var bytes = tex.EncodeToPNG();

            var dest = QcSharp.ReplaceFirst(AssetDatabase.GetAssetPath(tex),"Assets", "");

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh();
        }

        public static bool TrySaveTexture(ref Texture2D tex, string name)
        {
            if (name == tex.name)
            {
                return TrySaveTexture(ref tex);
            }

            var bytes = tex.EncodeToPNG();

            var dest = GetPathWithout_Assets_Word(tex);
            dest = ReplaceLastOccurrence(dest, tex.name, name);

            if (string.IsNullOrEmpty(dest))
            {
                Debug.LogError("{0} doesn't gave an Asset Path".F(tex));
                return false;
            }


            var savePath = Path.Combine(Application.dataPath, dest);
            File.WriteAllBytes(savePath, bytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);


            var result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + dest, typeof(Texture2D));

            result.CopyImportSettingFrom(tex);

            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tex));

            AssetDatabase.Refresh();

            tex = result;

            return true;
        }

        public static void SaveTextureSameFolder(ref Texture2D Result, Object asset, string name) 
        {
            var bytes = Result.EncodeToPNG();
            var dest =QcSharp.ReplaceFirst(text: AssetDatabase.GetAssetPath(asset), search: "Assets", replace: ""); // AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "", 1);// AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "");
            var extension = dest[(dest.LastIndexOf(".", StringComparison.Ordinal) + 1)..];

            dest = dest[..^extension.Length] + "png";

            dest = ReplaceLastOccurrence(dest, asset.name, name);

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh();

            Result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            var importer = Result.GetTextureImporter_Editor();

            var needReimport = importer.WasReadable_Editor();
            needReimport |= importer.WasWrongIsColor_Editor(false);
            needReimport |= importer.WasClamped_Editor();

            if (needReimport)
                importer.SaveAndReimport();
        }
   
        public static Texture2D SaveTextureAsAsset(Texture2D tex, string folderName, ref string textureName, bool saveAsNew)
        {
            var bytes = tex.EncodeToPNG();

            var folderPath = Path.Combine(Application.dataPath, folderName);
            Directory.CreateDirectory(folderPath);

            if (textureName.IsNullOrEmpty())
                textureName = "unnamed";

            var fileName = textureName + ".png";

            var relativePath = Path.Combine("Assets", folderName, fileName);

            if (saveAsNew)
                relativePath = AssetDatabase.GenerateUniqueAssetPath(relativePath);

            var fullPath = Application.dataPath[..^6] + relativePath;

            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

            var result = (Texture2D)AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D));

            textureName = result.name;

            result.CopyImportSettingFrom(tex);

            return result;
        }

        public static Texture2D CreatePngSameDirectory(Texture2D original, string newName) =>
            CreatePngSameDirectory(original, newName, original.width, original.height);

        public static Texture2D CreatePngSameDirectory(Texture2D original, string newName, int width, int height, bool linear = false)
        {
            if (!original) 
                return null;

            var result = new Texture2D(width, height, TextureFormat.RGBA32, true, linear: linear);

            original.Reimport_IfNotReadale_Editor();

            var pixels = original.GetPixels32(width, height);
            pixels[0].a = 128;

            result.SetPixels32(pixels);
            var bytes = result.EncodeToPNG();

            var dest = QcSharp.ReplaceFirst(text: AssetDatabase.GetAssetPath(original), search: "Assets", replace: ""); // AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "", 1);// AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "");

            var extension = dest[(dest.LastIndexOf(".", StringComparison.Ordinal) + 1)..];

            dest = dest[..^extension.Length] + "png";

            dest = ReplaceLastOccurrence(dest, original.name, newName);

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh();

            var tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            var imp = tex.GetTextureImporter_Editor();
            bool needReimport = imp.WasNotReadable_Editor();
            needReimport |= imp.WasClamped_Editor();
            needReimport |= imp.WasWrongIsColor_Editor(original.IsColorTexture());
            if (needReimport)
                imp.SaveAndReimport();

            return tex;

        }

        private static string ReplaceLastOccurrence(string source, string find, string replace)
        {
            var place = source.LastIndexOf(find, StringComparison.Ordinal);

            if (place == -1)
                return source;

            var result = source.Remove(place, find.Length).Insert(place, replace);
            return result;
        }

#endif

#endregion

#endregion

#region Shaders

        public static void SetShaderKeyword(this Material mat, string keyword, bool isTrue)
        {
            if (mat && !keyword.IsNullOrEmpty()) {
                if (isTrue)
                    mat.EnableKeyword(keyword);
                else
                    mat.DisableKeyword(keyword);
            }
        }

        public static void ToggleShaderKeywords(bool value, string ifTrue, string iFalse)
        {
            Shader.DisableKeyword(value ? iFalse : ifTrue);
            Shader.EnableKeyword(value ? ifTrue : iFalse);
        }

        public static void SetShaderKeyword(string keyword, bool isTrue)
        {
            if (keyword.IsNullOrEmpty()) return;

            if (isTrue)
                Shader.EnableKeyword(keyword);
            else
                Shader.DisableKeyword(keyword);
        }

        public static bool GetKeyword(this Material mat, string keyword) =>
            Array.IndexOf(mat.shaderKeywords, keyword) != -1;

#endregion

#region Meshes

        public static float CalculateVolume(this Mesh mesh) 
        {
            float volume = 0;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 p1 = vertices[triangles[i + 0]];
                Vector3 p2 = vertices[triangles[i + 1]];
                Vector3 p3 = vertices[triangles[i + 2]];
                volume += SignedVolumeOfTriangle(p1, p2, p3);
            }

            return Mathf.Abs(volume);
        }

        public static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float v321 = p3.x * p2.y * p1.z;
            float v231 = p2.x * p3.y * p1.z;
            float v312 = p3.x * p1.y * p2.z;
            float v132 = p1.x * p3.y * p2.z;
            float v213 = p2.x * p1.y * p3.z;
            float v123 = p1.x * p2.y * p3.z;

            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }



        public static void SetColor(this MeshFilter mf, Color col) {

            if (!mf) return;

            var m = mf.mesh;

            var cols = new Color[m.vertexCount];

            for (int i = 0; i < m.vertexCount; i++)
                cols[i] = col;

            mf.mesh.colors = cols;
        }

        public static void SetColor_RGB(this MeshFilter mf, Color col) {

            if (!mf) return;
            
            var m = mf.mesh;

            List<Color> colors = new();

            m.GetColors(colors);

            if (colors.Count < m.vertexCount)
                mf.SetColor(col);
            else
            {
                for (int i = 0; i < m.vertexCount; i++) {
                    col.a = colors[i].a;
                    colors[i] = col;
                }

                mf.mesh.colors = colors.ToArray();
            }
            
        }
        
        public static void SetAlpha(this MeshFilter mf, float alpha)
        {
            if (!mf) return;

            var mesh = mf.mesh;

            var m = mesh;

            var cols = mesh.colors;

            if (cols.IsNullOrEmpty())
            {
                cols = new Color[m.vertexCount];

                for (var i = 0; i < m.vertexCount; i++)
                    cols[i] = Color.white;


            } else for (var i = 0; i < m.vertexCount; i++)
                cols[i].a = alpha;

            mf.mesh.colors = cols;
        }
        public static bool TryGetSubMeshIndex_MAlloc(this RaycastHit hit, out int subMeshIndex)
        {
            subMeshIndex = 0;

            var meshCol = hit.collider as MeshCollider;

            if (!meshCol)
                return false;

            var mesh = meshCol.sharedMesh;

            if (!mesh)
                return false;

            if (mesh.isReadable == false)
            {
                QcLog.ChillLogger.LogWarningOnce("Mesh {0} is not readable".F(mesh.name), mesh.name, hit.transform);
                return false;
            }

            if (mesh.subMeshCount <= 1)
                return true;

            var triangleIndex = hit.triangleIndex;

            int triangleCount = 0;
            for (int i = 0; i < mesh.subMeshCount; ++i)
            {
                var triangles = mesh.GetTriangles(i);
                triangleCount += triangles.Length / 3;
                if (triangleIndex < triangleCount)
                {
                    subMeshIndex = i;
                    return true;
                }
            }

            return false;
        }

        /*
        public static bool TryGetSubMeshIndex(this Mesh mesh, int triangleIndex, out int subMeshIndex)
        { 
            subMeshIndex = 0;

            if (!mesh)
                return false;

            if (mesh.isReadable == false)
            {
                QcLog.ChillLogger.LogWarningOnce("Mesh {0} is not readable".F(mesh.name), mesh.name, mesh);
                return false;
            }

            int triangleCount = 0;
            for (int i = 0; i < mesh.subMeshCount; ++i)
            {
                var triangles = mesh.GetTriangles(i);
                triangleCount += triangles.Length / 3;
                if (triangleIndex < triangleCount)
                {
                    subMeshIndex = i;
                    return true;
                }
            }

            return false;
        }*/

        /*
        public static int GetSubMeshNumber_CheckAllTriangles(this Mesh m, int triangleIndex)
        {
            if (!m)
                return 0;

            if (m.subMeshCount == 1)
                return 0;

            if (!m.isReadable) {
                Debug.Log(string.Format("Mesh {0} is not readable. Enable for submesh material editing.",m.name));
                return 0;
            }

            var triangles = new[] {
                m.triangles[triangleIndex * 3],
                m.triangles[triangleIndex * 3 + 1],
                m.triangles[triangleIndex * 3 + 2]
            };

            for (var i = 0; i < m.subMeshCount; i++) {

                if (i == m.subMeshCount - 1)
                    return i;

                var subMeshTris = m.GetTriangles(i);
                for (var j = 0; j < subMeshTris.Length; j += 3)
                    if (subMeshTris[j] == triangles[0] &&
                        subMeshTris[j + 1] == triangles[1] &&
                        subMeshTris[j + 2] == triangles[2])
                        return i;
            }

            return 0;
        }
        */
        public static void AssignMeshAsCollider(this MeshCollider c, Mesh mesh) {
            // One version of Unity had a bug so this is to counter it, may be not needed anymore
            c.sharedMesh = null;
            c.sharedMesh = mesh;
        }

#endregion

#region Layer Masks

        public static bool GetLayerMaskForSceneView(int layerIndex, bool value)
        {
#if UNITY_EDITOR
            var flag = 1 << layerIndex;
            return (Tools.visibleLayers & flag)>0;
#else
            return false;
#endif

        }

        public static void SetLayerMaskForSceneView(int layerIndex, bool value)
        {
#if UNITY_EDITOR
            var flag = 1 << layerIndex;
            var vis = Tools.visibleLayers & flag;
            if (vis > 0 != value)
            {
                if (value)
                    Tools.visibleLayers |= flag;
                else 
                    Tools.visibleLayers &= ~flag;
            }
#endif
        }

        public static void SetMaskRemoveOthers(this Camera cam, int layerIndex)
        {
            cam.cullingMask = 1 << layerIndex;
        }

        public static void SetMask(this Camera cam, int layerIndex, bool value) 
        {
            if (value) 
            {
                cam.cullingMask |= 1 << layerIndex; 
            } else 
            {
                cam.cullingMask &= ~(1 << layerIndex);
            }
        }

        public static bool GetMask(this Camera cam, int layerIndex)
        {
              return  (cam.cullingMask & (1 << layerIndex))!= 0;
        }

        public static void Clear(this RenderTexture renderTexture, Color col)
        {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, col);
            RenderTexture.active = rt;
        }

        #endregion

        public static Vector3 GetOverlapAreaVector(this Bounds volumeBounds, Bounds elementBounds)
        {
            Vector3 elementMin = elementBounds.min;
            Vector3 elementMax = elementBounds.max;

            Vector3 volumeMin = volumeBounds.min;
            Vector3 volumeMax = volumeBounds.max;

            Vector3 overlapMin = Vector3.Max(elementMin, volumeMin);
            Vector3 overlapMax = Vector3.Min(elementMax, volumeMax);

            return Vector3.Max(Vector3.zero, overlapMax - overlapMin);
        }

        public static Vector3 GetOverlapVector(this Bounds volumeBounds, Vector3 center, Vector3 size)
        {
            var halfSize = size * 0.5f;

            Vector3 elementMin = center - halfSize;
            Vector3 elementMax = center + halfSize;

            Vector3 volumeMin = volumeBounds.min;
            Vector3 volumeMax = volumeBounds.max;

            Vector3 overlapMin = Vector3.Max(elementMin, volumeMin);
            Vector3 overlapMax = Vector3.Min(elementMax, volumeMax);

            return Vector3.Max(Vector3.zero, overlapMax - overlapMin);
        }
    }

#pragma warning restore IDE0019 // Use pattern matching
}





