using UnityEngine;

namespace QuizCanners.Inspect.Examples
{
    public class PlayerAndEditorGui_ExampleNested : MonoBehaviour, IPEGI
    {

        [System.NonSerialized] private Light lightSource;

        [SerializeField] private Color SunColor;
        [SerializeField] private Color MoonColor;
        private bool isDay = true;


        [SerializeField] private pegi.EnterExitContext _context = new();

        public void Inspect()
        {
            var changed = pegi.ChangeTrackStart();

            using (_context.StartContext())
            {
                if ("Light".PegiLabel().IsEntered().Nl())
                {
                    "Sun".PegiLabel(toolTip: "Color of sunlight", width: 50).Write();

                    pegi.Edit(ref SunColor);

                    if (!isDay)
                        "Set Day".PegiLabel().Click().OnChanged(() => isDay = true);

                    pegi.Nl();

                    "Moon".PegiLabel(toolTip: "Color of the Moon", width: 50).Edit(ref MoonColor);

                    if (isDay)
                        "Set Night".PegiLabel().Click().OnChanged(() => isDay = false);
                       
                    pegi.Nl();

                    nameof(lightSource).PegiLabel().Edit(ref lightSource).Nl();

                    if (lightSource)
                    {
                        var myLight = lightSource.color;
                        nameof(myLight).PegiLabel().Edit(ref myLight).Nl().OnChanged(() => lightSource.color = myLight);
                    }

                    if (changed && lightSource)
                    {
                        lightSource.color = isDay ? SunColor : MoonColor;
                    }

                    "FOG".PegiLabel(style: pegi.Styles.ListLabel).Nl();

                    var fogColor = RenderSettings.fogColor;
                    nameof(fogColor).PegiLabel().Edit(ref fogColor).Nl().OnChanged(()=> RenderSettings.fogColor = fogColor);
                }
            }
        }
    }
}