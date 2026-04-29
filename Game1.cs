using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MiJuegoPokemon;

enum GameState { Menu, Opciones, Lista, Pokedex }

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    Texture2D personaje;
    Texture2D mapa;
    SpriteFont fuente;

    GameState estadoActual = GameState.Menu;
    MouseState ratonAnterior;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        personaje = Content.Load<Texture2D>("personaje");
        mapa = Content.Load<Texture2D>("depositphotos_10589691-stock-photo-ground-background");
        fuente = Content.Load<SpriteFont>("fuente");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        MouseState ratonActual = Mouse.GetState();
        bool clic = ratonActual.LeftButton == ButtonState.Pressed && ratonAnterior.LeftButton == ButtonState.Released;
        Point posRaton = new Point(ratonActual.X, ratonActual.Y);

        int cx = GraphicsDevice.Viewport.Width / 2;
        int cy = GraphicsDevice.Viewport.Height / 2;

        switch (estadoActual)
        {
            case GameState.Menu:
                if (clic && new Rectangle(cx - 50, cy - 30, 150, 40).Contains(posRaton)) estadoActual = GameState.Lista;
                if (clic && new Rectangle(cx - 50, cy + 30, 150, 40).Contains(posRaton)) estadoActual = GameState.Opciones;
                break;

            case GameState.Opciones:
                if (clic && new Rectangle(cx - 100, cy, 250, 40).Contains(posRaton)) _graphics.ToggleFullScreen();
                if (clic && new Rectangle(cx - 30, cy + 70, 100, 40).Contains(posRaton)) estadoActual = GameState.Menu;
                break;

            case GameState.Lista:
                if (clic && new Rectangle(cx - 100, cy - 100, 200, 40).Contains(posRaton)) estadoActual = GameState.Pokedex;
                if (clic && new Rectangle(cx - 50, cy + 150, 100, 40).Contains(posRaton)) estadoActual = GameState.Menu;
                break;

            case GameState.Pokedex:
                if (clic && new Rectangle(cx - 50, cy + 200, 100, 40).Contains(posRaton)) estadoActual = GameState.Lista;
                break;
        }

        ratonAnterior = ratonActual;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin();

        int cx = GraphicsDevice.Viewport.Width / 2;
        int cy = GraphicsDevice.Viewport.Height / 2;

        switch (estadoActual)
        {
            case GameState.Menu:
                _spriteBatch.DrawString(fuente, "Empezar", new Vector2(cx - 50, cy - 30), Color.White);
                _spriteBatch.DrawString(fuente, "Opciones", new Vector2(cx - 50, cy + 30), Color.White);
                break;

            case GameState.Opciones:
                _spriteBatch.DrawString(fuente, "Estas en opciones", new Vector2(cx - 100, cy - 70), Color.White);
                _spriteBatch.DrawString(fuente, "Pantalla Completa", new Vector2(cx - 100, cy), Color.LightGreen);
                _spriteBatch.DrawString(fuente, "Salir", new Vector2(cx - 30, cy + 70), Color.Yellow);
                break;

            case GameState.Lista:
                _spriteBatch.DrawString(fuente, "> Spike (Planta)", new Vector2(cx - 100, cy - 100), Color.White);
                _spriteBatch.DrawString(fuente, "Volver", new Vector2(cx - 50, cy + 150), Color.Yellow);
                break;

            case GameState.Pokedex:
                _spriteBatch.Draw(personaje, new Rectangle(cx - 100, cy - 200, 200, 200), Color.White);
                _spriteBatch.DrawString(fuente, "Nombre: Spike\n\nTipo: Planta / Lucha\n\nDescripcion: Es el ultimo de su especie.\nAtaca lanzando granadas de cactus.", new Vector2(cx - 150, cy + 20), Color.White);
                _spriteBatch.DrawString(fuente, "Volver", new Vector2(cx - 50, cy + 200), Color.Yellow);
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}