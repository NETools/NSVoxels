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

            simpleDynamicsEffect = Statics.Content.Load<Effect>("");
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


        }
 

        public void Update(Texture3D data, StructuredBuffer accelerator)
        {

        }
    }
}
