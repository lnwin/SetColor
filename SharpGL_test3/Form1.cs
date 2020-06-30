/*
 * SharpGL实例
 * 灰度成像-->山脉
 * 颜色分层
 * 作者：locus
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//fileread
using System.IO;

//sharpgl
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Assets;



namespace SharpGL_test3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // --- Fields ---
        #region Private Static Fields
        private static IntPtr hDC;                                              // Private GDI Device Context
        private static IntPtr hRC;                                              // Permanent Rendering Context
        private static Form form;                                               // Our Current Windows Form
        private static bool[] keys = new bool[256];                             // Array Used For The Keyboard Routine
        private static bool active = true;                                      // Window Active Flag, Set To True By Default
        private static bool fullscreen = true;                                  // Fullscreen Flag, Set To Fullscreen Mode By Default
        private static bool done = false;                                       // Bool Variable To Exit Main Loop

        private const int MAP_SIZE = 1024;                                      // Size Of Our .RAW Height Map (NEW)
        private const int STEP_SIZE = 16;                                       // Width And Height Of Each Quad (NEW)
        private const float HEIGHT_RATIO = 1.5f;                                // Ratio That The Y Is Scaled According To The X And Z (NEW)
        private static bool bRender = true;                                     // Polygon Flag Set To TRUE By Default (NEW)
        private static byte[] heightMap = new byte[MAP_SIZE * MAP_SIZE];        // Holds The Height Map Data (NEW)
        private static float scaleValue = 0.15f;                                // Scale Value For The Terrain (NEW)
        public float view_x = 0.0f;
        public float view_y = 0.0f;
        public float view_z = 0.0f;
        public float Mouse_x = 0.0f;
        public float Mouse_y = 0.0f;
        public float Tra_z = 0.0f;
        public float eye_z = 0.0f;
        private static bool Key;
        private float Color_min=0.5f;
        private float Color_max=0.5f;
        private int msg = 0;
        #endregion Private Static Fields

        private void openGLControl1_OpenGLDraw(object sender, SharpGL.RenderEventArgs args)
        {

         
            // 创建一个GL对象
            SharpGL.OpenGL gl = this.openGLControl1.OpenGL;

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            gl.LoadIdentity();

           // gl.LookAt(212, 60, 194 , 186, 55, 171, 0, 1, 0);
            gl.LookAt(200, 30, 160 + eye_z, 0, 0, 0, 0, 1, 0);//设置视角位置

            gl.Scale(scaleValue, scaleValue * HEIGHT_RATIO, scaleValue);
            RenderHeightMap(heightMap);
          
 
        }

        //get height
        private static int GetHeight(byte[] heightMap, int x, int y)
        {          // This Returns The Height From A Height Map Index
            x = x % MAP_SIZE;                                                   // Error Check Our x Value
            y = y % MAP_SIZE;                                                   // Error Check Our y Value

            return heightMap[x + (y * MAP_SIZE)];                               // Index Into Our Height Array And Return The Height
        }


        private void OpenGLControl1_OpenGLInitialized(object sender, EventArgs e)
        {
            OpenGL gl = this.openGLControl1.OpenGL;

            //启用阴影平滑(Smooth Shading)。

            gl.ShadeModel(OpenGL.GL_SMOOTH);

            gl.ClearColor(1.0f,1.0f,1.0f,0.5f);

            //设置深度缓冲

            gl.ClearDepth(1.0f);

            //启动深度测试

            gl.Enable(OpenGL.GL_DEPTH_TEST);

            //深度测试的类型

            gl.DepthFunc(OpenGL.GL_LEQUAL);

            //进行透视修正

            gl.Hint(OpenGL.GL_PERSPECTIVE_CORRECTION_HINT, OpenGL.GL_NICEST);

            LoadRawFile("Terrian.raw", MAP_SIZE * MAP_SIZE, ref heightMap);//灰度文件在Debug/Data文件夹下
         
        }

        private static bool LoadRawFile(string name, int size, ref byte[] heightMap)
        {
            if (name == null || name == string.Empty)
            {                          // Make Sure A Filename Was Given
                return false;                                                   // If Not Return false
            }

            string fileName1 = string.Format("Data{0}{1}",                      // Look For Data\Filename
                Path.DirectorySeparatorChar, name);
            string fileName2 = string.Format("{0}{1}{0}{1}Data{1}{2}",          // Look For ..\..\Data\Filename
                "..", Path.DirectorySeparatorChar, name);

            // Make Sure The File Exists In One Of The Usual Directories
            if (!File.Exists(name) && !File.Exists(fileName1) && !File.Exists(fileName2))
            {
                MessageBox.Show("Can't Find The Height Map!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;                                                   // File Could Not Be Found
            }

            if (File.Exists(fileName1))
            {                                        // Does The File Exist Here?
                name = fileName1;                                               // Set To Correct File Path
            }
            else if (File.Exists(fileName2))
            {                                   // Does The File Exist Here?
                name = fileName2;                                               // Set To Correct File Path
            }

            // Open The File In Read / Binary Mode
            using (FileStream fs = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BinaryReader r = new BinaryReader(fs);
                heightMap = r.ReadBytes(size);
            }
            return true;                                                        // Found And Loaded Data In File
        }


        private void RenderHeightMap(byte[] heightMap)
        {                 // This Renders The Height Map As Quads
            int X, Y;                                                           // Create Some Variables To Walk The Array With.
            int x, y, z;                                                        // Create Some Variables For Readability
            OpenGL gl = this.openGLControl1.OpenGL;

            if (heightMap == null)
            {                                             // Make Sure Our Height Data Is Valid
                MessageBox.Show("Wrong Data!");
            }

            gl.Rotate(view_x,0 ,0);
            gl.Rotate(0,view_y,0);
            gl.Translate(0, 0, view_z);

            if (bRender)
            {                                                       // What We Want To Render
                gl.Begin(OpenGL.GL_QUADS);                                        // Render Polygons
            }
            else
            {
                gl.Begin(OpenGL.GL_LINES);                                        // Render Lines Instead
            }

            for (X = 0; X < (MAP_SIZE - STEP_SIZE); X += STEP_SIZE)
            {
                for (Y = 0; Y < (MAP_SIZE - STEP_SIZE); Y += STEP_SIZE)
                {
                    // Get The (X, Y, Z) Value For The Bottom Left Vertex
                    x = X;
                    y = GetHeight(heightMap, X, Y);
                    z = Y;

                    SetVertexColor(heightMap, x, z);                            // Set The Color Value Of The Current Vertex
                    gl.Vertex(x, y, z);                                     // Send This Vertex To OpenGL To Be Rendered (Integer Points Are Faster)

                    // Get The (X, Y, Z) Value For The Top Left Vertex
                    x = X;
                    y = GetHeight(heightMap, X, Y + STEP_SIZE);
                    z = Y + STEP_SIZE;

                    SetVertexColor(heightMap, x, z);                            // Set The Color Value Of The Current Vertex
                    gl.Vertex(x, y, z);                                     // Send This Vertex To OpenGL To Be Rendered

                    // Get The (X, Y, Z) Value For The Top Right Vertex
                    x = X + STEP_SIZE;
                    y = GetHeight(heightMap, X + STEP_SIZE, Y + STEP_SIZE);
                    z = Y + STEP_SIZE;

                    SetVertexColor(heightMap, x, z);                            // Set The Color Value Of The Current Vertex
                    gl.Vertex(x, y, z);                                     // Send This Vertex To OpenGL To Be Rendered

                    // Get The (X, Y, Z) Value For The Bottom Right Vertex
                    x = X + STEP_SIZE;
                    y = GetHeight(heightMap, X + STEP_SIZE, Y);
                    z = Y;

                    SetVertexColor(heightMap, x, z);                            // Set The Color Value Of The Current Vertex
                    gl.Vertex(x, y, z);                                     // Send This Vertex To OpenGL To Be Rendered
                }
            }
            gl.End();
            gl.Color(1, 1, 1, 1);                                           // Reset The Color
            if(msg<1)
            msg += 1;
            else if(msg==1)
            {
                //    MessageBox.Show("Color MAX= "+ Color_max + "\n Color Min= "+ Color_min );                                                 // All Good
            }
      
            else if(msg>2)
            {
                msg = 2;
            }

        }


        private  void SetVertexColor(byte[] heightMap, int x, int y)
        {
            float temp1;
            float temp2;
            float rand_color;
            float rand_height;
            Random ran = new Random();
            rand_color = ran.Next(0,10)/15;//颜色随机变量
            rand_height = ran.Next(0, 10) / 10;//高度随机变量
            float fColor = (GetHeight(heightMap, x, y) / 256.0f);
           // Console.WriteLine(fColor);
            OpenGL gl = this.openGLControl1.OpenGL;
           
            //设置色彩分层规则
            if (0.0f < fColor && fColor < (0.35f+rand_height))   //蓝色部分
            { gl.Color(fColor / 10 + rand_color, fColor / 2 + rand_color, fColor ); }
            else if ((0.2f + rand_height) < fColor && fColor < (0.45f + rand_height))//绿色部分
            {
                gl.Color(fColor / 5 + rand_color, fColor + rand_color, fColor / 2 ); 
            }
            else if((0.45f + rand_height) < fColor && fColor < (0.55f + rand_height))//灰色部分
            {
                gl.Color(fColor * 0.9 , fColor *0.9 , fColor * 0.9 + rand_color); 
            }
            else    //白色部分
            {
                gl.Color(fColor * 1.1, fColor * 1.1, fColor * 1.1 + rand_color); 
            }
            

            // Assign This Blue Shade To The Current Vertex

            //测量灰度最值
            /*
            if(fColor<Color_min)
            {
                Color_min = fColor;
            }
            if(fColor>Color_max)
            {
                Color_max = fColor;
            }
            */
        }

        private void MouseDown(object sender, MouseEventArgs e)
        {
            Mouse_x = e.X;//读取光标瞬时坐标X值
            Mouse_y = e.Y;//读取光标瞬时坐标Y值
        }

        private void MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            //判断鼠标左键是否按下
            {
                
            }
        }


        private void openGLControl1_MouseWheel(object sender, MouseEventArgs e)
        {

            if (e.Delta > 0)
            {
                if (Key == true)
                {
                    eye_z -= 1.0f;
                }
                else
                { Tra_z -= 1.0f; }
            }

            else
            {
                if (Key == true)
                {
                    eye_z += 1.0f;

                }
                else
                { Tra_z += 1.0f; }
            }
            Key = false;
        }


        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //方法一：switch 
            /*
            switch (keyData)
            {
                case Keys.E:
                    {
                        Key = true;
                        return true;
                    }
                default: break;
            }
            return true;
            */

            //方法二：if
            if (keyData == Keys.E)
            //判断E键是否按下
            {
                Key = true;
                return true;
            }
            else if (keyData == Keys.F)
            //判断F键是否按下
            { }
            else if(keyData== Keys.NumPad7)   //向下
            {
                view_x += 0.1f;
            }
            else if (keyData == Keys.NumPad8) //向上
            {
                view_x -= 0.1f;
            }
            else if (keyData == Keys.NumPad4) //向右
            {
                view_y += 0.1f;
            }
            else if (keyData == Keys.NumPad5) //向左
            {
                view_y -= 0.1f;
            }
            else if (keyData == Keys.NumPad1) //近
            {
                view_z += 2f;
            }
            else if (keyData == Keys.NumPad2)//远
            {
                view_z -= 2f;
            }

            return base.ProcessCmdKey(ref msg, keyData);//返回所有键值


        }

    }
}
