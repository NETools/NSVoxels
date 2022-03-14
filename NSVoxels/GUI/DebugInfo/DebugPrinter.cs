using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NSVoxels.Globals;
using NSVoxels.GUI.Macros;
using NSVoxels.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.GUI.DebugInfo
{
    public class DebugPrinter
    {
        private StringBuilder debugBuilder = new StringBuilder();
        private SpriteFont font;

        private bool showDebugInformation;
        private bool showContols;


        private SmartFramerate smartFPS = new SmartFramerate(5);

        private DebugPrinter()
        {
            font = Statics.Content.Load<SpriteFont>("font");


            MacroManager.GetDefault().DefineMacro(Keys.F3, (a) => showDebugInformation = !showDebugInformation, true);
            MacroManager.GetDefault().DefineMacro(Keys.F4, (a) => showContols = !showContols, true);
        }

        private static DebugPrinter debugPrinter;

        public static DebugPrinter GetDefault()
        {
            if (debugPrinter == null)
                debugPrinter = new DebugPrinter();

            return debugPrinter;
        }

        private void clearInfo()
        {
            debugBuilder.Clear();
        }

        private void buildInfo()
        {

            if ((!showContols && PreStartSettings.ResolutionIndex < 2) || PreStartSettings.ResolutionIndex == 2)
            {
                debugBuilder.AppendLine("FPS: " + Math.Round(smartFPS.framerate));
                debugBuilder.AppendLine("VSync: " + PreStartSettings.UseVSync);

                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("Render iterations: " + (int)VisualSettings.MaxRaycastingIterations);
                debugBuilder.AppendLine("");


                debugBuilder.AppendLine("Camera position: " + YawPitchCamera.CameraPosition);
                debugBuilder.AppendLine("FoV: " + RaycastingSettings.FOV);

                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("Use median filter: " + VisualSettings.UseMedianFilter);
                debugBuilder.AppendLine("");

                debugBuilder.AppendLine("Use accelerator: " + RaycastingSettings.UseAccelerator);
                debugBuilder.AppendLine("Volume size: " + PreStartSettings.VolumeSize);
                debugBuilder.AppendLine("Minimum node size: " + PreStartSettings.MinimumAcceleratorNodeSize);

            

                debugBuilder.AppendLine("Show shadows: " + VisualSettings.ShowShadows);
                debugBuilder.AppendLine("Shadow iterations: " + (int)VisualSettings.ShadowIterations);

                debugBuilder.AppendLine("");

                debugBuilder.AppendLine("Show reflections: " + VisualSettings.ShowReflections);
                debugBuilder.AppendLine("Reflection iterations: " + (int)VisualSettings.ReflectionIterations);
                debugBuilder.AppendLine("Max reflection bounces: " + (int)VisualSettings.MaxBounces);

                debugBuilder.AppendLine("");

                debugBuilder.AppendLine("Light angle: " + VisualSettings.AngleLightPosition0);
            }

            if (!showContols)
            {
                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("Show Controls: [F4]");
            }
            else
            {
                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("Controls:");

                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("[+]/[-] render iterations: [K][L]");

                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("[+]/[-] FOV: [F8]/[F9]");


                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("Toggle accelerator: [P]");

                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("Toggle shadows: [O]");
                debugBuilder.AppendLine("[+]/[-] shadow quality: [T][Y]");


                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("Toggle reflection: [R]");
                debugBuilder.AppendLine("[+]/[-] reflection quality: [G][H]");
                debugBuilder.AppendLine("[+]/[-] bouncings: [B][N]");

                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("Toggle median filter: [M]");

                debugBuilder.AppendLine("");
                debugBuilder.AppendLine("[+]/[-] light rotation: [Up][Down]");

            }

        }

        public void Print(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!showDebugInformation) return;

            smartFPS.Update(gameTime.ElapsedGameTime.TotalSeconds);

            buildInfo();
            spriteBatch.DrawString(font, debugBuilder.ToString(), new Vector2(2, 0), Color.Red);
            clearInfo();
        }

    }
}
