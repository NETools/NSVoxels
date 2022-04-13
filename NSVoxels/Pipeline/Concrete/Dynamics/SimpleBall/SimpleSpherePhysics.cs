using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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

namespace NSVoxels.Pipeline.Concrete.Dynamics.SimpleBall
{
    public class SimpleSpherePhysics : INSModification
    {
        public static float dt = 80;

        public Vector3 Position;
        public Vector3 Velocity;

        private Vector3 position_old;
        private Vector3 velocity_old;

        public Vector3 Force;
        public float Mass;

        private List<DynamicVoxelComponent> rigidComponents = new List<DynamicVoxelComponent>();
        private StructuredBuffer rigidComponentsBuffer;

        private Effect collisionQuery;
        private int numThreads;
        
        private StructuredBuffer collisionQueryBuffer;
        private CollisionData[] collisionResult;

        private Dictionary<int, SoundEffect> soundEffects;

        public SimpleSpherePhysics()
        {
            collisionQuery = Statics.Content.Load<Effect>("Dynamics\\Collision\\CollisionQuery");

            soundEffects = new Dictionary<int, SoundEffect>();


            soundEffects.Add(1, Statics.Content.Load<SoundEffect>("Sounds\\ballhit"));
            soundEffects.Add(6, Statics.Content.Load<SoundEffect>("Sounds\\ballhitGrass"));
            soundEffects.Add(3, Statics.Content.Load<SoundEffect>("Sounds\\ballhitDirt"));
            soundEffects.Add(4, Statics.Content.Load<SoundEffect>("Sounds\\ballhitWall"));

            int maxIterations = (int)Math.Ceiling(
                                        Math.Log(PreStartSettings.VolumeSize / PreStartSettings.MinimumAcceleratorNodeSize) / Math.Log(2));
            collisionQuery.Parameters["maxDepth"].SetValue(maxIterations);
            collisionQuery.Parameters["volumeInitialSize"].SetValue(PreStartSettings.VolumeSize);

           
        }
        public void Update(GameTime gameTime, Texture3D oldData, Texture3D newData, StructuredBuffer accelerator)
        {
            Force += new Vector3(0, -9.81f, 0);
            Force += -Velocity * 0.25f;


            Velocity += (Force / Mass) * dt * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * dt * (float)gameTime.ElapsedGameTime.TotalSeconds;

            collisionQuery.Parameters["com_Position"].SetValue(Position);
            collisionQuery.Parameters["com_Velocity"].SetValue(Velocity);

            collisionQuery.Parameters["dynamicComponents"].SetValue(rigidComponentsBuffer);
            collisionQuery.Parameters["accelerationStructureBuffer"].SetValue(accelerator);
            collisionQuery.Parameters["voxelDataBuffer"].SetValue(newData);

            collisionQuery.Parameters["collisionData"].SetValue(collisionQueryBuffer);
            collisionQuery.CurrentTechnique.Passes[0].ApplyCompute();
            Statics.GraphicsDevice.DispatchCompute(numThreads, 1, 1);


            //////////////////////////////// DELETION ////////////////////////////////
            velocity_old = Velocity;
            position_old = Position;

            collisionQuery.Parameters["com_Position_old"].SetValue(position_old);
            collisionQuery.Parameters["com_Velocity_old"].SetValue(velocity_old);

            Force = Vector3.Zero;

            //////////////////////////////////////////////////////////////////////////

            
            if (Velocity.Length() < 0.1)
                return;

            //////////////////////////////// RETRIEVAL ///////////////////////////////
            ////////////////////////////// (INEFFICIENT) /////////////////////////////
            collisionQueryBuffer.GetData<CollisionData>(collisionResult);


            if (collisionResult[0].Collisions > 0)
            {
                if (Math.Abs(collisionResult[0].NetRepellingForces.X) > 0.7 ||
                    Math.Abs(collisionResult[0].NetRepellingForces.Y) > 0.5 ||
                    Math.Abs(collisionResult[0].NetRepellingForces.Z) > 0.7)
                {


                    if (soundEffects.ContainsKey(collisionResult[0].BlockId))
                        soundEffects[collisionResult[0].BlockId].Play();
               
                }

            }
 
            Velocity -= collisionResult[0].NetRepellingForces;
            Position -= collisionResult[0].NetCorrectionOffsets;


            if (Position.X < 0 || Position.Y < 0 || Position.Z < 0 || Position.X >= 512 || Position.Y >= 512 || Position.Z >= 512
                || float.IsNaN(Position.X) || float.IsNaN(Position.Y) || float.IsNaN(Position.Z))
            {
                Position = new Vector3(255, 480, 255);
                Velocity = Vector3.Zero;
            }

            //////////////////////////////////////////////////////////////////////////

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


        public void CreateSphere(Vector3 center, int r, float massPerVoxel)
        {
            for (int j = -r; j < r; j++)
            {
                for (int k = -r; k < r; k++)
                {
                    for (int i = -r; i < r; i++)
                    {
                        Vector3 cur = new Vector3(i, j, k);
                        if (cur.LengthSquared() <= r * r)
                        {
                            this.AddRigidComponent(center + cur, massPerVoxel);
                        }
                    }
                }
            }
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
