//////////////////////////////////////////// - 1 - ////////////////////////////////////////////////
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Globals;
using NSVoxels.Pipeline.Stages;
using NSVoxels.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Accelerator
{
    public class OctreeAccelerator : INSAccelerator
    {

        private StructuredBuffer octreeDataBuffer;
        private int maxIterations;
        private int dispatchCount;
        private Effect acceleratorEffect;

        public OctreeAccelerator()
        {
            acceleratorEffect = Statics.Content.Load<Effect>("Accelerator\\AcceleratorGenerator");
        }

        private void _createInitialOctree()
        {
            OctreeEntry[] rwBuffer = new OctreeEntry[octreeDataBuffer.ElementCount];

            int index = 0;

            OctreeEntry root = new();
            root.childrenStartIndex = 1;
            root.childrenCount = 0;

            rwBuffer[index++] = root;
            while (index < rwBuffer.Length)
            {
                OctreeEntry current = new()
                {
                    childrenStartIndex = index * 8 + 1,
                    childrenCount = 0,
                };
                rwBuffer[index++] = current;
            }

            octreeDataBuffer.SetData<OctreeEntry>(rwBuffer);
            _ = Array.Empty<OctreeEntry>();

        }

        private void _createOctreeBuffer(int size)
        {
            maxIterations = (int)Math.Ceiling(Math.Log(size / PreStartSettings.MinimumAcceleratorNodeSize) / Math.Log(2)); // basic algebra
            int maxSize = (int)(1 - Math.Pow(8, maxIterations + 1)) / (1 - 8); // geometric series, q^0 + q^1 + ... + q^(n-1) = (1-q^n)/(1-q)
            octreeDataBuffer = new StructuredBuffer(Statics.GraphicsDevice, typeof(OctreeEntry), maxSize, BufferUsage.None, ShaderAccess.ReadWrite);

            dispatchCount = (int)Math.Ceiling((double)size / 4);
        }

        public void Load()
        {
            _createOctreeBuffer(PreStartSettings.VolumeSize);
            _createInitialOctree();

            acceleratorEffect.Parameters["accelerationStructureBuffer"].SetValue(octreeDataBuffer);
            acceleratorEffect.Parameters["volumeInitialSize"].SetValue(PreStartSettings.VolumeSize);
            acceleratorEffect.Parameters["maxDepth"].SetValue(maxIterations);
        }


        public StructuredBuffer Create(Texture3D data)
        {
            acceleratorEffect.Parameters["voxelDataBuffer"].SetValue(data);
            acceleratorEffect.Techniques["AcceleratorTechnique"].Passes["GenerateOctree"].ApplyCompute();
            Statics.GraphicsDevice.DispatchCompute(dispatchCount, dispatchCount, dispatchCount);

            return octreeDataBuffer;
        }

    }
}
