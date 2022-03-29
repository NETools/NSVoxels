//////////////////////////////////////////// - 1 - ////////////////////////////////////////////////
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NSVoxels.CustomD3D;
using NSVoxels.Globals;
using NSVoxels.Globals.Mappings;
using NSVoxels.GUI.DebugInfo;
using NSVoxels.GUI.Macros;
using NSVoxels.Interactive;
using NSVoxels.Pipeline.Concrete.Accelerator;
using NSVoxels.Pipeline.Concrete.Dynamics.RigidPhysics;
using NSVoxels.Pipeline.Concrete.Dynamics.Simple;
using NSVoxels.Pipeline.Concrete.Generator;
using NSVoxels.Pipeline.Concrete.Postprocessor;
using NSVoxels.Pipeline.Concrete.Raycaster;
using NSVoxels.Pipeline.Stages;
using NSVoxels.Structs.Dynamics.Simple;
using System;

namespace NSVoxels
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

       
        public Game1()
        {
            Content.RootDirectory = "Content";

            RaycastingSettings.Width = GUIIndexMapping.Resolutions[PreStartSettings.ResolutionIndex].Item1;
            RaycastingSettings.Height = GUIIndexMapping.Resolutions[PreStartSettings.ResolutionIndex].Item2;
            

            graphics = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                IsFullScreen = false,
                PreferredBackBufferWidth = RaycastingSettings.Width,
                PreferredBackBufferHeight = RaycastingSettings.Height,
                PreferMultiSampling = false,
                SynchronizeWithVerticalRetrace = PreStartSettings.UseVSync
            };

            IsFixedTimeStep = false;


        }

        protected override void Initialize()
        {
            base.Initialize();
        }


        NSRenderPipeline renderPipeline;
        VolumeInteractionRaycaster volumeInteraction; // debug, extremly experimental

        protected override void LoadContent()
        {
            Statics.GraphicsDevice = GraphicsDevice;
            Statics.Content = Content;

            RaycastingSettings.FOV = 2.0f;
            VisualSettings.UseMedianFilter = true;

            YawPitchCamera.Initialize();
            YawPitchCamera.CameraPosition = new Vector3(194.50781f, 422.70593f, 532.7831f);

            spriteBatch = new SpriteBatch(GraphicsDevice);
            #region Macros
            MacroManager.GetDefault().DefineMacro(Keys.O, (a) => VisualSettings.ShowShadows = !VisualSettings.ShowShadows, true);
            MacroManager.GetDefault().DefineMacro(Keys.R, (a) => VisualSettings.ShowReflections = !VisualSettings.ShowReflections, true);
            MacroManager.GetDefault().DefineMacro(Keys.P, (a) =>
            {
                RaycastingSettings.UseAccelerator = !RaycastingSettings.UseAccelerator;
                VisualSettings.IterationScalingFactor = RaycastingSettings.UseAccelerator ? (1.0f / 300.0f) : (1.0f / 1000.0f);
            }, true);
            MacroManager.GetDefault().DefineMacro(Keys.M, (a) => VisualSettings.UseMedianFilter = !VisualSettings.UseMedianFilter, true);


            MacroManager.GetDefault().DefineMacro(Keys.Up, (a) =>
            {
                VisualSettings.AngleLightPosition0 += 1.0f * (float)a.ElapsedGameTime.TotalSeconds;

            }, false);
            MacroManager.GetDefault().DefineMacro(Keys.Down, (a) =>
            {
                VisualSettings.AngleLightPosition0 -= 1.0f * (float)a.ElapsedGameTime.TotalSeconds;
            }, false);


            MacroManager.GetDefault().DefineMacro(Keys.F8, (a) =>
            {
                RaycastingSettings.FOV -= 0.1f * (float)a.ElapsedGameTime.TotalSeconds;

            }, false);
            MacroManager.GetDefault().DefineMacro(Keys.F9, (a) =>
            {
                RaycastingSettings.FOV += 0.1f * (float)a.ElapsedGameTime.TotalSeconds;
            }, false);


            const int adjustSpeed = 50;

            MacroManager.GetDefault().DefineMacro(Keys.T, (a) =>
            {
                VisualSettings.ShadowIterations -= (float)(adjustSpeed * a.ElapsedGameTime.TotalSeconds);

            }, false);
            MacroManager.GetDefault().DefineMacro(Keys.Y, (a) =>
            {
                VisualSettings.ShadowIterations += (float)(adjustSpeed * a.ElapsedGameTime.TotalSeconds);
            }, false);


            MacroManager.GetDefault().DefineMacro(Keys.G, (a) =>
            {
                VisualSettings.ReflectionIterations -= (float)(adjustSpeed * a.ElapsedGameTime.TotalSeconds);

            }, false);
            MacroManager.GetDefault().DefineMacro(Keys.H, (a) =>
            {
                VisualSettings.ReflectionIterations += (float)(adjustSpeed * a.ElapsedGameTime.TotalSeconds);
            }, false);


            MacroManager.GetDefault().DefineMacro(Keys.B, (a) =>
            {
                VisualSettings.MaxBounces -= (float)(adjustSpeed * a.ElapsedGameTime.TotalSeconds);

            }, false);
            MacroManager.GetDefault().DefineMacro(Keys.N, (a) =>
            {
                VisualSettings.MaxBounces += (float)(adjustSpeed * a.ElapsedGameTime.TotalSeconds);
            }, false);

            MacroManager.GetDefault().DefineMacro(Keys.Z, (a) =>
            {
                InteractionSettings.AddVoxels = !InteractionSettings.AddVoxels;
            }, true);

            MacroManager.GetDefault().DefineMacro(Keys.I, (a) =>
            {
                VisualSettings.CalculateIndirectLightning = !VisualSettings.CalculateIndirectLightning;
            }, true);
            #endregion

            MacroManager.GetDefault().DefineMacro(Keys.F5, (a) =>
            {
                VisualSettings.ShowIterations = !VisualSettings.ShowIterations;
            }, true);

            MacroManager.GetDefault().DefineMacro(Keys.J, (a) =>
            {
                renderPipeline.Update();
            }, false);

            VisualSettings.IterationScalingFactor = RaycastingSettings.UseAccelerator ? (1.0f / 300.0f) : (1.0f / 1000.0f);

            renderPipeline = new NSRenderPipeline();
            renderPipeline.VoxelDataGenerator = new DemoSceneGeneration();
            renderPipeline.AcceleratorStructureGenerator = new OctreeAccelerator();
            renderPipeline.Raytracer = new NSVoxelRaytracer();
            renderPipeline.PostProcessingFilter = new MedianFilter();

            SimpleDynamicsQuery dynQuery = new SimpleDynamicsQuery();
            SimpleDynamicsBatch dynBatch = new SimpleDynamicsBatch();
            SimpleDynamicsBatch dynBatch1 = new SimpleDynamicsBatch();

            const int dw = 50;
            const int dh = 50;
            const int dz = 50;

            Vector3 center = new Vector3(240, 455, 280);
            Vector3 centerA = new Vector3(450, 455, 280);


            VoxelRigidPhysics voxelRigidPhysics = new VoxelRigidPhysics();



            //for (int j = -dh; j < dh; j++)
            //{
            //    for (int k = -dz; k < dz; k++)
            //    {
            //        for (int i = -dw; i < dw; i++)
            //        {
            //            Vector3 cur = new Vector3(i, j, k);
            //            if (cur.LengthSquared() <= 25 * 25)
            //            {
            //                /*
            //                dynBatch.AddComponent(new DynamicVoxelComponent()
            //                {
            //                    Position = center + cur,
            //                    Direction = new Vector3(-0.49f, -1, 0)
            //                });

            //                dynBatch1.AddComponent(new DynamicVoxelComponent()
            //                {
            //                    Position = centerA + cur,
            //                    Direction = new Vector3(0, -0.55f, 0)
            //                });*/

            //                voxelRigidPhysics.AddRigidComponent(center + cur, 0.001f);
            //            }
            //        }
            //    }
            //}

            /*
            for (int j = 0; j < 10; j++)

                for (int i = 0; i < 50; i++)
                {
                    voxelRigidPhysics.AddRigidComponent(new Vector3(255 + i, 450 + i * 0.1f, 255 + j), 1f);
                }


            */

            int r = 20;
            for (int i = 0; i < 100; i++)
            {


                for (int j = 0; j < 360; j++)
                {
                    int px = (int)(r * Math.Cos(j * Math.PI / 180.0));
                    int py = (int)(r * Math.Sin(j * Math.PI / 180.0));


                    voxelRigidPhysics.AddRigidComponent(new Vector3(i + 255,450 + px, 255 + py ), 0.01f);
                }



            }

            /*
            dynBatch.CreateBuffers();
            dynBatch1.CreateBuffers();

            dynQuery.AddBatch(dynBatch);
            dynQuery.AddBatch(dynBatch1);
            */

            voxelRigidPhysics.LoadBuffers();


            renderPipeline.Modification = voxelRigidPhysics;


            volumeInteraction = new VolumeInteractionRaycaster();
            volumeInteraction.Load();



            renderPipeline.Start();

            base.LoadContent();
        }


        protected override void Update(GameTime gameTime)
        {
            if (!IsActive)
                return;


            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            if (Keyboard.GetState().IsKeyDown(Keys.W))
                YawPitchCamera.Move(new Vector3(0, 0, 1) * (float)gameTime.ElapsedGameTime.TotalSeconds);


            if (Keyboard.GetState().IsKeyDown(Keys.A))
                YawPitchCamera.Move(new Vector3(-1, 0, 0) * (float)gameTime.ElapsedGameTime.TotalSeconds);


            if (Keyboard.GetState().IsKeyDown(Keys.S))
                YawPitchCamera.Move(new Vector3(0, 0, -1) * (float)gameTime.ElapsedGameTime.TotalSeconds);


            if (Keyboard.GetState().IsKeyDown(Keys.D))
                YawPitchCamera.Move(new Vector3(1, 0, 0) * (float)gameTime.ElapsedGameTime.TotalSeconds);

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
                YawPitchCamera.Move(new Vector3(0, 1, 0) * (float)gameTime.ElapsedGameTime.TotalSeconds);


            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                YawPitchCamera.Move(new Vector3(0, -1, 0) * (float)gameTime.ElapsedGameTime.TotalSeconds);


            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                renderPipeline.Trigger(volumeInteraction);


       

            MacroManager.GetDefault().Update(gameTime);

            YawPitchCamera.Update();


            renderPipeline.Update();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {

            GraphicsDevice.Clear(Color.CornflowerBlue);


            renderPipeline.Draw();

            spriteBatch.Begin();
            DebugPrinter.GetDefault().Print(spriteBatch, gameTime);
            spriteBatch.End();



            base.Draw(gameTime);
        }

    }
}
