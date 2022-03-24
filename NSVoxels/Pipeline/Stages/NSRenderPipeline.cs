using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Globals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Stages
{
    public unsafe class NSRenderPipeline
    {
        public INStart VoxelDataGenerator { get; set; }
        public INSAccelerator AcceleratorStructureGenerator { get; set; }
        public INSModification Modification { get; set; }
        public INSRaytracer Raytracer { get; set; }
        public INSOutput PostProcessingFilter { get; set; }

        private bool isStarted;

        private Texture3D oldData;
        private Texture3D newData;

        private StructuredBuffer accelerator;

        public void Start()
        {
            if (isStarted)
                throw new Exception("Already started!");

            isStarted = true;

            var fullData = VoxelDataGenerator.Begin();

            oldData = fullData.Item1;
            newData = fullData.Item2;

            AcceleratorStructureGenerator.Load();
            accelerator = AcceleratorStructureGenerator.Create(newData);

            Raytracer.Load();   
        }

        public void Update()
        {

            var oldReference = oldData;
            oldData = newData;
            newData = oldReference;

            Modification.Update(oldData, newData, accelerator);
        }

        public void Draw()
        {
            if (Raytracer == null) return;

            var result = Raytracer.Calculate(newData, accelerator);
            PostProcessingFilter.End(result);

        }

        public void Trigger(INSModification modification)
        {
            modification.Update(oldData, newData, accelerator);
        }
    }
}
