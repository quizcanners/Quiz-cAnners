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

        void IPEGI.Inspect()
        {
            var changed = pegi.ChangeTrackStart();

            using (_context.StartContext())
            {
                if ("Light".PL().IsEntered().Nl())
                {
                    "Sun".PL(toolTip: "Color of sunlight", width: 50).Write();

                    pegi.Edit(ref SunColor);

                    if (!isDay)
                        "Set Day".PL().Click().OnChanged(() => isDay = true);

                    pegi.Nl();

                    "Moon".PL(toolTip: "Color of the Moon", width: 50).Edit(ref MoonColor);

                    if (isDay)
                        "Set Night".PL().Click().OnChanged(() => isDay = false);
                       
                    pegi.Nl();

                    nameof(lightSource).PL().Edit(ref lightSource).Nl();

                    if (lightSource)
                    {
                        var myLight = lightSource.color;
                        nameof(myLight).PL().Edit(ref myLight).Nl().OnChanged(() => lightSource.color = myLight);
                    }

                    if (changed && lightSource)
                    {
                        lightSource.color = isDay ? SunColor : MoonColor;
                    }

                    "FOG".PL(style: pegi.Styles.ListLabel).Nl();

                    var fogColor = RenderSettings.fogColor;
                    nameof(fogColor).PL().Edit(ref fogColor).Nl().OnChanged(()=> RenderSettings.fogColor = fogColor);
                }
            }
        }
    }
}