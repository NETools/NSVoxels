using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Globals;
using NSVoxels.Structs.Dynamics.Collision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Dynamics.Rigid
{
    public class StableCollisionQuery
    {
        private Effect collisionQuery;
        private StructuredBuffer collisionsBuffer;
        private StableCollisionQueryData[] resultBuffer;

        public int MaxContactPoints { get; private set; }

        public StableCollisionQuery(int maxContactPoints)
        {
            this.MaxContactPoints = maxContactPoints;
            collisionQuery = Statics.Content.Load<Effect>("Dynamics\\Stable\\Collision\\CQuery.fx");
            
        }

        public void LoadBuffers()
        {
            this.collisionsBuffer = new StructuredBuffer(
                Statics.GraphicsDevice,
                typeof(StableCollisionQueryData),
                MaxContactPoints,
                BufferUsage.None,
                ShaderAccess.ReadWrite,
                StructuredBufferType.Counter,
                MaxContactPoints);


            resultBuffer = new StableCollisionQueryData[MaxContactPoints];
            this.collisionsBuffer.SetData<StableCollisionQueryData>(resultBuffer);
        }

        public StableCollisionQueryData[] Apply(RigidVoxelBody rigidBody)
        {
            int numThreads = (int)Math.Ceiling(rigidBody.Count / 64.0) + 1;


            collisionQuery.Parameters["currentPosition"].SetValue(rigidBody.PositionCurrent);
            collisionQuery.Parameters["currentRotation"].SetValue(rigidBody.Rotation);
            collisionQuery.Parameters["rigidBodyBuffer"].SetValue(rigidBody.Buffer);
            collisionQuery.Parameters["collisionBuffer"].SetValue(collisionsBuffer);
    

            collisionQuery.CurrentTechnique.Passes[0].ApplyCompute();
            Statics.GraphicsDevice.DispatchCompute(numThreads, 1, 1);


            collisionsBuffer.GetData<StableCollisionQueryData>(resultBuffer);
            return resultBuffer;
        }

     

    }
}
