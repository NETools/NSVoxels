using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NSVoxels.Globals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.Interactive
{
    public class YawPitchCamera
    {

        public static Vector3 CameraPosition = new Vector3(165.79764f, 155.7133f, 222.13596f);
        public static Matrix YawPitchMatrix;

        public static float Yaw;
        public static float Pitch;

        public static float DPI = 0.0025f;
        public static float Velocity = 64;


        private static int oldX, oldY;

        public static void Initialize()
        {
            oldX = Statics.GraphicsDevice.Viewport.Width / 2;
            oldY = Statics.GraphicsDevice.Viewport.Height / 2;
        }

        public static void Move(Vector3 direction)
        {
            Matrix rotation = Matrix.CreateRotationY(Yaw);
            Vector3 transformed = Vector3.Transform(direction, rotation);

            transformed *= Velocity;
            CameraPosition += transformed;
        }


        public static void Update()
        {
            float dX = Mouse.GetState().X - oldX;
            float dY = Mouse.GetState().Y - oldY;

            Pitch += DPI * dY;
            Yaw += DPI * dX;

            Pitch = MathHelper.Clamp(Pitch, -1.5f, 1.5f);

            YawPitchMatrix = Matrix.CreateRotationX(Pitch) * Matrix.CreateRotationY(Yaw);

            Mouse.SetPosition(Statics.GraphicsDevice.Viewport.Width / 2, Statics.GraphicsDevice.Viewport.Height / 2);
        }



    }
}
