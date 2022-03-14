using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.GUI.Macros;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Globals
{
    public static class Statics
    {
        public static GraphicsDevice GraphicsDevice { get; set; }
        public static ContentManager Content { get; set; }

        public static Random Random = new Random();

    }
}
