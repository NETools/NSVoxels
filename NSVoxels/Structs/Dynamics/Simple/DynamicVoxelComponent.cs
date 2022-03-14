using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Structs.Dynamics.Simple
{
    public struct DynamicVoxelComponent
    {
        public Vector3 Position;
        public Vector3 Direction;
        public int VisitedOctants;
    } 
}
