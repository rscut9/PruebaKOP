using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MiJuegoPokemon;

enum GameState { Menu, Opciones, SeleccionPersonaje, Lista, Pokedex, Anadir }

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

    // Texturas y Fuentes
    SpriteFont fuente;
    Texture2D mcMale, mcFemale, mcSeleccionado;
    
    // Datos de la Pokedex
    List<Personaje> pokedex = new List<Personaje>();
    
    // Estado de Selección y Animación
    string nombreJugador = "";
    GameState estadoActual = GameState.Menu;
    MouseState ratonAnterior;
    int seleccionadoIndex = 0;

    // Variables de Animación
    float alphaMale = 0f;
    float alphaFemale = 0f;
    const float VelocidadAnimacion = 5f; 

    // Inputs para Añadir
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
        
        float scaleX = (float)VirtualWidth / _screenDestination.Width;
        float scaleY = (float)VirtualHeight / _screenDestination.Height;
        Point pos = new Point((int)((ratonActual.X - _screenDestination.X) * scaleX), (int)((ratonActual.Y - _screenDestination.Y) * scaleY));

        bool clic = ratonActual.LeftButton == ButtonState.Pressed && ratonAnterior.LeftButton == ButtonState.Released;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        int cx = VirtualWidth / 2;
        int cy = VirtualHeight / 2;

        // Salir solo si se pulsa Escape en el Menú
        if (Keyboard.GetState().IsKeyDown(Keys.Escape) && estadoActual == GameState.Menu) Exit();

        switch (estadoActual)
        {
            case GameState.Menu:
                if (clic && new Rectangle(cx - 100, cy - 50, 200, 40).Contains(pos)) estadoActual = GameState.SeleccionPersonaje;
                if (clic && new Rectangle(cx - 100, cy + 10, 200, 40).Contains(pos)) estadoActual = GameState.Opciones;
                break;

            case GameState.SeleccionPersonaje:
                Rectangle rectMale = new Rectangle(cx - 550, 120, 450, 550);
                Rectangle rectFemale = new Rectangle(cx + 100, 120, 450, 550);

                if (rectMale.Contains(pos) || (mcSeleccionado == mcMale))
                    alphaMale = MathHelper.Clamp(alphaMale + dt * VelocidadAnimacion, 0f, 1f);
                else
                    alphaMale = MathHelper.Clamp(alphaMale - dt * VelocidadAnimacion, 0f, 1f);

                if (rectFemale.Contains(pos) || (mcSeleccionado == mcFemale))
                    alphaFemale = MathHelper.Clamp(alphaFemale + dt * VelocidadAnimacion, 0f, 1f);
                else
                    alphaFemale = MathHelper.Clamp(alphaFemale - dt * VelocidadAnimacion, 0f, 1f);

                if (clic && rectMale.Contains(pos)) mcSeleccionado = mcMale;
                if (clic && rectFemale.Contains(pos)) mcSeleccionado = mcFemale;
                
                // Botón Confirmar (ajustado para que no coincida con el nombre)
                if (clic && nombreJugador.Length > 0 && mcSeleccionado != null && new Rectangle(cx - 100, 670, 200, 50).Contains(pos)) 
                    estadoActual = GameState.Lista;
                break;

            case GameState.Lista:
                for (int i = 0; i < pokedex.Count; i++)
                    if (clic && new Rectangle(900, 150 + (i * 45), 500, 35).Contains(pos)) { seleccionadoIndex = i; estadoActual = GameState.Pokedex; }
                if (clic && new Rectangle(VirtualWidth - 150, VirtualHeight - 60, 120, 40).Contains(pos)) estadoActual = GameState.Menu;
                break;

            case GameState.Pokedex:
                if (clic && new Rectangle(900, 550, 150, 40).Contains(pos)) estadoActual = GameState.Lista;
                break;

            case GameState.Opciones:
                if (clic && new Rectangle(cx - 100, cy, 200, 40).Contains(pos)) estadoActual = GameState.Anadir;
                if (clic && new Rectangle(cx - 50, cy + 100, 100, 40).Contains(pos)) estadoActual = GameState.Menu;
                break;

            case GameState.Anadir:
                if (clic && new Rectangle(cx - 150, cy - 150, 300, 30).Contains(pos)) campoActivo = 0;
                if (clic && new Rectangle(cx - 150, cy - 100, 300, 30).Contains(pos)) campoActivo = 1;
                if (clic && new Rectangle(cx - 150, cy - 50, 300, 30).Contains(pos)) campoActivo = 2;
                if (clic && new Rectangle(cx - 150, cy + 20, 200, 40).Contains(pos))
                {
                    Thread t = new Thread(() => {
                        using var d = new System.Windows.Forms.OpenFileDialog { Filter = "PNG|*.png" };
                        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) inputImagen = d.FileName;
                    });
                    t.SetApartmentState(ApartmentState.STA); t.Start();
                }
                if (clic && new Rectangle(cx - 110, cy + 150, 100, 40).Contains(pos) && File.Exists(inputImagen))
                {
                    string file = Path.GetFileName(inputImagen);
                    File.Copy(inputImagen, Path.Combine("src", "Pokedex", file), true);
                    File.WriteAllText(Path.Combine("src", "Pokedex", Path.GetFileNameWithoutExtension(file) + ".txt"), $"{inputNombre}\n{inputTipo}\n{inputDesc}");
                    CargarPokedex();
                    estadoActual = GameState.Opciones;
                }
                if (clic && new Rectangle(cx + 10, cy + 150, 100, 40).Contains(pos)) estadoActual = GameState.Opciones;
                break;
        }

        ratonAnterior = ratonActual;
        base.Update(gameTime);
    }

    private void CalculateScreenDestination()
    {
        float targetAspectRatio = (float)VirtualWidth / VirtualHeight;
        float windowAspectRatio = (float)Window.ClientBounds.Width / Window.ClientBounds.Height;
        if (windowAspectRatio > targetAspectRatio)
        {
            int width = (int)(Window.ClientBounds.Height * targetAspectRatio);
            _screenDestination = new Rectangle((Window.ClientBounds.Width - width) / 2, 0, width, Window.ClientBounds.Height);
        }
        else
        {
            int height = (int)(Window.ClientBounds.Width / targetAspectRatio);
            _screenDestination = new Rectangle(0, (Window.ClientBounds.Height - height) / 2, Window.ClientBounds.Width, height);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(new Color(25, 35, 40));
        
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        int cx = VirtualWidth / 2;
        int cy = VirtualHeight / 2;

        switch (estadoActual)
        {
            case GameState.Menu:
                _spriteBatch.DrawString(fuente, "COMENZAR", new Vector2(cx - 70, cy - 50), Color.White);
                _spriteBatch.DrawString(fuente, "OPCIONES", new Vector2(cx - 70, cy + 10), Color.White);
                break;

            case GameState.SeleccionPersonaje:
                _spriteBatch.DrawString(fuente, "ELIGE TU PERSONAJE", new Vector2(cx - 150, 50), Color.Yellow);
                
                Rectangle rM = new Rectangle(cx - 550, 120, 450, 550);
                Rectangle rF = new Rectangle(cx + 100, 120, 450, 550);

                _spriteBatch.Draw(mcMale, rM, Color.Lerp(Color.Black * 0.7f, Color.White, alphaMale));
                _spriteBatch.Draw(mcFemale, rF, Color.Lerp(Color.Black * 0.7f, Color.White, alphaFemale));
                
                _spriteBatch.DrawString(fuente, "NOMBRE: " + nombreJugador + "_", new Vector2(cx - 100, 640), Color.White);
                if (nombreJugador.Length > 0 && mcSeleccionado != null)
                    _spriteBatch.DrawString(fuente, "[ CONFIRMAR ]", new Vector2(cx - 100, 680), Color.GreenYellow);
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
                    _spriteBatch.DrawString(fuente, "SALIR", new Vector2(VirtualWidth - 150, VirtualHeight - 60), Color.Red);
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
                _spriteBatch.DrawString(fuente, "CONFIGURACION", new Vector2(cx - 100, cy - 100), Color.Yellow);
                _spriteBatch.DrawString(fuente, "AÑADIR POKEMON", new Vector2(cx - 100, cy), Color.White);
                _spriteBatch.DrawString(fuente, "VOLVER", new Vector2(cx - 50, cy + 100), Color.Red);
                break;

            case GameState.Anadir:
                _spriteBatch.DrawString(fuente, "NOMBRE: " + inputNombre + (campoActivo == 0 ? "_" : ""), new Vector2(cx - 150, cy - 150), campoActivo == 0 ? Color.Cyan : Color.White);
                _spriteBatch.DrawString(fuente, "TIPO: " + inputTipo + (campoActivo == 1 ? "_" : ""), new Vector2(cx - 150, cy - 100), campoActivo == 1 ? Color.Cyan : Color.White);
                _spriteBatch.DrawString(fuente, "DESC: " + inputDesc + (campoActivo == 2 ? "_" : ""), new Vector2(cx - 150, cy - 50), campoActivo == 2 ? Color.Cyan : Color.White);
                _spriteBatch.DrawString(fuente, "ADJUNTAR IMAGEN", new Vector2(cx - 150, cy + 20), Color.LightGreen);
                if (!string.IsNullOrEmpty(inputImagen)) _spriteBatch.DrawString(fuente, Path.GetFileName(inputImagen), new Vector2(cx + 80, cy + 20), Color.White);
                _spriteBatch.DrawString(fuente, "GUARDAR", new Vector2(cx - 110, cy + 150), Color.Green);
                _spriteBatch.DrawString(fuente, "CANCELAR", new Vector2(cx + 10, cy + 150), Color.Red);
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