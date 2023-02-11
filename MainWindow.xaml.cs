using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using Quad64.src;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Quad64
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow instance;

        public static Shader shader;
        public static Camera camera;
        Point lastPos;

        ROM rom;
        Level level;

        string romDir = "";
        string settingsPath = "../../../settings.ini";

        MediaPlayer mediaPlayer = new MediaPlayer();

        public MainWindow()
        {
            InitializeComponent();

            instance = this;

            var settings = new GLWpfControlSettings()
            {
                MajorVersion = 3,
                MinorVersion = 3
            };
            OpenTkControl.Start(settings);

            shader = new Shader("../../../shaders/shader.vert", "../../../shaders/shader.frag");
            camera = new Camera();
            mediaPlayer.MediaEnded += (s, e) =>
            {
                mediaPlayer.Position = TimeSpan.Zero;
                mediaPlayer.Play();
            };
            mediaPlayer.IsMuted = true;

            if (File.Exists(settingsPath))
            {
                romDir = File.ReadAllText(settingsPath);
                string[] romPaths = Directory.GetFiles(romDir);
                romList.ItemsSource = romPaths.Select(romPath => System.IO.Path.GetFileName(romPath));
            }

        }

        private void OpenTkControl_Render(TimeSpan obj)
        {
            FrameTimer.update(obj);

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.ClearColor(0.2f, 0.2f, 0.2f, 0.2f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if ((bool)wireFrame.IsChecked)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            else
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            shader.Use();
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());

            shader.SetInt("timer", FrameTimer.timer);

            // draw
            if (level != null && level.hasArea)
            {
                level.layers[1] = (bool)layer1.IsChecked;
                level.layers[2] = (bool)layer2.IsChecked;
                level.layers[3] = (bool)layer3.IsChecked;
                level.layers[4] = (bool)layer4.IsChecked;
                level.layers[5] = (bool)layer5.IsChecked;
                level.layers[6] = (bool)layer6.IsChecked;

                if ((bool)allModels.IsChecked)
                    level.drawModels();
                else
                    level.areas[level.curAreaID].draw();
            }

            float cameraSpeed = (float)this.cameraSpeed.Value;
            if (Keyboard.IsKeyDown(Key.W))
                camera.position += camera.front * obj.Milliseconds * cameraSpeed;
            if (Keyboard.IsKeyDown(Key.S))
                camera.position -= camera.front * obj.Milliseconds * cameraSpeed;
            if (Keyboard.IsKeyDown(Key.A))
                camera.position -= camera.right * obj.Milliseconds * cameraSpeed;
            if (Keyboard.IsKeyDown(Key.D))
                camera.position += camera.right * obj.Milliseconds * cameraSpeed;
            if (Keyboard.IsKeyDown(Key.Space))
                camera.position += camera.up * obj.Milliseconds * cameraSpeed;
            if (Keyboard.IsKeyDown(Key.LeftShift))
                camera.position -= camera.up * obj.Milliseconds * cameraSpeed;

            float sensitivity = 0.1f;
            Point curPos = Mouse.GetPosition(OpenTkControl);
            
            if(Mouse.LeftButton == MouseButtonState.Pressed && OpenTkControl == Mouse.DirectlyOver)
            {
                Vector delta = curPos - lastPos;
                camera.Yaw += (float)delta.X * sensitivity;
                camera.Pitch -= (float)delta.Y * sensitivity;
            }
            lastPos = curPos;
        }

        private void OpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            camera.aspect = OpenTkControl.FrameBufferWidth / (float)OpenTkControl.FrameBufferHeight;
        }

        private void romList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            rom = new ROM(System.IO.Path.Combine(romDir, (string)romList.SelectedItem));

            if(levelList.Items.Count == 0)
            {
                levelList.ItemsSource = ROM.levelIDs;
                levelList.DisplayMemberPath = "Key";
                levelList.SelectedValuePath = "Value";
            }
            levelList.SelectedIndex = -1;
            
            sequenceList.ItemsSource = rom.sequences;
            sequenceList.DisplayMemberPath = "name";

            //levelList.SelectedIndex = 0;
        }

        private void levelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(levelList.SelectedIndex != -1)
            {
                level = LevelScript.parse((ushort)levelList.SelectedValue);
                camera = new Camera();

                for(int i = 0; i < 8; i++)
                {
                    ((System.Windows.Controls.RadioButton)FindName("area" + i)).IsEnabled = level.areas[i] != null;
                }
                area1.IsChecked = false;
                area1.IsChecked = true;
            }
        }

        private void area_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.RadioButton;
            level.curAreaID = (byte)int.Parse(button.Name.Substring(4));
            camera = new Camera();
            sequenceList.SelectedIndex = 0;
            sequenceList.SelectedIndex = level.areas[level.curAreaID].seqID;

            // object list
            objectList.ItemsSource = level.curArea.AllObjects.Where(obj => obj.modelID != 0);
            objectList.DisplayMemberPath = "s_modelID";
        }

        private void sequenceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sequenceList.SelectedItem !=null && !(bool)muteButton.IsChecked)
                ((Sequence)sequenceList.SelectedItem).Play(mediaPlayer);
        }

        private void sequenceList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                File.WriteAllText(settingsPath, dialog.SelectedPath);
                romDir = dialog.SelectedPath;
                string[] romPaths = Directory.GetFiles(romDir);
                romList.ItemsSource = romPaths.Select(romPath => System.IO.Path.GetFileName(romPath));
            }
        }

        private void muteButton_Checked(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
        }

        private void muteButton_Unchecked(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Play();
        }

        private void OpenTkControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cameraSpeed.Value += e.Delta > 0 ? 1 : -1;
        }

        private void objectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(objectList.SelectedItem != null)
            {
                level.curArea.selectedObject = (Object3D)objectList.SelectedItem;
                Object3D obj = level.curArea.selectedObject;
                camera.position = new Vector3(obj.posX, obj.posY, obj.posZ);
                camera.position -= camera.front * 2000;

            }
        }
    }
}
