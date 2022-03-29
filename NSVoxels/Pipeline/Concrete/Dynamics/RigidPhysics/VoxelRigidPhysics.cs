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

namespace NSVoxels.Pipeline.Concrete.Dynamics.RigidPhysics
{
    public class VoxelRigidPhysics : INSModification
    {
        public static float dt = 0.1f;

        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Rotation;

        public Vector3 Force;
        public Vector3 Torque;


        public float Mass;
        //public float Inertia;


        private List<DynamicVoxelComponent> rigidComponents = new List<DynamicVoxelComponent>();
        private StructuredBuffer rigidComponentsBuffer;

        private Effect collisionQuery;
        private int numThreads;
        
        private StructuredBuffer collisionQueryBuffer;
        private CollisionData[] collisionResult;

        public VoxelRigidPhysics()
        {
            collisionQuery = Statics.Content.Load<Effect>("Dynamics\\Collision\\CollisionQuery");



            int maxIterations = (int)Math.Ceiling(
                                        Math.Log(PreStartSettings.VolumeSize / PreStartSettings.MinimumAcceleratorNodeSize) / Math.Log(2));
            collisionQuery.Parameters["maxDepth"].SetValue(maxIterations);
            collisionQuery.Parameters["volumeInitialSize"].SetValue(PreStartSettings.VolumeSize);


            Force = new Vector3(0, -10, 0);
        }

        public void Update(Texture3D oldData, Texture3D newData, StructuredBuffer accelerator)
        {

            Matrix transformationOld = Matrix.CreateFromAxisAngle(Rotation, Rotation.Length());
            collisionQuery.Parameters["com_Position_old"].SetValue(Position);
            collisionQuery.Parameters["lastVoxelTransformation"].SetValue(transformationOld);

            Velocity += (Force / Mass) * dt;
            Position += Velocity * dt;
            Rotation += (Torque / 1) * dt;

    
            Matrix transformationNew = Matrix.CreateFromAxisAngle(Rotation, Rotation.Length());

    
            collisionQuery.Parameters["voxelTransformation"].SetValue(transformationNew);
            collisionQuery.Parameters["com_Position"].SetValue(Position);


            collisionQuery.Parameters["com_Velocity"].SetValue(Velocity);

            collisionQuery.Parameters["dynamicComponents"].SetValue(rigidComponentsBuffer);
            collisionQuery.Parameters["accelerationStructureBuffer"].SetValue(accelerator);
            collisionQuery.Parameters["voxelDataBuffer"].SetValue(newData);

            collisionQuery.Parameters["collisionData"].SetValue(collisionQueryBuffer);



            collisionQuery.CurrentTechnique.Passes[0].ApplyCompute();
            Statics.GraphicsDevice.DispatchCompute(numThreads, 1, 1);

            // Questionable...
            collisionQueryBuffer.GetData<CollisionData>(collisionResult);

            Velocity -= collisionResult[0].NetRepellingForces;
            //Position -= collisionResult[0].NetCorrectionOffsets;
            Torque -= collisionResult[0].NetTorque;


            // Questionable... 
            collisionResult = new CollisionData[]{ new CollisionData() };
            collisionQueryBuffer.SetData<CollisionData>(collisionResult);
        }


        public void AddRigidComponent(Vector3 absolutePosition, float mass)
        {
            rigidComponents.Add(new DynamicVoxelComponent()
            {
                Position = absolutePosition
            });
            Position += absolutePosition;
            Mass += mass;
        }

        public void LoadBuffers()
        {
            Position /= rigidComponents.Count;

            for (int i = 0; i < rigidComponents.Count; i++)
            {
                var v = rigidComponents[i].Position;
                v -= Position;
                rigidComponents[i] = new DynamicVoxelComponent()
                {
                    Position = v
                };
            }



            rigidComponentsBuffer = new StructuredBuffer(Statics.GraphicsDevice, typeof(DynamicVoxelComponent), rigidComponents.Count, BufferUsage.None, ShaderAccess.ReadWrite);

            rigidComponentsBuffer.SetData<DynamicVoxelComponent>(rigidComponents.ToArray());


            collisionQueryBuffer = new StructuredBuffer(Statics.GraphicsDevice, typeof(CollisionData), 1, BufferUsage.None, ShaderAccess.ReadWrite);
            collisionResult = new CollisionData[]
            {
                new CollisionData()
            };

            collisionQueryBuffer.SetData<CollisionData>(collisionResult);

            numThreads = (int)Math.Ceiling(rigidComponents.Count / 64.0) + 1;
        }
    }
}
