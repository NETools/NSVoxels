using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Globals
{
    public static class VisualSettings
    {
        public static Vector3 LighPostion0 = new Vector3(256, 1_000, 256);
        public static float AngleLightPosition0;

        public static float MaxRaycastingIterations;

        public static bool UseMedianFilter;
        
        public static bool ShowShadows;
        public static float ShadowIterations;

        public static bool ShowReflections;
        public static float ReflectionIterations;
        public static float MaxBounces;

        public static bool ShowIterations;
        public static float IterationScalingFactor;




    }
}
