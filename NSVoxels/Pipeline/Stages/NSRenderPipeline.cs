using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Stages
{
    public class NSRenderPipeline
    {
        public INStart VoxelDataGenerator { get; set; }
        public INSAccelerator AcceleratorStructureGenerator { get; set; }
        public INSModification Modification { get; set; }
        public INSRaytracer Raytracer { get; set; }
        public INSOutput PostProcessingFilter { get; set; }

        private bool isStarted;

        private Texture3D data;
        private StructuredBuffer accelerator;

        public void Start()
        {
            if (isStarted)
                throw new Exception("Already started!");

            isStarted = true;

            data = VoxelDataGenerator.Begin();

            AcceleratorStructureGenerator.Load();
            accelerator = AcceleratorStructureGenerator.Create(data);

            Raytracer.Load();   
        }

        public void Update()
        {
            Modification.Update(data, accelerator);
        }

        public void Draw()
        {
            if (Raytracer == null) return;

            var result = Raytracer.Calculate(data, accelerator);
            PostProcessingFilter.End(result);

        }
    }
}
