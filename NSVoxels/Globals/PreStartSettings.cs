using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Globals
{
    public class PreStartSettings
    {
        public static int ResolutionIndex { get; set; }
        public static int KernelIndex { get; set; }

        public static int VolumeSize { get; set; }
        public static int MinimumAcceleratorNodeSize { get; set; }

        public static bool UseVSync { get; set; }


        public static bool UseDoubleBufferedVoxelData { get; set; }
    }
}
