using System;
using System.Collections.Generic;
using System.IO;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Inspect.LazyLocalization;

namespace QuizCanners.Inspect
{
    public enum Icon
    {
        NO_ICON = 0,
        Alpha, Active, Add, Animation, Audio,
        Back,
        Close, Condition, Config, Copy, Cut, Create, Clear,
        Delete, Done, Download, Down, DownLast, Debug, DeSelectAll, Dice,
        Edit, Enter, Exit, Empty,
        False, FoldedOut, Folder,
        Next,
        On, Off,
        Lock, Unlock, List, Link, UnLinked,
        Record, Replace, Refresh,
        Search, Script, Save, SaveAsNew, StateMachine, State, Show, SelectAll, Share, Size, ScreenGrab, Subtract, Swap,
        Stop,
        Question,
        Painter,
        Undo, Redo, UndoDisabled, RedoDisabled,
        Play,
        True, Timeline, Tools,
        Load,
        Pause, Ping, Pinned, UnPinned,
        Move,
        Red, Green, Blue,
        InActive, Insert,
        Hint, Home, Hide,
        Paste,
        Up, UpLast, User,
        Warning, Wait,
        
    }

    public static class Icons_MGMT 
    {
        private const string FOLDER_NAME = "Inspector Icons";

        private static readonly Dictionary<int, Texture2D> _managementIcons = new();

        internal static bool TryGetTexture(this Icon icon, out Texture2D tex)
        {
            tex = icon.GetIcon();
            return tex && tex != Texture2D.whiteTexture;
        }

        public static Texture2D GetIcon(this Icon icon)
        {

            var ind = (int) icon;

            if (_managementIcons.TryGetValue(ind, out var ico))
                return ico;

   

            switch (icon) {
                case Icon.Red: return ColorIcon(0) as Texture2D;
                case Icon.Green: return ColorIcon(1) as Texture2D;
                case Icon.Blue: return ColorIcon(2) as Texture2D;
                case Icon.Alpha: return ColorIcon(3) as Texture2D;
                default:
                    var name = Enum.GetName(typeof(Icon), ind);

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
            _painterIcons ??= new List<Texture2D>();

            while (_painterIcons.Count <= ind) 
                _painterIcons.Add(null);

            if (_painterIcons[ind] != null) 
                return (_painterIcons[ind]);

            _painterIcons[ind] = Resources.Load(Path.Combine(FOLDER_NAME, TryGetName())) as Texture2D;

            string TryGetName() 
            {
                return ind switch
                {
                    0 => "Red",
                    1 => "Green",
                    2 => "Blue",
                    3 => "Alpha",
                    _ => "Alpha",
                };
            }

            return (_painterIcons[ind]);
        }

        public static Texture GetIcon(this ColorChanel icon) => ColorIcon((int) icon);

        public static Texture GetIcon(this ColorMask icon) => icon.ToColorChannel().GetIcon();
        
 
    }
    
    internal static class LazyLocalizationForIcons {

        private static readonly TranslationsEnum iconTranslations = new();

        public static LazyTranslation GetTranslations(this Icon msg, int lang = 0) 
        {

            int index = (int)msg;

            if (iconTranslations.Initialized(index))
                return iconTranslations.GetWhenInited(index, lang);

            switch (msg) {

                case Icon.Add:          msg.Translate("Add"); break;
                case Icon.Enter:        msg.Translate("Enter", "Click to enter"); break;
                case Icon.Exit:         msg.Translate("Exit", "Click to exit"); break;
                case Icon.Empty:        msg.Translate("Empty"); break;
                case Icon.SelectAll:    msg.Translate("Select All"); break;
                case Icon.DeSelectAll:  msg.Translate("Deselect All"); break;
                case Icon.Search:       msg.Translate("Serch"); break;
                case Icon.Show:         msg.Translate("Show"); break;
                case Icon.Hide:         msg.Translate("Hide"); break;
                case Icon.Question:     msg.Translate("Question", "What is this?"); break;
                default:                msg.Translate(msg.ToString().SimplifyTypeName()); break;
            }

            return iconTranslations.GetWhenInited(index, lang);
        }

        public static pegi.TextLabel GetText(this Icon msg, string toolTip, int width)
        {
            var lt = msg.GetText(toolTip);
            lt.width = width;
            return lt;
        }

        public static pegi.TextLabel GetText(this Icon msg, string toolTip = null)
        {
            var lt = msg.GetTranslations();
            var lbl = new pegi.TextLabel(lt != null ? lt.ToString() : msg.ToString());
            if (toolTip != null)
                lbl.toolTip = toolTip;
            return lbl;
        }

        public static string GetDescription(this Icon msg)
        {
            var lt = msg.GetTranslations();
            return lt != null ? lt.details : msg.ToString();
        }

        private static Dictionary<int, LazyTranslation> Translate(this Icon smg, string english)
        {
            var org = iconTranslations[(int)smg];
            org[eng] = new LazyTranslation(english);
            return org;
        }

        private static Dictionary<int, LazyTranslation> Translate(this Icon smg, string english, string englishDetails)
        {
            var org = iconTranslations[(int)smg];
            org[eng] = new LazyTranslation(english, englishDetails);
            return org;
        }
        
        public static string F(this Icon msg, pegi.Msg other) =>  "{0} {1}".F(msg.GetText(), other.GetText());

        public static string F(this pegi.Msg msg, Icon other) => "{0} {1}".F(msg.GetText(), other.GetText());
     
    }


}