using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Stages
{
    public interface INSRaytracer
    {
        void Load();
        Texture2D Calculate(GameTime gameTime, Texture3D data, StructuredBuffer accelerator);
    }
}
