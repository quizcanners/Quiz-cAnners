﻿using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Migration
{
    public static class CfgDecoderExtensions 
    {
        private static ICfgCustom _loopSafe;

        public static void Decode<T>(this T icfg, Configuration cfg) where T : class, ICfg, ICfgDecode
        {
            icfg.Decode(cfg == null ? new CfgData() : cfg.data);
        }

        public static void Decode<T>(this T obj, CfgData data) where T : class, ICfgDecode
        {
            if (obj is ICfgCustom cstm)
            {
                if (cstm == _loopSafe)
                {
                    new CfgDecoder(data).DecodeTagsFor(obj);
                    Debug.LogError("Decode Full is probably calld from Decode Custom call. Use {0}".F(nameof(DecodeTagsFrom)));
                }
                else
                {
                    _loopSafe = cstm;
                    cstm.DecodeInternal(data);
                    _loopSafe = null;
                }
            }
            else
                new CfgDecoder(data).DecodeTagsFor(obj);
        }

        public static void DecodeTagsFrom<T>(this T obj, CfgData data) where T : class, ICfgCustom
        {
            if (!data.IsEmpty)
            {
                new CfgDecoder(data).DecodeTagsFor(obj);
            }
        }
        
        public static T TryDecodeInto<T>(this ICfg ovj, Type childType)
        {
            var val = (T)Activator.CreateInstance(childType);

            if (ovj == null) return val;

            if (val is not ICfg std) return val;

            std.Decode(new CfgData(ovj.Encode().ToString())); 
            
            return val;
        }
    }
    
    public class CfgDecoder   {

        public static string ListElementTag => CfgEncoder.ListElementTag;

        public delegate void DecodeDelegate(string tag, CfgData data);
        
        private readonly string _data;
        private int _position;
        private bool _expectingGetData;

        public CfgDecoder(string dataStream)
        {
            _data = dataStream ?? "";
            _position = 0;
        }

        public CfgDecoder(CfgData data)
        {
            _data = data.ToString() ?? "";
            _position = 0;
        }

        public void DecodeTagsFor(DecodeDelegate decodeDelegate)
        {
            foreach (var tag in this)
                decodeDelegate(tag, GetData());
        }

        private static bool _ignoreErrors;

        public void DecodeTagsIgnoreErrors<T>(T std) where T : class, ICfg
        {
            _ignoreErrors = true;

            try
            {
                DecodeTagsFor(std);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            _ignoreErrors = false;
        }

        public T DecodeTagsFor<T>(T std) where T : class, ICfgDecode
        {
            if (std == null) 
            {
                Debug.LogWarning("Decode Target is Null");
                return std;
            }

            if (_ignoreErrors)
            {
                    foreach (var tag in this)
                    {
                        var d = GetData();
                        try
                        {
                            std.DecodeTag(tag, d);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(_data + Environment.NewLine + ex);
                        }
                    }
            }
            else
            {
                    foreach (var tag in this)
                        std.DecodeTag(tag, GetData());
            }
            return std;
        }
        
        public void DecodeTagsFor<T>(ref T std) where T : ICfg
        {
            foreach (var tag in this)
                std.DecodeTag(tag, GetData());
        }

        private string ToNextSplitter()
        {
            var start = _position;
            while (_data[_position] != CfgEncoder.Splitter)
                _position++;
            
            _position++;
            
            return _data.Substring(start, _position - start - 1);
        }

        public bool GotData => _position < _data.Length; 

        public string GetNextTag()
        {
            if (_position >= _data.Length)
                return null;

            if (_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Data");
            }
            
            _expectingGetData = true;

            var tag = ToNextSplitter();

            if (tag.Length == 0)
            {
                Debug.LogError("Tag was empty after [{1}] tag. Position: {2} Length: {3}  {0} {4}".F(Environment.NewLine, CurrentTag, _position, _data.Length, _data));

                throw new ArgumentException("Tag length was 0");
            }

            CurrentTag = tag;

            currentTagIndex++;

            return CurrentTag;
        }

        public CfgData GetData()
        {
            if (!_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Tag");
            }

            _expectingGetData = false;

            var text = ToNextSplitter();

            try
            {
                int length = int.Parse(text);

                var result = _data.Substring(_position, length);

                _position += length + 1;

                return  new CfgData(result);

            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't get next splitter section [{0}] of {1}".F(CurrentTag, _data));
                throw ex;
            }

        }
        
        public string CurrentTag { get; private set; }

        public int currentTagIndex;

        public IEnumerator<string> GetEnumerator()
        {
            currentTagIndex = 0;
            while (GotNextTag())
                yield return CurrentTag;
        }

        private bool GotNextTag()
        {
            if (_expectingGetData)
            {
                throw new ArgumentException("Was expecting Get Tag");
            }

            return GetNextTag() != null;
        }
    }

}
