using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {

        public static class FullWindow
        {
            public const string DISCORD_SERVER = "https://discord.gg/rF7yXq3";
            public const string SUPPORT_EMAIL = "quizcanners@gmail.com";

            internal static string popUpHeader = "";
            internal static string popUpText = "";
            internal static string relatedLink = "";
            internal static string relatedLinkName = "";
            internal static Func<bool> inspectDocumentationDelegate;
            internal static Action areYouSureFunk;


            private static object _popUpTarget;
            private static string _understoodPopUpText = "Got it";
            private static readonly List<string> _gotItTexts = new List<string>
            {
                "I understand",
                "Clear as day",
                "Roger that",
                "Without a shadow of a doubt",
                "Couldn't be more clear",
                "Totally got it",
                "Well said",
                "Perfect explanation",
                "Thanks",
                "Take me back",
                "Reading Done",
                "Thanks",
                "Affirmative",
                "Comprehended",
                "Grasped",
                "I have learned something",
                "Acknowledged",
                "I see",
                "I get it",
                "I take it as read",
                "Point taken",
                "I infer",
                "Clear message",
                "This was useful",
                "A comprehensive explanation",
                "I have my answer",
                "How do I close this?",
                "Now I want to know something else",
                "Can I close this Pop Up now?",
                "I would like to see previous screen please",
                "This is what I wanted to know",
                "Now I can continue"



            };
            private static readonly List<string> _gotItTextsWeird = new List<string>
            {
                "Nice, this is easier then opening a documentation",
                "So convenient, thanks!",
                "Cool, no need to browse Documentation!",
                "Wish getting answers were always this easy",
                "It is nice to have tooltips like this",
                "I wonder how many texts are here",
                "Did someone had nothing to do to write this texts?",
                "This texts are random every time, aren't they?",
                "Why not make this just OK button"
            };
            private static int _textsShown;

            internal static void InitiatePopUp()
            {
                _popUpTarget = PegiEditorOnly.inspectedTarget;

                switch (_textsShown)
                {
                    case 0: _understoodPopUpText = "OK"; break;
                    case 1: _understoodPopUpText = "Got it!"; break;
                    case 666: _understoodPopUpText = "By clicking I confirm to selling my kidney"; break;
                    default: _understoodPopUpText = (_textsShown < 20 ? _gotItTexts : _gotItTextsWeird).GetRandom(); break;
                }

                _textsShown++;
            }

            internal static void ClosePopUp()
            {
                popUpText = null;
                relatedLink = null;
                relatedLinkName = null;
                inspectDocumentationDelegate = null;
                areYouSureFunk = null;
            }

            #region Documentation Click Open 


            public static void AreYouSureOpen(Action action, string header = "", string text = "")
            {
                if (header.IsNullOrEmpty())
                    header = Msg.AreYouSure.GetText();

                if (text.IsNullOrEmpty())
                    text = Msg.ClickYesToConfirm.GetText();

                areYouSureFunk = action;
                popUpText = text;
                popUpHeader = header;
                InitiatePopUp();
            }
            public static ChangesToken DocumentationWarningClickOpen(string text, string toolTip, int buttonSize = 20)
            {
                if (DocumentationClickInternal(toolTip, buttonSize: buttonSize, Icon.Warning))
                {
                    popUpText = text;
                    InitiatePopUp();
                    return ChangesToken.True;
                }
                return ChangesToken.False;
            }
            public static ChangesToken WarningDocumentationClickOpen(Func<string> text, string toolTip = "What is this?",
                int buttonSize = 20) => DocumentationClickOpen(text, toolTip, buttonSize, Icon.Warning);
            public static ChangesToken WarningDocumentationClickOpen(string text, string toolTip = "What is this?",
                int buttonSize = 20) => DocumentationClickOpen(text, toolTip, buttonSize, Icon.Warning);
            public static ChangesToken DocumentationClickOpen(Func<bool> inspectFunction, string toolTip = "", int buttonSize = 20, Icon clickIcon = Icon.Question)
            {
                if (toolTip.IsNullOrEmpty())
                    toolTip = clickIcon.GetDescription();

                if (DocumentationClickInternal(toolTip, buttonSize))
                {
                    inspectDocumentationDelegate = inspectFunction;
                    InitiatePopUp();
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }
            public static ChangesToken DocumentationClickOpen(Func<string> text, string toolTip = "", int buttonSize = 20, Icon clickIcon = Icon.Question)
            {

                bool gotHeadline = false;

                if (toolTip.IsNullOrEmpty())
                    toolTip = Msg.ToolTip.GetDescription();
                else gotHeadline = true;

                if (DocumentationClickInternal(toolTip, buttonSize, clickIcon))
                {
                    popUpText = text();
                    popUpHeader = gotHeadline ? toolTip : "";
                    InitiatePopUp();
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }
            public static ChangesToken DocumentationClickOpen(string text, string toolTip = "", int buttonSize = 20, Icon clickIcon = Icon.Question)
            {

                bool gotHeadline = false;

                if (toolTip.IsNullOrEmpty())
                    toolTip = Msg.ToolTip.GetDescription();
                else gotHeadline = true;

                if (DocumentationClickInternal(toolTip, buttonSize, clickIcon))
                {
                    popUpText = text;
                    popUpHeader = gotHeadline ? toolTip : "";
                    InitiatePopUp();
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }
            public static ChangesToken DocumentationWithLinkClickOpen(string text, string link, string linkName = null, string tip = "", int buttonSize = 20)
            {
                if (tip.IsNullOrEmpty())
                    tip = Icon.Question.GetDescription();

                if (DocumentationClickInternal(tip, buttonSize))
                {
                    popUpText = text;
                    InitiatePopUp();
                    relatedLink = link;
                    relatedLinkName = linkName.IsNullOrEmpty() ? link : linkName;
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }
            private static ChangesToken DocumentationClickInternal(string toolTip = "", int buttonSize = 20, Icon clickIcon = Icon.Question)
            {
                if (toolTip.IsNullOrEmpty())
                    toolTip = Icon.Question.GetDescription();

                using (SetBgColorDisposable(Color.clear))
                {
                    return clickIcon.Click(toolTip, buttonSize);
                }
            }

            #endregion

            #region Elements
            public static bool ShowingPopup()
            {
                if (_popUpTarget == null || _popUpTarget != PegiEditorOnly.inspectedTarget)
                    return false;

                if (areYouSureFunk != null)
                {
                    Icon.Close.Click(Msg.No.GetText(), 35).OnChanged(ClosePopUp);

                    WriteHeaderIfAny();

                    if (Icon.Done.Click(Msg.Yes.GetText(), 35))
                    {
                        try
                        {
                            areYouSureFunk();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                        ClosePopUp();
                    }


                    Nl();

                    popUpText.PegiLabel().WriteBig();

                    return true;

                }

                if (inspectDocumentationDelegate != null)
                {
                    if (Icon.Back.Click(Msg.Exit))
                        ClosePopUp();
                    else
                    {
                        WriteHeaderIfAny().Nl();
                        inspectDocumentationDelegate();
                        ContactOptions();
                    }

                    return true;
                }

                if (!popUpText.IsNullOrEmpty())
                {

                    WriteHeaderIfAny().Nl();

                    popUpText.PegiLabel(
                        toolTip: "Click the blue text below to close this toolTip. This is basically a toolTip for a toolTip. It is the world we are living in now.")
                        .WriteBig();

                    if (!relatedLink.IsNullOrEmpty() && relatedLinkName.PegiLabel().ClickText(14))
                        Application.OpenURL(relatedLink);

                    ConfirmLabel();
                    return true;
                }



                return false;
            }
            private static void ContactOptions()
            {
                Nl();
                "Didn't get the answer you need?".PegiLabel().Write();
                Icon.Discord.Click(() => Application.OpenURL(DISCORD_SERVER));
                Icon.Email.Click(() => QcUnity.SendEmail(
                        email: SUPPORT_EMAIL, 
                        subject: "About this hint",
                        body: "The toolTip:{0}***{0} {1} {0}***{0} haven't answered some of the questions I had on my mind. Specifically: {0}".F(EnvironmentNl, popUpText)));

            }
            private static void ConfirmLabel()
            {
                Nl();

                if (_understoodPopUpText.PegiLabel().ClickText(15).Nl())
                    ClosePopUp();

                ContactOptions();
            }

            private static StateToken WriteHeaderIfAny()
            {
                if (!popUpHeader.IsNullOrEmpty())
                {
                    popUpHeader.PegiLabel(Styles.ListLabel).Write();
                    return StateToken.True;
                }

                return StateToken.False;
            }

            #endregion
        }
    }
}