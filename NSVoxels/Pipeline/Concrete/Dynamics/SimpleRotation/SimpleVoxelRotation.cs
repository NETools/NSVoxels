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

        private List<DynamicVoxelComponent> components = new List<DynamicVoxelComponent>();
        private StructuredBuffer componentsBuffer;

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

            rotationQuery.Parameters["dynamicComponents"].SetValue(componentsBuffer);
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


        public void AddComponent(Vector3 absolutePosition)
        {
            components.Add(new DynamicVoxelComponent()
            {
                Position = absolutePosition
            });
            Position += absolutePosition;
        }


        public void LoadBuffers()
        {
            Position /= components.Count;

            for (int i = 0; i < components.Count; i++)
            {
                var v = components[i].Position;
                v -= Position;
                components[i] = new DynamicVoxelComponent()
                {
                    Position = v
                };
            }



            componentsBuffer = new StructuredBuffer(Statics.GraphicsDevice, typeof(DynamicVoxelComponent), components.Count, BufferUsage.None, ShaderAccess.ReadWrite);

            componentsBuffer.SetData<DynamicVoxelComponent>(components.ToArray());

            numThreads = (int)Math.Ceiling(components.Count / 64.0) + 1;
        }
    }
}
