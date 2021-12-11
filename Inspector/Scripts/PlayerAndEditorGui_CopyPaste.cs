using QuizCanners.Utils;
//using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public class CopyPaste
        {
            public class Buffer
            {
                public string CopyPasteJson;
                public string CopyPasteJsonSourceName;
            }

            private static readonly Dictionary<System.Type, Buffer> _copyPasteBuffs = new Dictionary<System.Type, Buffer>();

            private static Buffer GetOrCreate(System.Type type) 
            {
                if (_copyPasteBuffs.TryGetValue(type, out Buffer buff) == false)
                {
                    buff = new Buffer();
                    _copyPasteBuffs[type] = buff;
                }

                return buff;
            }

            public static ChangesToken InspectOptionsFor<T>(ref T el)
            {
                var type = typeof(T);

                var changed = ChangeTrackStart();

                if (type.IsSerializable)
                {
                    if (_copyPasteBuffs.TryGetValue(type, out var buffer))
                    {
                        if (!buffer.CopyPasteJson.IsNullOrEmpty() && Icon.Paste.Click("Paste " + buffer.CopyPasteJsonSourceName))
                            JsonUtility.FromJsonOverwrite(buffer.CopyPasteJson, el);
                    }

                    if (Icon.Copy.Click().IgnoreChanges(LatestInteractionEvent.Click))
                    {
                        if (buffer == null)
                        {
                            buffer = GetOrCreate(type);
                        }
                        buffer.CopyPasteJson = JsonUtility.ToJson(el);
                        buffer.CopyPasteJsonSourceName = el.GetNameForInspector();
                    }
                }
                return changed;
            }

            public static bool InspectOptions<T>(CollectionInspectorMeta meta= null) 
            {
                if (meta != null && meta[CollectionInspectParams.showCopyPasteOptions] == false)
                    return false;

                var type = typeof(T);

                if (_copyPasteBuffs.TryGetValue(type, out Buffer buff))
                {
                    Nl();

                    "Copy Paste: {0}".F(buff.CopyPasteJsonSourceName).PegiLabel().Write();
                    if (Icon.Clear.Click())
                        _copyPasteBuffs.Remove(type);

                    Nl();
                }

                return false;
            }
        }
    }
}