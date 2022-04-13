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
using NSVoxels.Pipeline.Concrete.Dynamics.SimpleBall;
using NSVoxels.Pipeline.Concrete.Dynamics.Simple;
using NSVoxels.Pipeline.Concrete.Generator;
using NSVoxels.Pipeline.Concrete.Postprocessor;
using NSVoxels.Pipeline.Concrete.Raycaster;
using NSVoxels.Pipeline.Stages;
using NSVoxels.Structs.Dynamics.Simple;
using System;
using NSVoxels.Pipeline.Concrete.RAWLoader;

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
            RaycastingSettings.UseAccelerator = true;

            VisualSettings.UseMedianFilter = true;

            YawPitchCamera.Initialize();
            YawPitchCamera.CameraPosition = new Vector3(550, 530, 550);

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


            SimpleDynamicsQuery dynQuery = new SimpleDynamicsQuery();


            MacroManager.GetDefault().DefineMacro(Keys.J, (a) =>
            {
                renderPipeline.Step(a, dynQuery);
            }, false);


            SimpleSpherePhysics simpleShere = new SimpleSpherePhysics();
            simpleShere.CreateSphere(new Vector3(230, 480, 300), 25, 0.01f);
            simpleShere.LoadBuffers();

            bool loadedSphere = false;
            MacroManager.GetDefault().DefineMacro(Keys.L, (a) =>
            {
                if (loadedSphere) return;
                loadedSphere = true;
                renderPipeline.Modifications.Add(simpleShere);

            }, true);


            VisualSettings.IterationScalingFactor = RaycastingSettings.UseAccelerator ? (1.0f / 300.0f) : (1.0f / 1000.0f);

            renderPipeline = new NSRenderPipeline();
            renderPipeline.VoxelDataGenerator = new DemoSceneGeneration();
            renderPipeline.AcceleratorStructureGenerator = new OctreeAccelerator();
            renderPipeline.Raytracer = new NSVoxelRaytracer();
            renderPipeline.PostProcessingFilter = new MedianFilter();

            SimpleDynamicsBatch dynBatch = new SimpleDynamicsBatch();
            SimpleDynamicsBatch dynBatch1 = new SimpleDynamicsBatch();

            int r = 25;

            Vector3 centerA = new Vector3(490, 460, 280);
            Vector3 centerB = new Vector3(240, 460, 280);

            for (int j = -r; j < r; j++)
            {
                for (int k = -r; k < r; k++)
                {
                    for (int i = -r; i < r; i++)
                    {
                        Vector3 cur = new Vector3(i, j, k);
                        if (cur.LengthSquared() <= r * r)
                        {
                            dynBatch.AddComponent(new DynamicVoxelComponent()
                            {
                                Position = centerA + cur,
                                Direction = new Vector3(-0.49f, -1, 0)
                            });

                            dynBatch1.AddComponent(new DynamicVoxelComponent()
                            {
                                Position = centerB + cur,
                                Direction = new Vector3(0, -0.55f, 0)
                            });

                        }
                    }
                }
            }


            SimpleVoxelRotation simpleVoxelRotation = new SimpleVoxelRotation();

            r = 5;
            for (int i = 0; i < 200; i++)
            {
                for (int j = 0; j < 360; j++)
                {
                    int px = (int)(r * Math.Cos(j * Math.PI / 180.0));
                    int py = (int)(r * Math.Sin(j * Math.PI / 180.0));


                    simpleVoxelRotation.AddComponent(new Vector3(i + 150, 450 + px, 300 + py));
                }
            }


            dynBatch.CreateBuffers();
            dynBatch1.CreateBuffers();

            dynQuery.AddBatch(dynBatch);
            dynQuery.AddBatch(dynBatch1);
            

            simpleVoxelRotation.LoadBuffers();

            

            //renderPipeline.Modification = voxelRigidPhysics;


            volumeInteraction = new VolumeInteractionRaycaster();
            volumeInteraction.Load();



            renderPipeline.Start();




            MacroManager.GetDefault().DefineMacro(Keys.Enter, (a) =>
            {
                RAWLoaderXNS loader = new RAWLoaderXNS(@"C:\Users\enesh\source\repos\VoxelizerRaytcast\VoxelizerRaytcast\bin\Debug\netcoreapp3.1\test.raw");

                renderPipeline.UploadData(250, 50, 150, loader.RAWXNSFile);


            }, true);



            bool loadedRod = false;
            MacroManager.GetDefault().DefineMacro(Keys.K, (a) =>
            {
                if (loadedRod) return;
                loadedRod = true;
                renderPipeline.Modifications.Add(simpleVoxelRotation);
            }, true);

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
                renderPipeline.Step(gameTime, volumeInteraction);


       

            MacroManager.GetDefault().Update(gameTime);

            YawPitchCamera.Update();


            renderPipeline.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {

            GraphicsDevice.Clear(Color.CornflowerBlue);


            renderPipeline.Draw(gameTime);

            spriteBatch.Begin();
            DebugPrinter.GetDefault().Print(spriteBatch, gameTime);
            spriteBatch.End();



            base.Draw(gameTime);
        }

    }
}
