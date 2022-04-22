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
        public List<INSModification> Modifications { get; set; }
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

            Modifications = new List<INSModification>();

            data = VoxelDataGenerator.Begin();

            AcceleratorStructureGenerator.Load();
            accelerator = AcceleratorStructureGenerator.Create(data);

            Raytracer.Load();   
        }

        public void Update(GameTime gameTime)
        {
            for (int i = 0; i < Modifications.Count; i++)
            {
                Modifications[i].Update(gameTime, data, accelerator);
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (Raytracer == null) return;

            var result = Raytracer.Calculate(gameTime, data, accelerator);
            PostProcessingFilter.End(result);

        }

        public void Step(GameTime gameTime, INSModification modification)
        {
            modification.Update(gameTime, data, accelerator);
        }

        public void UploadVoxelStream(int startX, int startY, int startZ, int[] voxels, int dataWidth, int dataHeight, int dataDepth)
        {
            data.SetData<int>(0,
                startX,
                startY,
                startX + dataWidth,
                startY + dataHeight,
                startZ,
                startZ + dataDepth,
                voxels,
                0, dataWidth * dataHeight * dataDepth);


        }
    }
}
