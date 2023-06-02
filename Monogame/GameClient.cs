using System;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Monogame
{
    public class GameClient : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private NetClient _client;
        private Dictionary<long, Vector2> playerPositions = new Dictionary<long, Vector2>();
        private Texture2D playerTexture;
        private long myUniqueId;
        private Vector2 myPosition;
        private Matrix viewMatrix;

        public GameClient()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnResize;

            _graphics.ApplyChanges();
            _client = new NetClient(new NetPeerConfiguration("game"));
            _client.Start();
            _client.Connect("127.0.0.1", 9999);
            myUniqueId = _client.UniqueIdentifier;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            playerTexture = new Texture2D(GraphicsDevice, 10, 10);
            Color[] data = new Color[10 * 10];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.White;
            playerTexture.SetData(data);
        }

        protected override void Update(GameTime gameTime)
        {
            NetIncomingMessage message;
            while ((message = _client.ReadMessage()) != null)
            {
                System.Diagnostics.Debug.WriteLine(message.MessageType);
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        long id = message.ReadInt64();
                        float x = message.ReadFloat();
                        float y = message.ReadFloat();
                        Vector2 pos = new Vector2(x, y);
                        playerPositions[id] = pos;
                        if (id == myUniqueId) myPosition = pos;
                        
                        break;
                }
            }

            KeyboardState newState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (mouseState.RightButton == ButtonState.Pressed)
            {
                Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
                var outmessage = _client.CreateMessage();
                outmessage.Write("MOVE_TO");
                float mousePosX = mousePos.X / _graphics.PreferredBackBufferWidth;
                outmessage.Write(mousePosX);
                float mousePosY = mousePos.Y / _graphics.PreferredBackBufferHeight;
                outmessage.Write(mousePosY);
                _client.SendMessage(outmessage, NetDeliveryMethod.ReliableOrdered);
            }

            if (newState.IsKeyDown(Keys.Escape))
                Exit();

            viewMatrix = Matrix.CreateTranslation(
                -myPosition.X + _graphics.PreferredBackBufferWidth / 2,
                -myPosition.Y + _graphics.PreferredBackBufferHeight / 2,
                0);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, viewMatrix);
            foreach (var player in playerPositions)
            {
                _spriteBatch.Draw(playerTexture, player.Value * 10, Color.White);
            }
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        public void OnResize(Object sender, EventArgs e)
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            _graphics.ApplyChanges();
        }
    }
}
