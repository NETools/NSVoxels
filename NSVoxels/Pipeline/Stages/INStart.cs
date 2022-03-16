using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Stages
{
    public interface INStart
    {
        (Texture3D, Texture3D) Begin();
    }
}
