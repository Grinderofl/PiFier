using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace PiFier
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run(60.0);
            }
        }
    }

    public class Game : GameWindow
    {
        public Game() : base(1900, 1080, GraphicsMode.Default, "PI")
        {
            VSync = VSyncMode.On;
            
            WindowBorder = WindowBorder.Hidden;
            LoadResources();
            _brushes = new List<Brush>();
            foreach (var color in _colors)
            {
                _brushes.Add(new SolidBrush(color));
            }
        }

        private short[] _list;
        private int _columnCount = 100;
        private Bitmap _bitmap;
        private int _texture;
        
        private Color[] _colors = new Color[]
        {
            Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Orange, Color.Violet, Color.DeepSkyBlue,
            Color.LightGray, Color.Coral, Color.Turquoise
        };

        private List<Brush> _brushes;

        private int CreateTexture()
        {
            int textureId;
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (float)TextureEnvMode.Replace);//Important, or wrong color on some computers
            Bitmap bitmap = _bitmap;
            GL.GenTextures(1, out textureId);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
            GL.Finish();
            bitmap.UnlockBits(data);
            return textureId;
        }

        private void LoadResources()
        {
            var data = File.ReadAllText("pi.txt");
            _list = new short[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                _list[i] = (short)(data[i] - '0');
            }
        }

        private void SetColumns(int columns)
        {
            _columnCount = columns;
            int rows = _list.Length/columns;

            using (var gfx = Graphics.FromImage(_bitmap))
            {
                gfx.Clear(Color.Black);
                for (var row = 0; row < rows; row++)
                {
                    for (var column = 0; column < columns; column++)
                    {
                        //_rows[row, column] = _list[(row*columns) + column];
                        short num = _list[(row * columns) + column];
                        
                        // Check up
                        if(row > 0 && _list[((row* columns) - columns) + column] == num)
                            gfx.DrawLine(new Pen(_brushes[num], 5), column * 15 + 6, row * 15 + 6 + 15, column * 15 + 6, (row - 1) * 15 + 6 + 15);
                        // Check left
                        if(column > 0 && _list[(row * columns) + column - 1] == num)
                            gfx.DrawLine(new Pen(_brushes[num], 5), column * 15 + 6, row * 15 + 6 + 10, (column - 1) * 15 + 6, row * 15 + 6 + 10);
                        // Check up left
                        if(column > 0 && row > 0 && _list[(row * columns) - columns + column - 1] == num)
                            gfx.DrawLine(new Pen(_brushes[num], 5), column * 15 + 6, row * 15 + 6 + 10, (column - 1) * 15 + 6, (row - 1) * 15 + 6 + 10);
                        // Check down left
                        if(column > 0 && row < rows-1 && _list[(row * columns) + columns + column - 1] == num)
                            gfx.DrawLine(new Pen(_brushes[num], 5), column * 15 + 6, row * 15 + 6 + 10, (column - 1) * 15 + 6, (row + 1) * 15 + 6 + 10);


                        gfx.FillEllipse(_brushes[num], column * 15, row * 15 + 10, 12, 12);
                    }
                }
            }

            var data = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, _bitmap.Width, _bitmap.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            _bitmap.UnlockBits(data);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            WindowState = WindowState.Maximized;
            
            _bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            //_texture = GL.GenTexture();
            _texture = CreateTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _bitmap.Width, _bitmap.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            SetColumns(_columnCount);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            if(Keyboard[Key.Escape]) Exit();
            if(Keyboard[Key.KeypadPlus]) SetColumns(++_columnCount);
            if(Keyboard[Key.KeypadMinus]) SetColumns(--_columnCount);
                
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            Draw();
            SwapBuffers();
        }

        public void Draw()
        {
            GL.PushMatrix();
            GL.LoadIdentity();

            Matrix4 orthoProjection = Matrix4.CreateOrthographicOffCenter(0, ClientSize.Width, ClientSize.Height, 0, -1, 1);
            GL.MatrixMode(MatrixMode.Projection);

            GL.PushMatrix();//
            GL.LoadMatrix(ref orthoProjection);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.DstColor);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, _texture);


            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(0, 0);
            GL.TexCoord2(1, 0); GL.Vertex2(_bitmap.Width, 0);
            GL.TexCoord2(1, 1); GL.Vertex2(_bitmap.Width, _bitmap.Height);
            GL.TexCoord2(0, 1); GL.Vertex2(0, _bitmap.Height);
            GL.End();
            GL.PopMatrix();

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();

        }

        private void DrawCircle(double x, double y, int radius, Color color)
        {
            
            GL.Color3(color);
            for (int i = 0; i < 10; ++i)
            {
                var deg = 2*Math.PI*i/10;//i * Math.PI / 180;
                GL.Vertex2(Math.Cos(deg) * radius + x, Math.Sin(deg) * radius + y);
            }
            
        }
    }
}
