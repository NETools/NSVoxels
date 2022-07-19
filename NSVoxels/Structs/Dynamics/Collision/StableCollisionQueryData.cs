using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Structs.Dynamics.Collision
{
    public struct StableCollisionQueryData
    {
        public Vector3 Position;
        public Vector3 Normal;
        public float PenetrationDepth;
    }
}
