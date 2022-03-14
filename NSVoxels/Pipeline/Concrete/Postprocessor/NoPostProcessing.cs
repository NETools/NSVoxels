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
    public class NoPostProcessing : INSOutput
    {
        private SpriteBatch spriteBatch;
        public NoPostProcessing()
        {
            spriteBatch = new SpriteBatch(Statics.GraphicsDevice);
        }

        public void End(Texture2D renderTarget)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}
