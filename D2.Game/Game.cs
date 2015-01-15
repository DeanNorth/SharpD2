using System;
using System.Text;
using SharpDX;


namespace D2.Game
{
    // Use these namespaces here to override SharpDX.Direct3D11
    using SharpDX.Toolkit;
    using SharpDX.Toolkit.Graphics;
    using SharpDX.Toolkit.Input;
    using System.Collections.Generic;
    using D2.FileTypes;
    using System.Linq;
    using SharpDX.Toolkit.CefGlue;

    /// <summary>
    /// Simple Game game using SharpDX.Toolkit.
    /// </summary>
    public class Game : SharpDX.Toolkit.Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;
        private SpriteBatch spriteBatch;
        private Texture2D ballsTexture;
        private SpriteFont arial16Font;

        private KeyboardManager keyboard;
        private KeyboardState keyboardState;

        private MouseManager mouse;
        private MouseState mouseState;

        private float billboardWidth = (float)Math.Sqrt(5 * 5 * 2);
        private float billboardHeight = (float)Math.Sqrt(5 * 5 * 2) * 6.0f * 1.8f;
        private GeometricPrimitive plane;
        private GeometricPrimitive player;
        private GeometricPrimitive billboard;

        private BasicEffect basicEffect;
        private Texture2D texture;

        private Vector3 cameraOffset = new Vector3(4, 4, 5f);

        private List<DT1Texture> tiles = new List<DT1Texture>();
        private Effect floorEffect;

        private float zoom = 0.18f;

        private DS1File level;

        private Vector2 mousePressedPosition;
        private Vector3 mousePressedCameraPosition = new Vector3(0, 0, 0);
        private Vector3 mousePressedCameraOffset = new Vector3(0, 0, 0);

        private Vector3 cameraPosition = new Vector3(0 ,0, 0);

        private Vector2 selectedTile = new Vector2(0, 0);

        private Area tristram;

        private Effect quadEffect;
        private SharpDXCefBrowser browser;

        /// <summary>
        /// Initializes a new instance of the <see cref="Game" /> class.
        /// </summary>
        public Game()
        {
            // Creates a graphics manager. This is mandatory.
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Setup the relative directory to the executable directory
            // for loading contents with the ContentManager

            Content.RootDirectory = "Content";
            
            Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Blizzard Entertainment\Diablo II");
            if (registryKey == null)
            {
                registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Blizzard Entertainment\Diablo II Shareware");
            }

            string installPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Content";
            
            if (registryKey != null)
            {
                installPath = registryKey.GetValue("InstallPath").ToString();
            }

            Content.Resolvers.Add(new MPQContentResolver(installPath));
            //Content.Resolvers.Add(new SharpDX.Toolkit.Content.FileSystemContentResolver(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Content"));

            Content.ReaderFactories.Add(new Diablo2ReaderFactory());

            // Initialize input keyboard system
            keyboard = new KeyboardManager(this);

            // Initialize input mouse system
            mouse = new MouseManager(this);
        }

        protected override void Initialize()
        {
            // Modify the title of the window
            Window.Title = "Game";

            Window.IsMouseVisible = true;

            ((System.Windows.Forms.Form)Window.NativeWindow).ClientSize = new System.Drawing.Size(800, 600);

            Window.ClientSizeChanged += (s, ea) =>
            {
                CalculateResize();
            };

            base.Initialize();

            CefConfig.Initialize(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Cef");
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            CefConfig.Shutdown();
            base.OnExiting(sender, args);
        }

        protected override void LoadContent()
        {
            // Instantiate a SpriteBatch
            spriteBatch = ToDisposeContent(new SpriteBatch(GraphicsDevice));

            // Loads the balls texture (32 textures (32x32) stored vertically => 32 x 1024 ).
            // The [Balls.dds] file is defined with the build action [ToolkitTexture] in the project
            ballsTexture = Content.Load<Texture2D>("Balls");

            // Loads a sprite font
            // The [Arial16.xml] file is defined with the build action [ToolkitFont] in the project
            arial16Font = Content.Load<SpriteFont>("Arial16");

            billboard = ToDisposeContent(GeometricPrimitive.Plane.New(GraphicsDevice, billboardWidth, billboardHeight));  // 1.8 * 30
            plane = ToDisposeContent(GeometricPrimitive.Plane.New(GraphicsDevice,5,5));
            player = ToDisposeContent(GeometricPrimitive.Cylinder.New(GraphicsDevice, 3, 1, 32, true));

            basicEffect = ToDisposeContent(new BasicEffect(GraphicsDevice)
            {
                View = Matrix.LookAtLH(cameraPosition + cameraOffset, cameraPosition, Vector3.UnitZ),
                World = Matrix.Identity
            });

            CalculateResize();

            texture = Content.Load<Texture2D>("GeneticaMortarlessBlocks");

            level = Content.Load<DS1File>(@"data\global\tiles\act1\town\towne1.ds1");

            cameraPosition = new Vector3(level.Width / 2 * 5, level.Height / 2 * 5, 0);

            floorEffect = Content.Load<Effect>("FloorEffect");


            foreach (var file in level.files)
            {
                //try
                //{
                    tiles.Add(Content.Load<DT1Texture>(file.Substring(4).Replace(".tg1", ".dt1")));
                //}
                //catch (Exception ex)
                //{
                //    tiles.Add(new DT1Texture()); //Texture2D.New(GraphicsDevice, Image.New(new ImageDescription() { ArraySize = 1, Width = 128, Height = 128, Depth = 1, Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm, Dimension = TextureDimension.Texture2D })));
                //}
            }

            tristram = new Area(level, tiles);

            basicEffect.Texture = texture;
            basicEffect.TextureEnabled = true;

            quadEffect = Content.Load<Effect>("QuadEffect");

            browser = ToDisposeContent(new SharpDXCefBrowser(GraphicsDevice, Window.ClientBounds.Width, Window.ClientBounds.Height));


            base.LoadContent();
        }

        private void CalculateResize()
        {
            // A game tile is 160 * 79
            float w = (float)GraphicsDevice.BackBuffer.Width / 4; // / 160f;
            float h = (float)GraphicsDevice.BackBuffer.Height / 3; // / 80f;

            //w *= 7.15f;
            //h *= 4.76f;

            w *= zoom;
            h *= zoom;

            basicEffect.Projection = Matrix.OrthoLH(w, h, -1000.0f, 1000.0f);
        }

        private Vector2 ScreenToWorld(Vector2 input)
        {
            //var diff = input;

            var absInput = input * new Vector2(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height);

            ////float scale = 0.18f;

            ////var x = Vector3.TransformCoordinate(new Vector3((diff * absDiff * zoom * scale).X, 0, (diff * absDiff * zoom * scale).X), basicEffect.World);
            ////var y = Vector3.TransformCoordinate(new Vector3(0, (diff * absDiff * zoom * (0.45f)).Y, 0), basicEffect.World);

            //var v3 = Vector3.TransformCoordinate(new Vector3(diff * absDiff, 0), basicEffect.View);

            //return new Vector2(v3.X,v3.Y);

            //var worldPoint = GraphicsDevice.Viewport.Unproject(new Vector3(absInput, 0), basicEffect.Projection, basicEffect.View, Matrix.Identity);

            Vector3 nearPoint = GraphicsDevice.Viewport.Unproject(new Vector3(absInput, 0f), basicEffect.Projection, basicEffect.View, Matrix.Identity);
            Vector3 farPoint = GraphicsDevice.Viewport.Unproject(new Vector3(absInput, 1f), basicEffect.Projection, basicEffect.View, Matrix.Identity);

            var ray = new Ray(nearPoint, Vector3.Normalize(farPoint - nearPoint));
            Plane groundPlane = new Plane(Vector3.Zero, Vector3.UnitZ);

            float result;
            if (ray.Intersects(ref groundPlane, out result))
            {
                Vector3 worldPoint = ray.Position + ray.Direction * result;

                worldPoint = new Vector3((float)Math.Round(worldPoint.X), (float)Math.Round(worldPoint.Y), 0);
                
                worldPoint = worldPoint / 5;

                worldPoint = new Vector3((float)Math.Round(worldPoint.X), (float)Math.Round(worldPoint.Y), 0);
                

                return new Vector2(worldPoint.X, worldPoint.Y);
            }

            return new Vector2(0, 0);
        }

        bool loaded = false;

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!loaded || keyboardState.IsKeyPressed(Keys.Enter))
            {
                browser.NavigateTo("http://www.google.co.uk");
                browser.Focus();
                loaded = true;
            }


            

            // Get the current state of the keyboard
            keyboardState = keyboard.GetState();

            // Get the current state of the mouse
            mouseState = mouse.GetState();


            if (mouseState.WheelDelta != 0)
            {
                if(zoom - mouseState.WheelDelta / 2000.0f > 0.001f)
                {
                    zoom -= mouseState.WheelDelta / 2000.0f;
                    CalculateResize();
                }
            }

            if (mouseState.LeftButton.Pressed)
            {
                mousePressedPosition = new Vector2(mouseState.X, mouseState.Y);
                mousePressedCameraPosition = cameraPosition;
            }

            if (mouseState.LeftButton.Down)
            {
                var diff = new Vector2(mouseState.X, mouseState.Y) - mousePressedPosition;

                var absDiff = new Vector2(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height);

                float scale = 0.18f;

                var transdiffX = Vector3.TransformCoordinate(new Vector3((diff * absDiff * zoom * scale).X, 0, (diff * absDiff * zoom * scale).X), basicEffect.World);
                var transdiffY = Vector3.TransformCoordinate(new Vector3(0, (diff * absDiff * zoom * (0.45f)).Y, 0), basicEffect.World);


                cameraPosition = mousePressedCameraPosition + transdiffY - transdiffX;
            }

            if (mouseState.RightButton.Pressed)
            {
                mousePressedPosition = new Vector2(mouseState.X, mouseState.Y);
                mousePressedCameraOffset = cameraOffset;
            }

            if (mouseState.RightButton.Down)
            {
                var diff = mouseState.X - mousePressedPosition.X;

                var trans = Vector3.TransformCoordinate(cameraOffset, Matrix.RotationZ(diff));

                cameraOffset = trans;

                //cameraOffset.Z = mouseState.Y - mousePressedPosition.Y;
            }

            selectedTile = ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));
        }

        protected override void Draw(GameTime gameTime)
        {
            // Use time in seconds directly
            var time = (float)gameTime.TotalGameTime.TotalSeconds;

            // Clears the screen with the Color.CornflowerBlue
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // ------------------------------------------------------------------------
            // Draw the some 2d text
            // ------------------------------------------------------------------------
            spriteBatch.Begin();
            var text = new StringBuilder("Zoom").Append(zoom / 0.18f).AppendLine();

            // Display pressed keys
            var pressedKeys = new List<Keys>(); //keyboardState.GetDownKeys();
            keyboardState.GetDownKeys(pressedKeys);
            text.Append("Key Pressed: [");
            foreach (var key in pressedKeys)
            {
                text.Append(key.ToString());
                text.Append(" ");
            }
            text.Append("]").AppendLine();

            // Display mouse coordinates and mouse button status
            text.AppendFormat("Selected: {4},{5}", mouseState.X, mouseState.Y, mouseState.LeftButton, mouseState.RightButton, selectedTile.X, selectedTile.Y).AppendLine();

            spriteBatch.DrawString(arial16Font, text.ToString(), new Vector2(16, 16), Color.White);
            spriteBatch.End();


            basicEffect.View = Matrix.LookAtLH(cameraPosition + cameraOffset, cameraPosition, Vector3.UnitZ);

            tristram.DrawFloors(GraphicsDevice, basicEffect.View, basicEffect.Projection, floorEffect, plane);

            // Selected Tile
            basicEffect.TextureEnabled = true;
            basicEffect.World = Matrix.Translation(selectedTile.X * 5, selectedTile.Y * 5, 0);
            plane.Draw(basicEffect);

            tristram.DrawWalls(GraphicsDevice, basicEffect.View, basicEffect.Projection, floorEffect, billboard);

            // Test Billboard
            //basicEffect.World = Matrix.RotationX(MathUtil.DegreesToRadians(90)) * Matrix.RotationZ(MathUtil.DegreesToRadians(-45)) * Matrix.Translation(50, 50, 0);
            //billboard.Draw(basicEffect);

            GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.CullBack);
            basicEffect.TextureEnabled = false;
            basicEffect.World = Matrix.Translation(0, 1, 0) * Matrix.RotationX(MathUtil.DegreesToRadians(90));
            player.Draw(basicEffect);


            spriteBatch.Begin();
            spriteBatch.DrawString(arial16Font, text.ToString(), new Vector2(16, 16), Color.White);
            spriteBatch.End();

            // Draw the UI
            quadEffect.Parameters["Texture"].SetResource(browser.Texture);
            GraphicsDevice.Quad.Draw(quadEffect, true);

            base.Draw(gameTime);
        }
    }
}
