using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Globals;
using NSVoxels.Pipeline.Stages.Dynamics;
using NSVoxels.Structs.Dynamics.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Dynamics.Simple
{
    public class SimpleDynamicsBatch : INSDynamicsBatch
    {
        private StructuredBuffer dynamicComponentBuffer;

        private List<DynamicVoxelComponent> components;

        private int numThreads;

        private Effect simpleDynamicsEffect;

        public SimpleDynamicsBatch()
        {
            components = new List<DynamicVoxelComponent>();
            if (PreStartSettings.UseDoubleBufferedVoxelData)
                simpleDynamicsEffect = Statics.Content.Load<Effect>("Dynamics\\Simple\\SimpleDynamicQuery");
            else simpleDynamicsEffect = Statics.Content.Load<Effect>("Dynamics\\NoDoubleBuffer\\SimpleDynamicQueryNoDoubleBuffer");
        }

        public void AddComponent(DynamicVoxelComponent component)
        {
            this.components.Add(component);
        }

        public void CreateDynamicsFor()
        {
            dynamicComponentBuffer = new StructuredBuffer(
                                   Statics.GraphicsDevice,
                                   typeof(DynamicVoxelComponent),
                                   components.Count,
                                   BufferUsage.None,
                                   ShaderAccess.ReadWrite);
            dynamicComponentBuffer.SetData<DynamicVoxelComponent>(components.ToArray());
            


            numThreads = (int)Math.Ceiling(components.Count / 64.0) + 1;


            int maxIterations = (int)Math.Ceiling(
                                        Math.Log(PreStartSettings.VolumeSize / PreStartSettings.MinimumAcceleratorNodeSize) / Math.Log(2));
            simpleDynamicsEffect.Parameters["maxDepth"].SetValue(maxIterations);
            simpleDynamicsEffect.Parameters["volumeInitialSize"].SetValue(PreStartSettings.VolumeSize);

   
        }
 

        public void Update(Texture3D oldData, Texture3D newData, StructuredBuffer accelerator)
        {
            simpleDynamicsEffect.Parameters["dynamicComponents"].SetValue(dynamicComponentBuffer);
            simpleDynamicsEffect.Parameters["accelerationStructureBuffer"].SetValue(accelerator);

            if (PreStartSettings.UseDoubleBufferedVoxelData)
            {
                simpleDynamicsEffect.Parameters["voxelDataBufferOld"].SetValue(oldData);
                simpleDynamicsEffect.Parameters["voxelDataBufferNew"].SetValue(newData);
            }
            else
            {
                simpleDynamicsEffect.Parameters["voxelDataBuffer"].SetValue(oldData);
            }

            simpleDynamicsEffect.Techniques["AcceleratorTechnique"].Passes["GenerateOctree"].ApplyCompute();
            Statics.GraphicsDevice.DispatchCompute(numThreads, 1, 1);

        }
    }
}
