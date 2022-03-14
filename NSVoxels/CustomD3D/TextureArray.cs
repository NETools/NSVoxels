using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NSVoxels.Globals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSVoxels.CustomD3D
{
    // - 1 -
    public class TextureArray : Texture2D
    {
        public TextureArray(GraphicsDevice graphicsDevice, int width, int height, int arraySize) :
            base(graphicsDevice, width, height, true, SurfaceFormat.Color, SurfaceType.Texture, false, arraySize)
        {

        }
        public void Add(int index, Texture2D texture)
        {
            for (int i = 0; i < texture.LevelCount; i++)
            {

                float divisor = 1.0f / (1 << i);
                int[] pixelData = new int[(int)(texture.Width * texture.Height * divisor * divisor)];

                texture.GetData<int>(
                    i,
                    0,
                    new Rectangle(0, 0, (int)(texture.Width * divisor), (int)(texture.Height * divisor)),
                    pixelData,
                    0,
                    pixelData.Length);

                this.SetData<int>(
                    i,
                    index,
                    new Rectangle(0, 0, (int)(texture.Width * divisor), (int)(texture.Height * divisor)),
                    pixelData,
                    0,
                    pixelData.Length);
            }
        }

        public static TextureArray LoadFromContentFolder(GraphicsDevice graphicsDevice, int widthPerTex, int heightPerTex, string path)
        {
            var paths = Directory.GetFiles(Environment.CurrentDirectory + @"\Content\" + path);

            TextureArray pTexArray = new TextureArray(graphicsDevice, widthPerTex, heightPerTex, paths.Length);

            int index = 0;

            foreach (var file in paths)
                pTexArray.Add(index++, Statics.Content.Load<Texture2D>(path + @"\" + Path.GetFileNameWithoutExtension(file)));

            return pTexArray;
        }

    }
}
