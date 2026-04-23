using libsm64sharp;
using MMDTools;
using MP3Sharp;
using NAudio.Wave;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using Quad64.src;
using Scallion.DomainModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Quad64_new
{
    public partial class MainWindow : Window
    {
        public static MainWindow instance;

        public static Shader shader;
        public static Camera camera;

        ROM rom;
        Level level;

        string romDir = "";
        string settingsPath = "settings.ini";

        MediaPlayer mediaPlayer = new MediaPlayer();

        Sm64Manager sm64Manager;

        PMXMesh pmxMesh;
        Mp3FileReader reader;
        WaveOut waveOut;

        public static Stopwatch stopwatch = new Stopwatch();

        // FPSカメラモード（右クリックでON、左クリックでOFF）
        private bool fpsMode = false;
        private float fpsDeltaYaw = 0, fpsDeltaPitch = 0;
        private System.Windows.Point fpsLastPos;
        private bool fpsModeFirstFrame = false;

        // ROM一覧の全データ（検索フィルタ用）
        private List<RomEntry> allRomEntries = new List<RomEntry>();

        public MainWindow()
        {
            InitializeComponent();
            instance = this;

            var settings = new GLWpfControlSettings { MajorVersion = 3, MinorVersion = 3 };
            OpenTkControl.Start(settings);

            shader = new Shader("shaders/shader.vert", "shaders/shader.frag");
            camera = new Camera();

            mediaPlayer.MediaEnded += (s, e) =>
            {
                mediaPlayer.Position = TimeSpan.Zero;
                mediaPlayer.Play();
            };
            mediaPlayer.IsMuted = false;

            // 前回のROMディレクトリを復元
            if (File.Exists(settingsPath))
            {
                romDir = File.ReadAllText(settingsPath).Trim();
                LoadRomDirectory(romDir);
            }

            // libsm64
            if (File.Exists("rom/sm64.z64"))
            {
                Sm64Context.RegisterPlaySoundFunction(args => { });
                byte[] sm64Rom = File.ReadAllBytes("rom/sm64.z64");
                sm64Manager = new Sm64Manager();
                sm64Manager.sm64Context = Sm64Context.InitFromRom(sm64Rom);
                Sm64Audio.Start(sm64Manager.sm64Context);
            }

            // mmd
            if (File.Exists("Model/model.pmx"))
            {
                PMXObject pmx = PMXParser.Parse("Model/model.pmx");
                pmxMesh = new PMXMesh(pmx);
            }

            // vmd
            if (File.Exists("Motion/Motion.vmd"))
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Motion motion = new Motion();
                motion.Load("Motion/Motion.vmd");
                pmxMesh.motion = motion;
                for (int i = 0; i < motion.Bones.Count; i++)
                    motion.Bones[i].KeyFrames.Sort((f1, f2) => f1.KeyFrameIndex - f2.KeyFrameIndex);
            }

            if (File.Exists("Motion/motion.mp3"))
            {
                reader = new Mp3FileReader("Motion/motion.mp3");
                waveOut = new WaveOut();
                waveOut.Volume = 0.5f;
                waveOut.Init(reader);
            }
        }

        // =========================================================
        // ROM ディレクトリ読み込み
        // =========================================================

        private void LoadRomDirectory(string dir)
        {
            if (!Directory.Exists(dir)) return;

            var extensions = new[] { "*.z64", "*.n64", "*.v64" };
            var paths = extensions.SelectMany(ext => Directory.GetFiles(dir, ext)).ToList();

            allRomEntries = paths.Select(path => new RomEntry
            {
                FilePath = path,
                RomName = ReadRomName(path)
            }).ToList();

            romList.ItemsSource = allRomEntries;
            if (romSearchBox != null) romSearchBox.Text = string.Empty;
        }

        // ROMヘッダーのoffset 0x20から内部名を読む（GL不要、高速）
        private static string ReadRomName(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs.Length < 0x34) return string.Empty;
                fs.Seek(0x20, SeekOrigin.Begin);
                byte[] buf = new byte[20];
                fs.ReadExactly(buf, 0, 20);
                return Encoding.ASCII.GetString(buf).TrimEnd('\0', ' ');
            }
            catch { return string.Empty; }
        }

        // =========================================================
        // ROM リスト選択
        // =========================================================

        private void romList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (romList.SelectedItem is not RomEntry entry) return;

            try
            {
                rom = new ROM(entry.FilePath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ROM の読み込みに失敗しました:\n{ex.Message}", "Error");
                return;
            }

            // レベル一覧を生成
            var levels = ROM.levelIDs
                .Select(kv => new LevelEntry { Name = kv.Key, ID = kv.Value })
                .ToList();

            levelGrid.ItemsSource = levels;
            sequenceList.ItemsSource = rom.sequences;

            statusText.Text = $"{entry.FileName}  |  {levels.Count} levels";

            if (levels.Count > 0)
                levelGrid.SelectedIndex = 0;
        }

        // =========================================================
        // ROM 検索
        // =========================================================

        private void romSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = romSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                romList.ItemsSource = allRomEntries;
            }
            else
            {
                romList.ItemsSource = allRomEntries
                    .Where(r => r.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                             || r.FileName.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        // =========================================================
        // レベル カードクリック
        // =========================================================

        private void levelGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (levelGrid.SelectedItem is LevelEntry entry)
                LoadLevel(entry.ID);
        }

        private void LoadLevel(ushort levelID)
        {
            if (rom == null) return;

            try
            {
                level = LevelScript.parse(levelID);
            }
            catch (Exception ex)
            {
                statusText.Text = $"Level parse error: {ex.Message}";
                level = null;
                return;
            }

            camera = new Camera();

            // Areaラジオボタンのラベルと有効/無効を動的に設定
            for (int i = 0; i < 8; i++)
            {
                var radio = (System.Windows.Controls.RadioButton)FindName($"area{i}");
                if (level.areas[i] != null)
                {
                    radio.IsEnabled = true;
                    radio.Content = $"Area {i}";
                }
                else
                {
                    radio.IsEnabled = false;
                    radio.Content = $"Area {i}（なし）";
                }
                // 選択状態解除・色リセット
                radio.IsChecked = false;
                radio.Background = System.Windows.Media.Brushes.Transparent;
                radio.FontWeight = FontWeights.Normal;
            }

            // 有効な最初のエリアを探して選択＆強調
            for (int i = 0; i < 8; i++)
            {
                if (level.areas[i] != null)
                {
                    var radio = (System.Windows.Controls.RadioButton)FindName($"area{i}");
                    radio.IsChecked = true;
                    radio.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 74, 122)); // 青系
                    radio.FontWeight = FontWeights.Bold;
                    break;
                }
            }
        }

        // =========================================================
        // Renderループ
        // =========================================================

        private void OpenTkControl_Render(TimeSpan obj)
        {
            FrameTimer.update(obj);

            // サムネ生成は現在無効（resetSegmentが描画中レベルを破壊するため）
            // try { ProcessOneThumbnail(); } catch { thumbnailQueue.Clear(); }

            // --- メイン描画 ---
            GL.Viewport(0, 0, OpenTkControl.FrameBufferWidth, OpenTkControl.FrameBufferHeight);

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.ClearColor(0.15f, 0.15f, 0.2f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.PolygonMode(MaterialFace.FrontAndBack,
                (bool)wireFrame.IsChecked ? PolygonMode.Line : PolygonMode.Fill);

            shader.Use();
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            shader.SetInt("timer", FrameTimer.timer);

            if (level != null && level.hasArea && level.areas[level.curAreaID] != null)
            {
                try
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
                catch { level = null; }
            }

            // libsm64
            if (sm64Manager != null && sm64Manager.sm64Mario != null)
            {
                if (FrameTimer.animTimer == 0)
                    sm64Manager.sm64Mario.Tick();
                sm64Manager.Draw(camera);
                sm64Manager.sm64Mario.Gamepad.IsAButtonDown = Keyboard.IsKeyDown(Key.Enter);
            }

            // mmd
            if (level != null && level.hasArea && pmxMesh != null)
                pmxMesh.Draw(camera);


            // カメラ移動（fpsモード時のみ）
            if (fpsMode)
            {
                float speed = (float)cameraSpeed.Value;
                if (Keyboard.IsKeyDown(Key.W))         camera.position += camera.front * (float)obj.TotalMilliseconds * speed;
                if (Keyboard.IsKeyDown(Key.S))         camera.position -= camera.front * (float)obj.TotalMilliseconds * speed;
                if (Keyboard.IsKeyDown(Key.A))         camera.position -= camera.right * (float)obj.TotalMilliseconds * speed;
                if (Keyboard.IsKeyDown(Key.D))         camera.position += camera.right * (float)obj.TotalMilliseconds * speed;
                if (Keyboard.IsKeyDown(Key.Space))     camera.position += camera.up    * (float)obj.TotalMilliseconds * speed;
                if (Keyboard.IsKeyDown(Key.LeftShift)) camera.position -= camera.up    * (float)obj.TotalMilliseconds * speed;

                // MouseMoveで蓄積したデルタを適用してリセット
                camera.Yaw   += fpsDeltaYaw;
                camera.Pitch -= fpsDeltaPitch;
                fpsDeltaYaw = fpsDeltaPitch = 0;
            }
        }

        // =========================================================
        // 既存イベントハンドラ（変更なし）
        // =========================================================

        private void area_Checked(object sender, RoutedEventArgs e)
        {
            if (level == null) return;
            var button = sender as System.Windows.Controls.RadioButton;
            byte areaID = (byte)int.Parse(button.Name.Substring(4));
            if (level.areas[areaID] == null) return;

            level.curAreaID = areaID;
            camera = new Camera();

            // Areaラジオボタンの強調表示を更新
            for (int i = 0; i < 8; i++)
            {
                var radio = (System.Windows.Controls.RadioButton)FindName($"area{i}");
                if (i == areaID && level.areas[i] != null)
                {
                    radio.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 74, 122)); // 青系
                    radio.FontWeight = FontWeights.Bold;
                }
                else
                {
                    radio.Background = System.Windows.Media.Brushes.Transparent;
                    radio.FontWeight = FontWeights.Normal;
                }
            }

            try
            {
                sequenceList.SelectedIndex = 0;
                sequenceList.SelectedIndex = level.areas[level.curAreaID].seqID;
                objectList.ItemsSource = level.curArea.AllObjects.Where(obj => obj.modelID != 0);
            }
            catch { /* シーケンス/オブジェクト取得失敗は無視 */ }

            try { if (sm64Manager != null) sm64Manager.Init(level); } catch { }

            if (pmxMesh != null)
            {
                pmxMesh.worldPos = level.marioPos;
                pmxMesh.worldPos.Z -= 400;
            }

            if (reader != null)
            {
                reader.Position = 0;
                waveOut.Play();
                stopwatch.Restart();
            }
        }

        private void sequenceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sequenceList.SelectedItem != null && !(bool)muteButton.IsChecked)
                ((Sequence)sequenceList.SelectedItem).Play(mediaPlayer);
        }

        private void levelGrid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void sequenceList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllText(settingsPath, dialog.SelectedPath);
                romDir = dialog.SelectedPath;
                LoadRomDirectory(romDir);
            }
        }

        private void muteButton_Checked(object sender, RoutedEventArgs e) => mediaPlayer.Pause();
        private void muteButton_Unchecked(object sender, RoutedEventArgs e) => mediaPlayer.Play();

        private void OpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (OpenTkControl.FrameBufferWidth > 0 && OpenTkControl.FrameBufferHeight > 0)
                camera.aspect = OpenTkControl.FrameBufferWidth / (float)OpenTkControl.FrameBufferHeight;
        }

        private void OpenTkControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cameraSpeed.Value = Math.Clamp(cameraSpeed.Value + (e.Delta > 0 ? 1 : -1), 1, 50);
        }

        private void OpenTkControl_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (!fpsMode) return;
            ExitFpsMode();
        }

        private void OpenTkControl_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (fpsMode) return;
            fpsMode = true;
            fpsDeltaYaw = fpsDeltaPitch = 0;
            fpsLastPos = e.GetPosition(OpenTkControl);

            OpenTkControl.Cursor = System.Windows.Input.Cursors.None;

            modeText.Text = "左クリック: FPS モード終了  |  WASD: 移動  Space/Shift: 上下";
        }

        private void ExitFpsMode()
        {
            fpsMode = false;
            fpsDeltaYaw = fpsDeltaPitch = 0;
            OpenTkControl.Cursor = System.Windows.Input.Cursors.Arrow;
            modeText.Text = "右クリック: FPS モード";
        }

        private void OpenTkControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!fpsMode) return;
            var pos = e.GetPosition(OpenTkControl);
            double dx = pos.X - fpsLastPos.X;
            double dy = pos.Y - fpsLastPos.Y;

            // ワープ直後の大ジャンプは無視
            if (Math.Abs(dx) < OpenTkControl.ActualWidth / 2 &&
                Math.Abs(dy) < OpenTkControl.ActualHeight / 2)
            {
                fpsDeltaYaw   += (float)dx * 0.15f;
                fpsDeltaPitch += (float)dy * 0.15f;
            }

            // 端（50px以内）に近づいたら中央にワープ
            const double margin = 50;
            if (pos.X < margin || pos.X > OpenTkControl.ActualWidth  - margin ||
                pos.Y < margin || pos.Y > OpenTkControl.ActualHeight - margin)
            {
                var center = new System.Windows.Point(OpenTkControl.ActualWidth / 2, OpenTkControl.ActualHeight / 2);
                var sc = OpenTkControl.PointToScreen(center);
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)sc.X, (int)sc.Y);
                fpsLastPos = center;
            }
            else
            {
                fpsLastPos = pos;
            }
        }

        private void objectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (objectList.SelectedItem is Object3D obj && level != null)
            {
                level.curArea.selectedObject = obj;
                camera.position = new Vector3(obj.posX, obj.posY, obj.posZ);
                camera.position -= camera.front * 2000;
            }
        }
    }
}
