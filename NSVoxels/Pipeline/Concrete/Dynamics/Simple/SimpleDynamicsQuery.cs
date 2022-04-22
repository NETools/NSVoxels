using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Pipeline.Stages;
using NSVoxels.Pipeline.Stages.Dynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Dynamics.Simple
{
    public class SimpleDynamicsQuery : INSModification
    {
        private List<INSDynamicsBatch> dynamicBatches;

        public SimpleDynamicsQuery()
        {
            dynamicBatches = new List<INSDynamicsBatch>();
        }

        public void AddBatch(SimpleDynamicsBatch batch)
        {
            this.dynamicBatches.Add(batch);
        }

        public void Update(GameTime gameTime, Texture3D data, StructuredBuffer accelerator)
        {
            for (int i = 0; i < dynamicBatches.Count; i++)
            {
                var currentBatch = dynamicBatches[i];
                currentBatch.Update(data, accelerator);
            }
        }
    }
}
