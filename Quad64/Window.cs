using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.ImGui;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Quad64
{
    internal class Window : GameWindow
    {
        ImGuiController imGuiController;
        bool resize = true;
        const int uiWidth = 4;
        int currentRomIndex = -1;
        int currentSeqIndex = -1;
        int currentLevelIndex = -1;
        ImFontPtr font;

        string[] romPaths;
        Level level;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            // window init
            CenterWindow();
            IsVisible = true;

            // imgui init
            imGuiController = new ImGuiController(this);
            ImGui.StyleColorsClassic();
            font = ImGui.GetIO().Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\ARIAL.TTF", 20);
            imGuiController.RecreateFontDeviceTexture();

            // rom list init
            romPaths = Directory.GetFiles(System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "/roms/");
        }

        void loadLevel(int levelID)
        {
            Level testLevel = new Level(levelID);
            LevelScripts.parse(testLevel, 0x15, 0);
            if (testLevel.areas.Length != 0)
                level = testLevel;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(Color4.DarkBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // imgui begin
            imGuiController.Update(this, (float)args.Time);
            ImGui.PushFont(font);

            // imgui Rom list
            if (resize)
            {
                ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(Size.X/ uiWidth, Size.Y/2));
            }
            ImGui.SetNextWindowCollapsed(false, ImGuiCond.Once);
            ImGui.Begin("Rom");
            for (int i = 0; i < romPaths.Length;i++)
            {
                bool selected = currentRomIndex == i;
                if(selected)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, 0x8000FF00);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x8000FF00);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x8000FF00);
                }
                if (ImGui.Button(Path.GetFileNameWithoutExtension(romPaths[i])))
                {
                    // rom button pushed
                    ROM.Instance = new ROM(romPaths[i]);
                    currentRomIndex = i;
                    currentSeqIndex = -1;
                    currentLevelIndex = -1;
                }
                if (selected)
                {
                     ImGui.PopStyleColor();
                     ImGui.PopStyleColor();
                     ImGui.PopStyleColor();
                }
            }
            ImGui.End();

            // imgui Level list
            if (resize)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, Size.Y/2));
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(Size.X / uiWidth, Size.Y / 2));
            }
            ImGui.SetNextWindowCollapsed(false, ImGuiCond.Once);
            ImGui.Begin("Level");
            if(ROM.Instance != null)
            {
                int i = 0;
                foreach(var levelID in ROM.levelIDs)
                {
                    bool selected = currentLevelIndex == i;
                    if (selected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, 0x8000FF00);
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x8000FF00);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x8000FF00);
                    }
                    if (ImGui.Button(levelID.Key))
                    {
                        loadLevel(levelID.Value);
                        currentLevelIndex = i;
                    }
                    if (selected)
                    {
                        ImGui.PopStyleColor();
                        ImGui.PopStyleColor();
                        ImGui.PopStyleColor();
                    }
                    i++;
                }
            }
            ImGui.End();

            // imgui Sequence list
            if (resize)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X - Size.X/ uiWidth, 0));
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(Size.X / uiWidth, Size.Y));
            }
            ImGui.SetNextWindowCollapsed(false, ImGuiCond.Once);
            ImGui.Begin("Sequence");
            if(ROM.Instance != null)
            {
                foreach (var seq in ROM.Instance.sequences)
                {
                    bool selected = currentSeqIndex == seq.id;
                    if (selected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Button, 0x8000FF00);
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x8000FF00);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x8000FF00);
                    }
                    if (seq.defaultSeq)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, 0xff00ffff);
                    }
                    if (ImGui.Button($"{seq.id:D2} {seq.insts[0]} " + seq.name))
                    {
                        // sequence button pushed
                        seq.Play();
                        currentSeqIndex = seq.id;
                    }
                    if (seq.defaultSeq)
                    {
                        ImGui.PopStyleColor();
                    }
                    if (selected)
                    {
                        ImGui.PopStyleColor();
                        ImGui.PopStyleColor();
                        ImGui.PopStyleColor();
                    }
                }
            }
            ImGui.End();

            // imgui end
            resize = false;
            ImGui.PopFont();
            imGuiController.Render();

            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);

            imGuiController.WindowResized(e.Width, e.Height);
            resize = true;
        }
    }
}