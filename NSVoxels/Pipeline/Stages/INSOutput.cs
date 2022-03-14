using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Stages
{
    public interface INSOutput
    {
        void End(Texture2D renderTarget);
    }
}
