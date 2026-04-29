using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MiJuegoPokemon;

enum GameState { Menu, Opciones, SeleccionPersonaje, Exploracion, Lista, Pokedex, Anadir }

class Personaje
{
    public string Nombre;
    public string Tipo;
    public string Descripcion;
    public Texture2D Textura;
}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private const int VirtualWidth = 1600;
    private const int VirtualHeight = 720;
    private RenderTarget2D _renderTarget;
    private Rectangle _screenDestination;

    SpriteFont fuente;
    Texture2D mcMale, mcFemale, mcSeleccionado, puntoBlanco, circuloBorde;
    List<Personaje> pokedex = new List<Personaje>();
    
    GameState estadoActual = GameState.Menu;
    MouseState ratonAnterior;
    KeyboardState tecladoAnterior;
    
    string nombreJugador = "";
    Vector2 posJugador = new Vector2(800, 360);
    float velocidadJugador = 300f;
    bool menuLateralAbierto = false;
    int seleccionadoIndex = 0;

    float alphaMale = 0f, alphaFemale = 0f;
    const float VelocidadAnimacion = 5f; 

    string inputNombre = "", inputTipo = "", inputDesc = "", inputImagen = "";
    int campoActivo = 0;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        _graphics.PreferredBackBufferWidth = 1600;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        Window.TextInput += Window_TextInput;
        base.Initialize();
    }

    private void Window_TextInput(object sender, TextInputEventArgs e)
    {
        if (estadoActual == GameState.SeleccionPersonaje)
        {
            if (e.Key == Keys.Back && nombreJugador.Length > 0) nombreJugador = nombreJugador.Substring(0, nombreJugador.Length - 1);
            else if (fuente.Characters.Contains(e.Character) && nombreJugador.Length < 15) nombreJugador += e.Character;
        }
        else if (estadoActual == GameState.Anadir)
        {
            if (e.Key == Keys.Back)
            {
                if (campoActivo == 0 && inputNombre.Length > 0) inputNombre = inputNombre.Substring(0, inputNombre.Length - 1);
                else if (campoActivo == 1 && inputTipo.Length > 0) inputTipo = inputTipo.Substring(0, inputTipo.Length - 1);
                else if (campoActivo == 2 && inputDesc.Length > 0) inputDesc = inputDesc.Substring(0, inputDesc.Length - 1);
            }
            else if (fuente.Characters.Contains(e.Character))
            {
                if (campoActivo == 0) inputNombre += e.Character;
                else if (campoActivo == 1) inputTipo += e.Character;
                else if (campoActivo == 2) inputDesc += e.Character;
            }
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);
        fuente = Content.Load<SpriteFont>("fuente");
        
        // Crear punto blanco para rectángulos
        puntoBlanco = new Texture2D(GraphicsDevice, 1, 1);
        puntoBlanco.SetData(new[] { Color.White });

        // Generar textura de círculo para el border-radius
        int radius = 20;
        int diameter = radius * 2;
        circuloBorde = new Texture2D(GraphicsDevice, diameter, diameter);
        Color[] colorData = new Color[diameter * diameter];
        for (int x = 0; x < diameter; x++) {
            for (int y = 0; y < diameter; y++) {
                Vector2 position = new Vector2(x - radius, y - radius);
                if (position.Length() <= radius) colorData[x + y * diameter] = Color.White;
                else colorData[x + y * diameter] = Color.Transparent;
            }
        }
        circuloBorde.SetData(colorData);

        mcMale = Texture2D.FromStream(GraphicsDevice, File.OpenRead(Path.Combine("src", "MainCharacters", "MC_Male.png")));
        mcFemale = Texture2D.FromStream(GraphicsDevice, File.OpenRead(Path.Combine("src", "MainCharacters", "MC_Female.png")));
        CargarPokedex();
    }

    private void CargarPokedex()
    {
        pokedex.Clear();
        string ruta = Path.Combine("src", "Pokedex");
        if (!Directory.Exists(ruta)) Directory.CreateDirectory(ruta);
        foreach (string png in Directory.GetFiles(ruta, "*.png"))
        {
            Personaje p = new Personaje();
            using (var stream = File.OpenRead(png)) p.Textura = Texture2D.FromStream(GraphicsDevice, stream);
            string txt = png.Replace(".png", ".txt");
            if (File.Exists(txt))
            {
                string[] lineas = File.ReadAllLines(txt);
                p.Nombre = lineas.Length > 0 ? lineas[0] : "???";
                p.Tipo = lineas.Length > 1 ? lineas[1] : "???";
                p.Descripcion = lineas.Length > 2 ? string.Join("\n", lineas, 2, lineas.Length - 2) : "";
            }
            pokedex.Add(p);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        CalculateScreenDestination();
        MouseState ratonActual = Mouse.GetState();
        KeyboardState tecladoActual = Keyboard.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        float scaleX = (float)VirtualWidth / _screenDestination.Width;
        float scaleY = (float)VirtualHeight / _screenDestination.Height;
        Point pos = new Point((int)((ratonActual.X - _screenDestination.X) * scaleX), (int)((ratonActual.Y - _screenDestination.Y) * scaleY));

        bool clic = ratonActual.LeftButton == ButtonState.Pressed && ratonAnterior.LeftButton == ButtonState.Released;
        int cx = VirtualWidth / 2;
        int cy = VirtualHeight / 2;

        switch (estadoActual)
        {
            case GameState.Menu:
                if (clic && new Rectangle(cx - 100, cy - 50, 200, 40).Contains(pos)) estadoActual = GameState.SeleccionPersonaje;
                if (clic && new Rectangle(cx - 100, cy + 10, 200, 40).Contains(pos)) estadoActual = GameState.Opciones;
                break;

            case GameState.SeleccionPersonaje:
                Rectangle rM = new Rectangle(cx - 550, 120, 450, 550);
                Rectangle rF = new Rectangle(cx + 100, 120, 450, 550);
                if (rM.Contains(pos) || mcSeleccionado == mcMale) alphaMale = MathHelper.Clamp(alphaMale + dt * VelocidadAnimacion, 0f, 1f);
                else alphaMale = MathHelper.Clamp(alphaMale - dt * VelocidadAnimacion, 0f, 1f);
                if (rF.Contains(pos) || mcSeleccionado == mcFemale) alphaFemale = MathHelper.Clamp(alphaFemale + dt * VelocidadAnimacion, 0f, 1f);
                else alphaFemale = MathHelper.Clamp(alphaFemale - dt * VelocidadAnimacion, 0f, 1f);

                if (clic && rM.Contains(pos)) mcSeleccionado = mcMale;
                if (clic && rF.Contains(pos)) mcSeleccionado = mcFemale;
                if (clic && nombreJugador.Length > 0 && mcSeleccionado != null && new Rectangle(cx - 100, 670, 200, 50).Contains(pos)) 
                    estadoActual = GameState.Exploracion;
                break;

            case GameState.Exploracion:
                if (tecladoActual.IsKeyDown(Keys.W)) posJugador.Y -= velocidadJugador * dt;
                if (tecladoActual.IsKeyDown(Keys.S)) posJugador.Y += velocidadJugador * dt;
                if (tecladoActual.IsKeyDown(Keys.A)) posJugador.X -= velocidadJugador * dt;
                if (tecladoActual.IsKeyDown(Keys.D)) posJugador.X += velocidadJugador * dt;

                if (tecladoActual.IsKeyDown(Keys.X) && tecladoAnterior.IsKeyUp(Keys.X))
                    menuLateralAbierto = !menuLateralAbierto;

                if (menuLateralAbierto && clic)
                {
                    if (new Rectangle(VirtualWidth - 280, 80, 250, 60).Contains(pos))
                    {
                        estadoActual = GameState.Lista;
                        menuLateralAbierto = false;
                    }
                }
                break;

            case GameState.Lista:
                for (int i = 0; i < pokedex.Count; i++)
                    if (clic && new Rectangle(900, 150 + (i * 45), 500, 35).Contains(pos)) { seleccionadoIndex = i; estadoActual = GameState.Pokedex; }
                if (clic && new Rectangle(VirtualWidth - 150, VirtualHeight - 70, 130, 50).Contains(pos)) estadoActual = GameState.Exploracion;
                break;

            case GameState.Pokedex:
                if (clic && new Rectangle(900, 620, 200, 50).Contains(pos)) estadoActual = GameState.Lista;
                break;

            case GameState.Opciones:
                if (clic && new Rectangle(cx - 100, cy, 250, 40).Contains(pos)) estadoActual = GameState.Anadir;
                if (clic && new Rectangle(cx - 50, cy + 100, 100, 40).Contains(pos)) estadoActual = GameState.Menu;
                break;

            case GameState.Anadir:
                if (clic && new Rectangle(cx - 110, cy + 150, 100, 40).Contains(pos) && File.Exists(inputImagen))
                {
                    string f = Path.GetFileName(inputImagen);
                    File.Copy(inputImagen, Path.Combine("src", "Pokedex", f), true);
                    File.WriteAllText(Path.Combine("src", "Pokedex", Path.GetFileNameWithoutExtension(f) + ".txt"), $"{inputNombre}\n{inputTipo}\n{inputDesc}");
                    CargarPokedex();
                    estadoActual = GameState.Opciones;
                }
                if (clic && new Rectangle(cx + 10, cy + 150, 100, 40).Contains(pos)) estadoActual = GameState.Opciones;
                break;
        }

        ratonAnterior = ratonActual;
        tecladoAnterior = tecladoActual;
        base.Update(gameTime);
    }

    private void CalculateScreenDestination()
    {
        float target = (float)VirtualWidth / VirtualHeight;
        float window = (float)Window.ClientBounds.Width / Window.ClientBounds.Height;
        if (window > target)
        {
            int w = (int)(Window.ClientBounds.Height * target);
            _screenDestination = new Rectangle((Window.ClientBounds.Width - w) / 2, 0, w, Window.ClientBounds.Height);
        }
        else
        {
            int h = (int)(Window.ClientBounds.Width / target);
            _screenDestination = new Rectangle(0, (Window.ClientBounds.Height - h) / 2, Window.ClientBounds.Width, h);
        }
    }

    private void DrawRoundedRect(Rectangle rect, Color color, int radius)
    {
        // Centrales
        _spriteBatch.Draw(puntoBlanco, new Rectangle(rect.X + radius, rect.Y, rect.Width - radius * 2, rect.Height), color);
        _spriteBatch.Draw(puntoBlanco, new Rectangle(rect.X, rect.Y + radius, rect.Width, rect.Height - radius * 2), color);
        
        // Esquinas (Círculos)
        Rectangle d = new Rectangle(0, 0, radius * 2, radius * 2);
        _spriteBatch.Draw(circuloBorde, new Rectangle(rect.X, rect.Y, radius * 2, radius * 2), color); // Superior Izq
        _spriteBatch.Draw(circuloBorde, new Rectangle(rect.X + rect.Width - radius * 2, rect.Y, radius * 2, radius * 2), color); // Superior Der
        _spriteBatch.Draw(circuloBorde, new Rectangle(rect.X, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2), color); // Inferior Izq
        _spriteBatch.Draw(circuloBorde, new Rectangle(rect.X + rect.Width - radius * 2, rect.Y + rect.Height - radius * 2, radius * 2, radius * 2), color); // Inferior Der
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(25, 35, 40));
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        int cx = VirtualWidth / 2, cy = VirtualHeight / 2;

        switch (estadoActual)
        {
            case GameState.Menu:
                _spriteBatch.DrawString(fuente, "COMENZAR", new Vector2(cx - 70, cy - 50), Color.White);
                _spriteBatch.DrawString(fuente, "OPCIONES", new Vector2(cx - 70, cy + 10), Color.White);
                break;

            case GameState.SeleccionPersonaje:
                _spriteBatch.DrawString(fuente, "ELIGE TU PERSONAJE", new Vector2(cx - 150, 50), Color.Yellow);
                _spriteBatch.Draw(mcMale, new Rectangle(cx - 550, 120, 450, 550), Color.Lerp(Color.Black * 0.7f, Color.White, alphaMale));
                _spriteBatch.Draw(mcFemale, new Rectangle(cx + 100, 120, 450, 550), Color.Lerp(Color.Black * 0.7f, Color.White, alphaFemale));
                _spriteBatch.DrawString(fuente, "NOMBRE: " + nombreJugador + "_", new Vector2(cx - 100, 640), Color.White);
                if (nombreJugador.Length > 0 && mcSeleccionado != null)
                    _spriteBatch.DrawString(fuente, "[ CONFIRMAR ]", new Vector2(cx - 100, 680), Color.GreenYellow);
                break;

            case GameState.Exploracion:
                GraphicsDevice.Clear(Color.Black);
                _spriteBatch.Draw(mcSeleccionado, new Rectangle((int)posJugador.X, (int)posJugador.Y, 80, 100), Color.White);
                
                if (menuLateralAbierto)
                {
                    Rectangle menuRect = new Rectangle(VirtualWidth - 320, 20, 300, VirtualHeight - 40);
                    
                    // Sombra
                    _spriteBatch.Draw(puntoBlanco, new Rectangle(menuRect.X - 8, menuRect.Y + 8, menuRect.Width, menuRect.Height), Color.Black * 0.4f);
                    
                    // Fondo Blanco con Border Radius
                    DrawRoundedRect(menuRect, Color.White, 20);
                    
                    // Opción Pokedex
                    _spriteBatch.DrawString(fuente, "POKEDEX", new Vector2(VirtualWidth - 270, 100), Color.Black);
                }
                break;

            case GameState.Lista:
            case GameState.Pokedex:
                _spriteBatch.Draw(mcSeleccionado, new Rectangle(80, 40, 550, 640), Color.White);
                _spriteBatch.DrawString(fuente, "ENTRENADOR: " + nombreJugador.ToUpper(), new Vector2(80, 680), Color.Yellow);
                if (estadoActual == GameState.Lista)
                {
                    _spriteBatch.DrawString(fuente, "POKEDEX DISPONIBLE:", new Vector2(900, 100), Color.Cyan);
                    for (int i = 0; i < pokedex.Count; i++)
                        _spriteBatch.DrawString(fuente, $"{i + 1}. {pokedex[i].Nombre}", new Vector2(900, 150 + (i * 45)), Color.White);
                    _spriteBatch.DrawString(fuente, "SALIR", new Vector2(VirtualWidth - 150, VirtualHeight - 70), Color.Red);
                }
                else
                {
                    var p = pokedex[seleccionadoIndex];
                    _spriteBatch.Draw(p.Textura, new Rectangle(900, 80, 350, 350), Color.White);
                    _spriteBatch.DrawString(fuente, $"NOMBRE: {p.Nombre}\nTIPO: {p.Tipo}\n\n{p.Descripcion}", new Vector2(900, 450), Color.White);
                    _spriteBatch.DrawString(fuente, "[ VOLVER ]", new Vector2(900, 620), Color.Yellow);
                }
                break;

            case GameState.Opciones:
                _spriteBatch.DrawString(fuente, "AÑADIR POKEMON", new Vector2(cx - 100, cy), Color.White);
                _spriteBatch.DrawString(fuente, "VOLVER", new Vector2(cx - 50, cy + 100), Color.Red);
                break;
        }
        _spriteBatch.End();
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, null);
        _spriteBatch.Draw(_renderTarget, _screenDestination, Color.White);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}