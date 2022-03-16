using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Stages.Dynamics
{
    public interface INSDynamicsBatch
    {
        void Update(Texture3D oldData, Texture3D newData, StructuredBuffer accelerator);
    }
}
