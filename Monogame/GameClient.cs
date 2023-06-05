using System;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Monogame
{
    public struct Player
    {
        public long Id { get; private set; }
        public Vector2 Position { get; set; }

        public Player(long id, Vector2 position)
        {
            this.Id = id;
            this.Position = position;
        }
    }

    public class Camera
    {
        private Vector2 _position;
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public Matrix Transform { get; private set; }

        public Camera(Vector2 position)
        {
            _position = position;
        }

        public void Update(Vector2 playerPosition, int screenWidth, int screenHeight)
        {
            var cameraPosition = new Vector2(
                screenWidth / 2f - playerPosition.X,
                screenHeight / 2f - playerPosition.Y);
            Transform = Matrix.CreateTranslation(new Vector3(cameraPosition, 0));
        }
    }

    public class GameClient : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private NetClient _client;
        private Player myPlayer;
        private Texture2D playerTexture;
        private Vector2 mapSize = new Vector2(1000, 1000); // Assuming a map size of 1000x1000 units
        private int playerSize = 10;
        private Camera camera;

        public GameClient()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.IsFullScreen = true; // set game to fullscreen
            _graphics.PreferredBackBufferWidth = 1920;  // set this value to the desired width of your window
            _graphics.PreferredBackBufferHeight = 1080;   // set this value to the desired height of your window
        }

        protected override void Initialize()
        {
            base.Initialize();

            _graphics.ApplyChanges();

            _client = new NetClient(new NetPeerConfiguration("game"));
            _client.Start();
            _client.Connect("127.0.0.1", 9999);
            myPlayer = new Player(_client.UniqueIdentifier, Vector2.Zero);

            camera = new Camera(Vector2.Zero);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Make the player texture larger, so it's more visible.
            playerTexture = new Texture2D(GraphicsDevice, playerSize, playerSize);
            Color[] data = new Color[playerSize * playerSize];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.White;
            playerTexture.SetData(data);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var keyboardState = Keyboard.GetState();

            // Handle arrow key camera movement
            const float cameraSpeed = 5f; // Adjust the camera movement speed as needed

            if (keyboardState.IsKeyDown(Keys.Left))
            {
                camera.Position += new Vector2(-cameraSpeed, 0);
            }
            else if (keyboardState.IsKeyDown(Keys.Right))
            {
                camera.Position += new Vector2(cameraSpeed, 0);
            }

            if (keyboardState.IsKeyDown(Keys.Up))
            {
                camera.Position += new Vector2(0, -cameraSpeed);
            }
            else if (keyboardState.IsKeyDown(Keys.Down))
            {
                camera.Position += new Vector2(0, cameraSpeed);
            }

            // Handle player movement using arrow keys
            const float playerSpeed = 3f; // Adjust the player movement speed as needed

            Vector2 playerMovement = Vector2.Zero;

            if (keyboardState.IsKeyDown(Keys.A))
            {
                playerMovement += new Vector2(-playerSpeed, 0);
            }
            else if (keyboardState.IsKeyDown(Keys.D))
            {
                playerMovement += new Vector2(playerSpeed, 0);
            }

            if (keyboardState.IsKeyDown(Keys.W))
            {
                playerMovement += new Vector2(0, -playerSpeed);
            }
            else if (keyboardState.IsKeyDown(Keys.S))
            {
                playerMovement += new Vector2(0, playerSpeed);
            }

            myPlayer.Position += playerMovement;

            var mouseState = Mouse.GetState();
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                var mousePos = new Vector2(mouseState.X, mouseState.Y);
                mousePos = ScreenToWorldCoordinates(mousePos);
                myPlayer.Position = mousePos;

                var outMsg = _client.CreateMessage();
                outMsg.Write(myPlayer.Id);
                outMsg.Write(myPlayer.Position.X);
                outMsg.Write(myPlayer.Position.Y);
                _client.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered);
            }

            camera.Update(myPlayer.Position, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, camera.Transform);
            var screenPos = WorldToScreenCoordinates(myPlayer.Position);
            _spriteBatch.Draw(playerTexture, screenPos, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private Vector2 ScreenToWorldCoordinates(Vector2 screenCoordinates)
        {
            float xRatio = mapSize.X / _graphics.PreferredBackBufferWidth;
            float yRatio = mapSize.Y / _graphics.PreferredBackBufferHeight;
            return new Vector2(screenCoordinates.X * xRatio, screenCoordinates.Y * yRatio);
        }

        private Vector2 WorldToScreenCoordinates(Vector2 worldCoordinates)
        {
            float xRatio = _graphics.PreferredBackBufferWidth / mapSize.X;
            float yRatio = _graphics.PreferredBackBufferHeight / mapSize.Y;
            return new Vector2(worldCoordinates.X * xRatio, worldCoordinates.Y * yRatio);
        }
    }
}
