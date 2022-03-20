using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.GUI.DebugInfo
{
    /// <summary>
    /// Quelle: https://stackoverflow.com/a/44689035
    /// </summary>
    public class SmartFramerate
    {
        double currentFrametimes;
        double weight;
        int numerator;

        public double framerate
        {
            get
            {
                return (numerator / currentFrametimes);
            }
        }

        private double samples;
        private double fpsAccumulator;

        private double fpsMax;
        private double fpsMin;

        public double Average { get { return fpsAccumulator / samples; } }
        public double MinFps { get { return fpsMin; } }
        public double MaxFps { get { return fpsMax; } }

        public SmartFramerate(int oldFrameWeight)
        {
            numerator = oldFrameWeight;
            weight = (double)oldFrameWeight / ((double)oldFrameWeight - 1d);

            fpsMax = double.MinValue;
            fpsMin = double.MaxValue;
        }


        public void Reset()
        {
            fpsMax = double.MinValue;
            fpsMin = double.MaxValue;

            samples = 0;
            fpsAccumulator = 0;
        }

        public void Update(double timeSinceLastFrame)
        {
            currentFrametimes = currentFrametimes / weight;
            currentFrametimes += timeSinceLastFrame;

            double currentFps = framerate;

            fpsMin = Math.Min(fpsMin, currentFps);
            fpsMax = Math.Max(fpsMax, currentFps);

            samples++;
            fpsAccumulator += currentFps;
        }
    }
}
