using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.CustomD3D;
using NSVoxels.Globals;
using NSVoxels.Interactive;
using NSVoxels.Pipeline;
using NSVoxels.Pipeline.Stages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Raycaster
{
    public class NSVoxelRaytracer : INSRaytracer
    {
        private Texture2D backbuffer;
        private Effect acceleratedRaycasterEffect;

        private int divisor = 8;
        public NSVoxelRaytracer()
        {
            if (PreStartSettings.KernelIndex == 0)
                acceleratedRaycasterEffect = Statics.Content.Load<Effect>("Raycaster\\VoxelRaycaster");
            else
            {
                acceleratedRaycasterEffect = Statics.Content.Load<Effect>("Raycaster\\VoxelRaycaster16x8");
                divisor = 16;
            }
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


            backbuffer = new Texture2D(
                Statics.GraphicsDevice, 
                RaycastingSettings.Width, 
                RaycastingSettings.Height, 
                false, 
                SurfaceFormat.Color, 
                ShaderAccess.ReadWrite);

            acceleratedRaycasterEffect.Parameters["screenWidth"].SetValue(1.0f / (float)RaycastingSettings.Width);
            acceleratedRaycasterEffect.Parameters["screenHeight"].SetValue(1.0f / (float)RaycastingSettings.Height);
            acceleratedRaycasterEffect.Parameters["backBuffer"].SetValue(backbuffer);
            acceleratedRaycasterEffect.Parameters["aspectRatio"].SetValue(
                new Vector2(RaycastingSettings.AspectRatio, 1));



            acceleratedRaycasterEffect.Parameters["volumeInitialSize"].SetValue(PreStartSettings.VolumeSize);
            acceleratedRaycasterEffect.Parameters["oneOverVolumeInitialSize"].SetValue(1.0f / (float)PreStartSettings.VolumeSize);
            
            acceleratedRaycasterEffect.Parameters["nodeMinimumSize"].SetValue(PreStartSettings.MinimumAcceleratorNodeSize);
            acceleratedRaycasterEffect.Parameters["octantVectorLookUp"].SetValue(positionLookUp);


            TextureArray textureArray = TextureArray.LoadFromContentFolder(Statics.GraphicsDevice, 512, 512, "Textures\\scene0");
            acceleratedRaycasterEffect.Parameters["VoxelTextures"].SetValue(textureArray);



        }



        private float lastFOV;
        public Texture2D Calculate(Texture3D data, StructuredBuffer accelerator)
        {

            #region Parameter Pass

            if (lastFOV != RaycastingSettings.FOV)
            {
                float fov = RaycastingSettings.FOV * .5f;
                float focalDistance = 1.0f / MathF.Tan(fov * 0.5f);

                acceleratedRaycasterEffect.Parameters["focalDistance"].SetValue(focalDistance);

                lastFOV = RaycastingSettings.FOV;
            }

            acceleratedRaycasterEffect.Parameters["lightPosition0"].SetValue(
                Vector3.Transform(VisualSettings.LighPostion0, Matrix.CreateRotationZ(VisualSettings.AngleLightPosition0)));


            acceleratedRaycasterEffect.Parameters["useAccelerator"].SetValue(RaycastingSettings.UseAccelerator);

            acceleratedRaycasterEffect.Parameters["showShadow"].SetValue(VisualSettings.ShowShadows);
            acceleratedRaycasterEffect.Parameters["showReflection"].SetValue(VisualSettings.ShowReflections);

            acceleratedRaycasterEffect.Parameters["shadowMaxAcceleratorIterations"].SetValue((int)VisualSettings.ShadowIterations);
            
            acceleratedRaycasterEffect.Parameters["reflectionMaxAcceleratorIterations"].SetValue((int)VisualSettings.ReflectionIterations);
            acceleratedRaycasterEffect.Parameters["maxBounces"].SetValue((int)VisualSettings.MaxBounces);

            acceleratedRaycasterEffect.Parameters["maxRaycasterIterations"].SetValue((int)VisualSettings.MaxRaycastingIterations);


            acceleratedRaycasterEffect.Parameters["showIterations"].SetValue(VisualSettings.ShowIterations);
            acceleratedRaycasterEffect.Parameters["iterationScale"].SetValue(VisualSettings.IterationScalingFactor);

            #endregion

            acceleratedRaycasterEffect.Parameters["accelerationStructureBuffer"].SetValue(accelerator);
            acceleratedRaycasterEffect.Parameters["voxelDataBuffer"].SetValue(data);

            acceleratedRaycasterEffect.Parameters["cameraRotation"].SetValue(YawPitchCamera.YawPitchMatrix);
            acceleratedRaycasterEffect.Parameters["cameraPosition"].SetValue(YawPitchCamera.CameraPosition);


            acceleratedRaycasterEffect.CurrentTechnique.Passes[0].ApplyCompute();


            Statics.GraphicsDevice.DispatchCompute(
                (int)MathF.Ceiling(RaycastingSettings.Width / divisor), 
                (int)MathF.Ceiling(RaycastingSettings.Height / 8), 1);

            return backbuffer;
        }
    }
}
