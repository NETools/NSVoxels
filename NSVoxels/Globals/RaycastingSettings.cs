using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Globals
{
    public static class RaycastingSettings
    {

        public static int Width { get; set; }
        public static int Height { get; set; }
        public static float AspectRatio { get { return (float)Width / (float)Height; } }
        public static float FOV { get; set; }

        public static bool UseAccelerator;


    }
}
