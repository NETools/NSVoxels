using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Globals;
using NSVoxels.Pipeline.Stages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Pipeline.Concrete.Generator
{
    public class DemoSceneGeneration : INStart
    {
        public Texture3D Data { get; private set; }
        //public Texture3D DataCopy { get; private set; }

        private Effect demoSceneEffect;
        public DemoSceneGeneration()
        {
            Data = new Texture3D(
                Statics.GraphicsDevice,
                PreStartSettings.VolumeSize,
                PreStartSettings.VolumeSize,
                PreStartSettings.VolumeSize,
                false, 
                SurfaceFormat.Single, 
                ShaderAccess.ReadWrite);

            //DataCopy = new Texture3D(
            //    Statics.GraphicsDevice,
            //    PreStartSettings.VolumeSize,
            //    PreStartSettings.VolumeSize,
            //    PreStartSettings.VolumeSize,
            //    false,
            //    SurfaceFormat.Single,
            //    ShaderAccess.ReadWrite);

            demoSceneEffect = Statics.Content.Load<Effect>("Generator\\DemoSceneGenerator");
        }


        public Texture3D Begin()
        {
            demoSceneEffect.Parameters["voxelDataBuffer"].SetValue(Data);
            //demoSceneEffect.Parameters["voxelDataBufferCopy"].SetValue(DataCopy);
            demoSceneEffect.CurrentTechnique.Passes[0].ApplyCompute();

            int dispatchCount = (int)Math.Ceiling((double)Data.Width / 4);
            Statics.GraphicsDevice.DispatchCompute(dispatchCount, dispatchCount, dispatchCount);


            return Data;
        }
    }
}
