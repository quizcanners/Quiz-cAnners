﻿using System;
using System.Collections.Generic;
using System.IO;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Inspect.LazyLocalization;

namespace QuizCanners.Inspect {

    // ReSharper disable InconsistentNaming
    #pragma warning disable IDE1006 // Naming Styles

    public enum icon
    {
        Alpha, Active, Add, Animation, Audio,
        Back, Book,
        Close, Condition, Config, Copy, Cut, Create, Clear, CPU, GPU,
        Discord, Delete, Done, Docs, Download, Down, DownLast, Debug, DeSelectAll, Dice,
        Edit, Enter, Exit, Email, Empty,
        False, FoldedOut, Folder,
        NewMaterial, NewTexture, Next,
        On,
        Off,
        Lock, Unlock, List, Link, UnLinked,
        Round, Record, Replace, Refresh,
        Search, Script, Square, Save, SaveAsNew, StateMachine, State, Show, SelectAll, Share, Size, ScreenGrab, Subtract,
        Question,
        Painter,
        PreviewShader,
        OriginalShader,
        Undo,
        Redo,
        UndoDisabled,
        RedoDisabled,
        Play,
        True,
        Load,
        Pause, Ping,
        Mesh,
        Move,
        Red,
        Green,
        Blue,
        InActive,
        Insert,
        Hint,
        Home,
        Hide,
        Paste,
        Up, UpLast, User,
        Warning,
        Wait
    }

    #pragma warning restore IDE1006 // Naming Styles

    public static class Icons_MGMT {
        private const string FOLDER_NAME = "Inspector Icons";

        private static readonly Countless<Texture2D> _managementIcons = new Countless<Texture2D>();

        internal static bool TryGetTexture(this icon icon, out Texture2D tex)
        {
            tex = icon.GetIcon();
            return tex && tex != Texture2D.whiteTexture;
        }

        public static Texture2D GetIcon(this icon icon)
        {

            var ind = (int) icon;

            var ico = _managementIcons[ind];

            if (ico)
                return ico;

            switch (icon) {
                case icon.Red: return ColorIcon(0) as Texture2D;
                case icon.Green: return ColorIcon(1) as Texture2D;
                case icon.Blue: return ColorIcon(2) as Texture2D;
                case icon.Alpha: return ColorIcon(3) as Texture2D;
                default:
                    var name = Enum.GetName(typeof(icon), ind);

                    Texture2D tmp = null;
                    
                    if (name != null)
                        tmp = Resources.Load(Path.Combine(FOLDER_NAME, name)) as Texture2D;
                    
                    _managementIcons[ind] = tmp ? tmp : Texture2D.whiteTexture;

                    return tmp;
            }
        }

        private static List<Texture2D> _painterIcons;

        private static Texture ColorIcon(int ind)
        {
            if (_painterIcons == null) _painterIcons = new List<Texture2D>();

            while (_painterIcons.Count <= ind) 
                _painterIcons.Add(null);

            if (_painterIcons[ind] != null) 
                return (_painterIcons[ind]);

            _painterIcons[ind] = Resources.Load(Path.Combine(FOLDER_NAME, TryGetName())) as Texture2D;

            string TryGetName() 
            {
                switch (ind) 
                {
                    case 0: return "Red";
                    case 1: return "Green";
                    case 2:return "Blue";
                    case 3:return "Alpha";
                    default: return "Alpha";
                }
            }

            return (_painterIcons[ind]);
        }

        public static Texture GetIcon(this ColorChanel icon) => ColorIcon((int) icon);

        public static Texture GetIcon(this ColorMask icon) => icon.ToColorChannel().GetIcon();
        
 
    }
    
    internal static class LazyLocalizationForIcons {

        private static readonly TranslationsEnum iconTranslations = new TranslationsEnum();

        public static LazyTranslation GetTranslations(this icon msg, int lang = 0) 
        {

            int index = (int)msg;

            if (iconTranslations.Initialized(index))
                return iconTranslations.GetWhenInited(index, lang);

            switch (msg) {

                case icon.Add:          msg.Translate("Add"); break;
                case icon.Enter:        msg.Translate("Enter", "Click to enter"); break;
                case icon.Exit:         msg.Translate("Exit", "Click to exit"); break;
                case icon.Empty:        msg.Translate("Empty"); break;
                case icon.SelectAll:    msg.Translate("Select All"); break;
                case icon.DeSelectAll:  msg.Translate("Deselect All"); break;
                case icon.Search:       msg.Translate("Serch"); break;
                case icon.Show:         msg.Translate("Show"); break;
                case icon.Hide:         msg.Translate("Hide"); break;
                case icon.Question:     msg.Translate("Question", "What is this?"); break;
                default:                msg.Translate(msg.ToString().SimplifyTypeName()); break;
            }

            return iconTranslations.GetWhenInited(index, lang);
        }

        public static pegi.TextLabel GetText(this icon msg, string toolTip, int width)
        {
            var lt = msg.GetText(toolTip);
            lt.width = width;
            return lt;
        }

        public static pegi.TextLabel GetText(this icon msg, string toolTip = null)
        {
            var lt = msg.GetTranslations();
            var lbl = new pegi.TextLabel(lt != null ? lt.ToString() : msg.ToString());
            if (toolTip != null)
                lbl.toolTip = toolTip;
            return lbl;
        }

        public static string GetDescription(this icon msg)
        {
            var lt = msg.GetTranslations();
            return lt != null ? lt.details : msg.ToString();
        }

        private static Countless<LazyTranslation> Translate(this icon smg, string english)
        {
            var org = iconTranslations[(int)smg];
            org[eng] = new LazyTranslation(english);
            return org;
        }

        private static Countless<LazyTranslation> Translate(this icon smg, string english, string englishDetails)
        {
            var org = iconTranslations[(int)smg];
            org[eng] = new LazyTranslation(english, englishDetails);
            return org;
        }
        
        public static string F(this icon msg, Msg other) =>  "{0} {1}".F(msg.GetText(), other.GetText());

        public static string F(this Msg msg, icon other) => "{0} {1}".F(msg.GetText(), other.GetText());
     
    }


}