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
        private KeyboardState oldState;

        public GameClient()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;  // Set this to your desired width
            _graphics.PreferredBackBufferHeight = 600;  // Set this to your desired height
            // allow the window to be resized
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnResize;

            _graphics.ApplyChanges();
            _client = new NetClient(new NetPeerConfiguration("game"));
            _client.Start();
            _client.Connect("213.32.89.28", 9999);
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
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        long id = message.ReadInt64();
                        float x = message.ReadFloat();
                        float y = message.ReadFloat();
                        Vector2 pos = new Vector2(x, y);
                        playerPositions[id] = pos;
                        if (id == myUniqueId) myPosition = pos;
                        System.Diagnostics.Debug.WriteLine($"Received position {pos} for player {id}");
                        break;
                }
            }

            var newState = Keyboard.GetState();
            if (newState.IsKeyDown(Keys.Left))
                _client.SendMessage(_client.CreateMessage("MOVE_LEFT"), NetDeliveryMethod.ReliableOrdered);
            if (newState.IsKeyDown(Keys.Right))
                _client.SendMessage(_client.CreateMessage("MOVE_RIGHT"), NetDeliveryMethod.ReliableOrdered);
            if (newState.IsKeyDown(Keys.Up))
                _client.SendMessage(_client.CreateMessage("MOVE_UP"), NetDeliveryMethod.ReliableOrdered);
            if (newState.IsKeyDown(Keys.Down))
                _client.SendMessage(_client.CreateMessage("MOVE_DOWN"), NetDeliveryMethod.ReliableOrdered);

            oldState = newState;

            if (newState.IsKeyDown(Keys.Escape))
                Exit();
            
            // Focus the view on the player
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
                System.Diagnostics.Debug.WriteLine($"Drawing player {player.Key} at position {player.Value * 10}");
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
