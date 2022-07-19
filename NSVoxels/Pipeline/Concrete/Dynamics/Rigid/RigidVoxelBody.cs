using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Globals;
using NSVoxels.Pipeline.Stages;
using NSVoxels.Structs.Dynamics.Collision;
using NSVoxels.Structs.Dynamics.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Dynamics.Rigid
{
    public class RigidVoxelBody : INSModification
    {
        public static float dt = 0.1f;

        public Vector3 PositionCurrent;
        public Vector3 PositionPrevious;
        public Matrix Rotation;

        public Vector3 Force;
        public float Mass;

        public int Count { get; private set; }
        public StructuredBuffer Buffer { get; private set; }

        private List<DynamicVoxelComponent> rigidComponents = new List<DynamicVoxelComponent>();
        private StableCollisionQuery collisionQuery;

 
        private int numThreads;

        

        public RigidVoxelBody()
        {

        }
        public void Update(GameTime gameTime, Texture3D data, StructuredBuffer accelerator)
        {
            // APPLY FORCE
            // APPLY COLLISION (CONSTRAINTS)
            // UPDATE POSITION

            var collisionResult = collisionQuery.Apply(this);

            Vector3 torque = Vector3.Zero;
            Vector3 velocity = PositionCurrent - PositionPrevious;

            for (int i = 0; i < collisionResult.Length; i++)
            {
                StableCollisionQueryData current = collisionResult[i];
                torque += Vector3.Cross(current.Position, velocity);

                PositionCurrent += current.Normal * current.PenetrationDepth;
            }

            float torqueLength = torque.Length();
            torque.Normalize();

            Rotation += Matrix.CreateFromAxisAngle(torque, torqueLength) * dt;


            velocity = PositionCurrent - PositionPrevious;
            PositionPrevious = PositionCurrent;
            PositionCurrent = PositionCurrent + velocity + (Force / Mass) * dt * dt;
            Force = Vector3.Zero;
        }


        public void AddRigidComponent(Vector3 absolutePosition, float mass)
        {
            rigidComponents.Add(new DynamicVoxelComponent()
            {
                Position = absolutePosition
            });

            PositionPrevious += absolutePosition;
            Mass += mass;
        }


        public void LoadBuffers()
        {
            PositionCurrent /= rigidComponents.Count;
            PositionPrevious = PositionCurrent;

            for (int i = 0; i < rigidComponents.Count; i++)
            {
                var v = rigidComponents[i].Position;
                v -= PositionCurrent;
                rigidComponents[i] = new DynamicVoxelComponent()
                {
                    Position = v
                };
            }



            Buffer = new StructuredBuffer(Statics.GraphicsDevice, typeof(DynamicVoxelComponent), rigidComponents.Count, BufferUsage.None, ShaderAccess.ReadWrite);

            Buffer.SetData<DynamicVoxelComponent>(rigidComponents.ToArray());

            numThreads = (int)Math.Ceiling(rigidComponents.Count / 64.0) + 1;
            Count = rigidComponents.Count;


            collisionQuery = new StableCollisionQuery(8);
            collisionQuery.LoadBuffers();
        }
    }
}
