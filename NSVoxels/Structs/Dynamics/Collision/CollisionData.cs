using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Structs.Dynamics.Collision
{
    public struct CollisionData
    {
        public Vector3 NetRepellingForces;
        public Vector3 NetCorrectionOffsets;

        public int BlockId;

        public int Collisions;
    }
}
