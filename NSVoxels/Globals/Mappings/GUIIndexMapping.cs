using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Globals.Mappings
{
    public class GUIIndexMapping
    {
        public static Dictionary<int, Tuple<int, int>> Resolutions = new Dictionary<int, Tuple<int, int>>()
        {
            {0, new (720, 416) },
            {1, new (1088, 720) },
            {2, new (1920, 1088) }
        };

        public static Dictionary<int, int> VolumeSizes = new Dictionary<int, int>()
        {
            {0, 64 },
            {1, 128 },
            {2, 256 },
            {3, 512 },
        };

        public static Dictionary<int, int> MinimumAcceleratorNodeSize = new Dictionary<int, int>()
        {
            {0, 16 },
            {1, 32 },
            {2, 64 },
            {3, 128 },
        };



    }
}
