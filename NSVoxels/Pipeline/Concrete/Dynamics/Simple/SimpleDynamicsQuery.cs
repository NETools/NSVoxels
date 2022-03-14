using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Pipeline.Stages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Dynamics.Simple
{
    public class SimpleDynamicsQuery : INSModification
    {
        private List<SimpleDynamicsBatch> dynamicBatches;

        public SimpleDynamicsQuery()
        {
            dynamicBatches = new List<SimpleDynamicsBatch>();
        }

        public void AddBatch(SimpleDynamicsBatch batch)
        {
            this.dynamicBatches.Add(batch);
        }

        public void Update(Texture3D data, StructuredBuffer accelerator)
        {
            for (int i = 0; i < dynamicBatches.Count; i++)
            {
                SimpleDynamicsBatch currentBatch = dynamicBatches[i];
                currentBatch.Update(data, accelerator);
            }
        }
    }
}
