using QuizCanners.Utils;
using UnityEngine.Events;

namespace QuizCanners.Inspect
{

#pragma warning disable IDE1006 // Naming Styles
    public static partial class pegi
    {
        public static ChangesToken edit_Listener(UnityEvent sourceEvent, UnityAction action, UnityEngine.Object target, bool showAll = true)
        {
            var changed = ChangeTrackStart();

#if UNITY_EDITOR
            var cnt = sourceEvent.GetPersistentEventCount();

            bool included = false;

            for (int i = 0; i < cnt; i++)
            {
                var n = sourceEvent.GetPersistentMethodName(i);
                var t = sourceEvent.GetPersistentTarget(i);

                bool match = n.Equals(action.Method.Name) && t.Equals(action.Target);

                included |= match;

                if (match || showAll)
                {
                    if (Icon.Delete.Click())
                    {
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(sourceEvent, i); //action);
                        return ChangesToken.True;
                    }

                    if (match)
                    {
                        if (sourceEvent.GetPersistentListenerState(i) == UnityEventCallState.Off)
                        {
                            Icon.Warning.Draw("Listener is off");
                            if ("Activate".PegiLabel().Click())
                                sourceEvent.SetPersistentListenerState(i, UnityEventCallState.RuntimeOnly);
                        } else 
                            Icon.Done.Draw();
                    }

                    "{0} on {1}".F(n.IsNullOrEmpty() ? "NULL" : n, t ? t.name : "NULL").PegiLabel().Nl();
                }
            }

            if (!included && "Add {0} to Button".F(action.Method.Name).PegiLabel().Click())
                UnityEditor.Events.UnityEventTools.AddPersistentListener(sourceEvent, action);

            if (changed && target)
                target.SetToDirty();
#endif

            return changed;
        }
    }
}