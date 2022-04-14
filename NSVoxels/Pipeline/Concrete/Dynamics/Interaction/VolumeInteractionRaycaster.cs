using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.CustomD3D;
using NSVoxels.Globals;
using NSVoxels.Interactive;
using NSVoxels.Pipeline;
using NSVoxels.Pipeline.Stages;
using NSVoxels.Structs.Dynamics.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Raycaster
{
    public class VolumeInteractionRaycaster : INSModification
    {
        //private Texture2D backbuffer;
        private Effect acceleratedRaycasterEffect;


        private StructuredBuffer brushDataBuffer;

        private int divisor = 64;
        public VolumeInteractionRaycaster()
        {
            acceleratedRaycasterEffect = Statics.Content.Load<Effect>("Dynamics\\Editor\\VolumeEditorComputeShader");
        }

        public void Load()
        {
     


            Vector4[] positionLookUps = new Vector4[]
            {
                new Vector4(0, 0, 0, 1),
                new Vector4(1, 0, 0, 1),
                new Vector4(0, 0, 1, 1),
                new Vector4(1, 0, 1, 1),


                new Vector4(0, 1, 0, 1),
                new Vector4(1, 1, 0, 1),
                new Vector4(0, 1, 1, 1),
                new Vector4(1, 1, 1, 1),
            };

            Texture2D positionLookUp = new Texture2D(Statics.GraphicsDevice, 8, 1, false, SurfaceFormat.Vector4);
            positionLookUp.SetData<Vector4>(positionLookUps);


            /*
            backbuffer = new Texture2D(
                Statics.GraphicsDevice, 
                RaycastingSettings.Width, 
                RaycastingSettings.Height, 
                false, 
                SurfaceFormat.Color, 
                ShaderAccess.ReadWrite);
            */

            acceleratedRaycasterEffect.Parameters["volumeInitialSize"].SetValue(PreStartSettings.VolumeSize);

            acceleratedRaycasterEffect.Parameters["maxDepth"].SetValue((int)Math.Ceiling(Math.Log(PreStartSettings.VolumeSize / PreStartSettings.MinimumAcceleratorNodeSize) / Math.Log(2)));

            CreateBrushData();

           
        }


        private void CreateBrushData()
        {
            const int r = 10;
            const int dw = r;
            const int dh = r;

            List<BrushData> brushData = new List<BrushData>();
            for (int i = -dw; i <= dw; i++)
            {
                for (int j = -dh; j <= dh; j++)
                {
                    Vector3 cur = new Vector3(i, j, 0);
                    if (cur.Length() <= r)
                        brushData.Add(new BrushData() { Position = cur });
                }
            }

          

       
            brushDataBuffer = new StructuredBuffer(Statics.GraphicsDevice, typeof(BrushData), brushData.Count, BufferUsage.None, ShaderAccess.ReadWrite);
            brushDataBuffer.SetData<BrushData>(brushData.ToArray());

            brushData.Clear();


            acceleratedRaycasterEffect.Parameters["brushBuffer"].SetValue(brushDataBuffer);
        }



        private float lastFOV;
        public void Update(GameTime gameTime, Texture3D oldData, Texture3D newData, StructuredBuffer accelerator)
        {

            #region Parameter Pass

            if (lastFOV != RaycastingSettings.FOV)
            {
                float fov = RaycastingSettings.FOV * .5f;
                float focalDistance = 1.0f / MathF.Tan(fov * 0.5f);

                acceleratedRaycasterEffect.Parameters["focalDistance"].SetValue(focalDistance);

                lastFOV = RaycastingSettings.FOV;
            }


            #endregion

            acceleratedRaycasterEffect.Parameters["accelerationStructureBuffer"].SetValue(accelerator);
            acceleratedRaycasterEffect.Parameters["voxelDataBuffer"].SetValue(newData);

            acceleratedRaycasterEffect.Parameters["brushAdd"].SetValue(InteractionSettings.AddVoxels);


            acceleratedRaycasterEffect.Parameters["cameraRotation"].SetValue(YawPitchCamera.YawPitchMatrix);
            acceleratedRaycasterEffect.Parameters["cameraPosition"].SetValue(YawPitchCamera.CameraPosition);


            acceleratedRaycasterEffect.CurrentTechnique.Passes[0].ApplyCompute();

            Statics.GraphicsDevice.DispatchCompute(
                (int)MathF.Ceiling(brushDataBuffer.ElementCount / divisor) +1 , 1, 1);
        }
    }
}
