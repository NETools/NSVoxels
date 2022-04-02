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
    public class SimpleVoxelRotation : INSModification
    {
        public static float dt = 0.5f;

        public Vector3 Position;
        public Vector3 Rotation;


        private Vector3 position_old;
        private Vector3 rotation_old;

        private List<DynamicVoxelComponent> rigidComponents = new List<DynamicVoxelComponent>();
        private StructuredBuffer rigidComponentsBuffer;

        private Effect rotationQuery;
        private int numThreads;

        private Vector3 rotationValue;

        public SimpleVoxelRotation()
        {
            rotationQuery = Statics.Content.Load<Effect>("Dynamics\\Rotation\\SimpleRotation");

            int maxIterations = (int)Math.Ceiling(
                                        Math.Log(PreStartSettings.VolumeSize / PreStartSettings.MinimumAcceleratorNodeSize) / Math.Log(2));
            rotationQuery.Parameters["maxDepth"].SetValue(maxIterations);
            rotationQuery.Parameters["volumeInitialSize"].SetValue(PreStartSettings.VolumeSize);

            rotationValue = new Vector3(10, 10, 0);

        }

        public void Update(GameTime gameTime, Texture3D oldData, Texture3D newData, StructuredBuffer accelerator)
        {

            Rotation += rotationValue * (float)gameTime.ElapsedGameTime.TotalSeconds * 0.05f;


            Matrix transformation_new = Matrix.Identity;
            var rotationLength_new = Rotation.Length();
            if (rotationLength_new != 0)
                transformation_new = Matrix.CreateFromAxisAngle(Rotation / rotationLength_new, rotationLength_new);

            rotationQuery.Parameters["voxelTransformation"].SetValue(transformation_new);
            rotationQuery.Parameters["com_Position"].SetValue(Position);

            rotationQuery.Parameters["dynamicComponents"].SetValue(rigidComponentsBuffer);
            rotationQuery.Parameters["accelerationStructureBuffer"].SetValue(accelerator);
            rotationQuery.Parameters["voxelDataBuffer"].SetValue(newData);

            rotationQuery.CurrentTechnique.Passes[0].ApplyCompute();
            Statics.GraphicsDevice.DispatchCompute(numThreads, 1, 1);


            //////////////////////////////// DELETION ////////////////////////////////
            position_old = Position;
            rotation_old = Rotation;

            Matrix transformation_old = Matrix.Identity;
            var rotationLength_old = rotation_old.Length();
            if (rotationLength_old != 0)
                transformation_old = Matrix.CreateFromAxisAngle(rotation_old / rotationLength_old, rotationLength_old);

            rotationQuery.Parameters["com_Position_old"].SetValue(position_old);
            rotationQuery.Parameters["voxelTransformation_old"].SetValue(transformation_old);

        }


        public void AddRigidComponent(Vector3 absolutePosition, float mass)
        {
            rigidComponents.Add(new DynamicVoxelComponent()
            {
                Position = absolutePosition
            });
            Position += absolutePosition;
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

            numThreads = (int)Math.Ceiling(rigidComponents.Count / 64.0) + 1;
        }
    }
}
