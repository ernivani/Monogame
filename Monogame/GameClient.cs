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
        private Dictionary<long, int> playerPositions = new Dictionary<long, int>();
        private Texture2D playerTexture;

        public GameClient()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            _client = new NetClient(new NetPeerConfiguration("game"));
            _client.Start();
            _client.Connect("213.32.89.28", 9999);

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
                        int pos = message.ReadInt32();
                        playerPositions[id] = pos;
                        System.Diagnostics.Debug.WriteLine($"Received position {pos} for player {id}");
                        break;
                }
            }

            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Left))
                _client.SendMessage(_client.CreateMessage("MOVE_LEFT"), NetDeliveryMethod.ReliableOrdered);
            else if (keyboardState.IsKeyDown(Keys.Right))
                _client.SendMessage(_client.CreateMessage("MOVE_RIGHT"), NetDeliveryMethod.ReliableOrdered);
            else if (keyboardState.IsKeyDown(Keys.Up))
                _client.SendMessage(_client.CreateMessage("MOVE_UP"), NetDeliveryMethod.ReliableOrdered);
            else if (keyboardState.IsKeyDown(Keys.Down))
                _client.SendMessage(_client.CreateMessage("MOVE_DOWN"), NetDeliveryMethod.ReliableOrdered);
            
            else if (keyboardState.IsKeyDown(Keys.Escape))
                Exit();
            

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
            foreach (var player in playerPositions)
            {
                _spriteBatch.Draw(playerTexture, new Vector2(player.Value * 10, 0), Color.White);
                System.Diagnostics.Debug.WriteLine($"Drawing player {player.Key} at position {player.Value * 10}");
            }
            // For debugging: Draw a player at a known position
            _spriteBatch.Draw(playerTexture, new Vector2(100, 100), Color.Red);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }

    
}
