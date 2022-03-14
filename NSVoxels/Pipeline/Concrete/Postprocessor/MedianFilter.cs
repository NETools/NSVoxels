using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Globals;
using NSVoxels.Pipeline;
using NSVoxels.Pipeline.Stages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Postprocessor
{
    public class MedianFilter : INSOutput
    {
        private SpriteBatch spriteBatch;
        private Effect medianFilterEffect;
        public MedianFilter()
        {
            spriteBatch = new SpriteBatch(Statics.GraphicsDevice);

            medianFilterEffect = Statics.Content.Load<Effect>("Postprocessor\\MedianFilter");

            medianFilterEffect.Parameters["screenWidth"].SetValue(RaycastingSettings.Width);
            medianFilterEffect.Parameters["screenHeight"].SetValue(RaycastingSettings.Height);
        }

        public void End(Texture2D renderTarget)
        {
            if(VisualSettings.UseMedianFilter)
                spriteBatch.Begin(effect: medianFilterEffect);
            else spriteBatch.Begin();

            spriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}
