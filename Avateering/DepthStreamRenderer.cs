﻿//------------------------------------------------------------------------------
// <copyright file="DepthStreamRenderer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering
{
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// This class renders the current depth stream frame.
    /// </summary>
    public class DepthStreamRenderer : Object2D
    {
        /// <summary>
        /// The back buffer where the depth frame is scaled as requested by the Size.
        /// </summary>
        private RenderTarget2D backBuffer;

        /// <summary>
        /// The last frame of depth data.
        /// </summary>
        private short[] depthData;

        /// <summary>
        /// The depth frame as a texture.
        /// </summary>
        private Texture2D depthTexture;

        /// <summary>
        /// This Xna effect is used to convert the depth to RGB color information.
        /// </summary>
        private Effect kinectDepthVisualizer;

        /// <summary>
        /// Whether or not the back buffer needs updating.
        /// </summary>
        private bool needToRedrawBackBuffer = true;

        /// <summary>
        /// Whether the rendering has been initialized.
        /// </summary>
        private bool initialized;

        /// <summary>
        /// Initializes a new instance of the DepthStreamRenderer class.
        /// </summary>
        /// <param name="game">The related game object.</param>
        public DepthStreamRenderer(Game game)
            : base(game)
        {
            this.Size = new Vector2(160, 120);
            this.initialized = false;
        }

        /// <summary>
        /// Gets the SpriteBatch from the services.
        /// </summary>
        public SpriteBatch SharedSpriteBatch
        {
            get
            {
                return (SpriteBatch)this.Game.Services.GetService(typeof(SpriteBatch));
            }
        }

        /// <summary>
        /// Initializes the necessary children.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            this.initialized = true;
        }

        /// <summary>
        /// The update method where the new depth frame is retrieved.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // If the sensor is not found, not running, or not connected, stop now
            if (null == this.Chooser.Sensor ||
                false == this.Chooser.Sensor.IsRunning ||
                KinectStatus.Connected != this.Chooser.Sensor.Status)
            {
                return;
            }

            using (var frame = this.Chooser.Sensor.DepthStream.OpenNextFrame(0))
            {
                // Sometimes we get a null frame back if no data is ready
                if (null == frame)
                {
                    return;
                }

                // Reallocate values if necessary
                if (null == this.depthData || this.depthData.Length != frame.PixelDataLength)
                {
                    this.depthData = new short[frame.PixelDataLength];

                    this.depthTexture = new Texture2D(
                        Game.GraphicsDevice,
                        frame.Width,
                        frame.Height,
                        false,
                        SurfaceFormat.Bgra4444);

                    this.backBuffer = new RenderTarget2D(
                        Game.GraphicsDevice,
                        frame.Width,
                        frame.Height,
                        false,
                        SurfaceFormat.Color,
                        DepthFormat.None,
                        this.Game.GraphicsDevice.PresentationParameters.MultiSampleCount,
                        RenderTargetUsage.PreserveContents);
                }

                frame.CopyPixelDataTo(this.depthData);
                this.needToRedrawBackBuffer = true;
            }
        }

        /// <summary>
        /// This method renders the color and skeleton frame.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        public override void Draw(GameTime gameTime)
        {
            // If we don't have a depth target, exit
            if (null == this.depthTexture)
            {
                return;
            }

            if (false == this.initialized)
            {
                this.Initialize();
            }

            if (this.needToRedrawBackBuffer)
            {
                // Set the backbuffer and clear
                Game.GraphicsDevice.SetRenderTarget(this.backBuffer);
                Game.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1.0f, 0);

                this.depthTexture.SetData<short>(this.depthData);

                // Draw the depth image
                this.SharedSpriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, this.kinectDepthVisualizer);
                this.SharedSpriteBatch.Draw(this.depthTexture, Vector2.Zero, Color.White);
                this.SharedSpriteBatch.End();
                
                // Reset the render target and prepare to draw scaled image
                this.Game.GraphicsDevice.SetRenderTarget(null);

                // No need to re-render the back buffer until we get new data
                this.needToRedrawBackBuffer = false;
            }

            // Draw scaled image
            this.SharedSpriteBatch.Begin();
            this.SharedSpriteBatch.Draw(
                this.backBuffer,
                new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y),
                null,
                Color.White);
            this.SharedSpriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// This method loads the Xna effect.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            // This effect is used to convert depth data to color for display
            this.kinectDepthVisualizer = Game.Content.Load<Effect>("KinectDepthVisualizer");
        }
    }
}
