//using System;
using UnityEngine;

namespace QuizCanners.Inspect.Examples
{
    public class PlayerAndEditorGui_ExampleNested : MonoBehaviour, IPEGI
    {

        [System.NonSerialized] private Light lightSource;

        [SerializeField] private Color SunColor;
        [SerializeField] private Color MoonColor;
        private bool isDay = true;


        [SerializeField] private pegi.EnterExitContext _context = new pegi.EnterExitContext();

        public void Inspect()
        {
            var changed = pegi.ChangeTrackStart();

            using (_context.StartContext())
            {
                if ("Light".PegiLabel().isEntered().nl())
                {
                    "Sun".PegiLabel(toolTip: "Color of sunlight", width: 50).write();

                    pegi.edit(ref SunColor);

                    if (!isDay)
                        "Set Day".PegiLabel().Click().OnChanged(() => isDay = true);

                    pegi.nl();

                    "Moon".PegiLabel(toolTip: "Color of the Moon", width: 50).edit(ref MoonColor);

                    if (isDay)
                        "Set Night".PegiLabel().Click().OnChanged(() => isDay = false);
                       
                    pegi.nl();

                    nameof(lightSource).PegiLabel().edit(ref lightSource).nl();

                    if (lightSource)
                    {
                        var myLight = lightSource.color;
                        nameof(myLight).PegiLabel().edit(ref myLight).nl().OnChanged(() => lightSource.color = myLight);
                    }

                    if (changed && lightSource)
                    {
                        lightSource.color = isDay ? SunColor : MoonColor;
                    }

                    "FOG".PegiLabel(style: pegi.Styles.ListLabel).nl();

                    var fogColor = RenderSettings.fogColor;
                    nameof(fogColor).PegiLabel().edit(ref fogColor).nl().OnChanged(()=> RenderSettings.fogColor = fogColor);
                }
            }
        }
    }
}