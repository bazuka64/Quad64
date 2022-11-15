﻿using ImGuiNET;
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
        ImFontPtr font;

        string[] romPaths;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            CenterWindow();
            IsVisible = true;
            imGuiController = new ImGuiController(this);
            ImGui.StyleColorsClassic();
            font = ImGui.GetIO().Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\ARIAL.TTF", 20);
            imGuiController.RecreateFontDeviceTexture();

            romPaths = Directory.GetFiles(System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "/roms/");
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(Color4.DarkBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // imgui
            imGuiController.Update(this, (float)args.Time);
            ImGui.PushFont(font);

            // Rom and Level
            if (resize)
            {
                ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(Size.X/ uiWidth, Size.Y));
            }
            ImGui.SetNextWindowCollapsed(false, ImGuiCond.Once);
            ImGui.Begin("Rom and Level");
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
                    ROM.Instance = new ROM(romPaths[i]);
                    currentRomIndex = i;
                    currentSeqIndex = -1;
                }
                if (selected)
                {
                     ImGui.PopStyleColor();
                     ImGui.PopStyleColor();
                     ImGui.PopStyleColor();
                }
            }
            ImGui.End();

            // Sequence
            if (resize)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(Size.X/ uiWidth, 0));
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
                    if (ImGui.Button($"{seq.id:D2} {seq.insts[0]} " + seq.name))
                    {
                        seq.Play();
                        currentSeqIndex = seq.id;
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