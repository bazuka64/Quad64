using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Quad64_new;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace Quad64.src
{
    // GLコンテキストが有効なスレッド（OpenTkControl_Render内）からのみ呼ぶこと
    internal static class ThumbnailRenderer
    {
        private const int ThumbW = 200;
        private const int ThumbH = 150;

        public static BitmapSource Render(Level level, byte areaID = 1)
        {
            var area = level.areas[areaID];
            if (area == null) return null;

            // 呼び出し元のFBO・ビューポートを退避
            GL.GetInteger(GetPName.FramebufferBinding, out int savedFbo);
            int[] savedViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, savedViewport);

            // --- FBO セットアップ ---
            int fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                ThumbW, ThumbH, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, tex, 0);

            int rbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                RenderbufferStorage.Depth24Stencil8, ThumbW, ThumbH);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Cleanup(fbo, tex, rbo);
                return null;
            }

            // --- 描画 ---
            GL.Viewport(0, 0, ThumbW, ThumbH);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0.15f, 0.15f, 0.2f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var thumbCam = new Camera();
            thumbCam.aspect = (float)ThumbW / ThumbH;

            // レベルのマリオスポーン位置を中心に俯瞰カメラを配置
            Vector3 center = level.marioPos;
            thumbCam.position = center + new Vector3(0, 7000, 5000);
            thumbCam.Pitch = -50f;
            thumbCam.Yaw = -90f;

            MainWindow.shader.Use();
            MainWindow.shader.SetMatrix4("view", thumbCam.GetViewMatrix());
            MainWindow.shader.SetMatrix4("projection", thumbCam.GetProjectionMatrix());
            MainWindow.shader.SetInt("timer", 0);

            // ジオメトリのみ描画（objectFlag等のUIフラグは参照しない）
            area.model.draw(Matrix4.Identity, null);

            // --- ピクセル読み出し ---
            byte[] pixels = new byte[ThumbW * ThumbH * 4];
            GL.ReadPixels(0, 0, ThumbW, ThumbH, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

            // OpenGLは下から上のためY反転
            FlipY(pixels, ThumbW, ThumbH);

            // RGBA → BGRA 変換（WPF Bgr32/Pbgra32用）
            byte[] bgra = RgbaToBgra(pixels);

            // --- BitmapSource 生成 ---
            var bmp = BitmapSource.Create(ThumbW, ThumbH, 96, 96,
                PixelFormats.Bgra32, null, bgra, ThumbW * 4);
            bmp.Freeze();

            // --- クリーンアップ・FBO/ビューポート復元 ---
            Cleanup(fbo, tex, rbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, savedFbo);
            GL.Viewport(savedViewport[0], savedViewport[1], savedViewport[2], savedViewport[3]);

            return bmp;
        }

        private static void FlipY(byte[] pixels, int width, int height)
        {
            int stride = width * 4;
            byte[] row = new byte[stride];
            for (int y = 0; y < height / 2; y++)
            {
                int top = y * stride;
                int bottom = (height - 1 - y) * stride;
                System.Buffer.BlockCopy(pixels, top, row, 0, stride);
                System.Buffer.BlockCopy(pixels, bottom, pixels, top, stride);
                System.Buffer.BlockCopy(row, 0, pixels, bottom, stride);
            }
        }

        private static byte[] RgbaToBgra(byte[] rgba)
        {
            byte[] bgra = new byte[rgba.Length];
            for (int i = 0; i < rgba.Length; i += 4)
            {
                bgra[i + 0] = rgba[i + 2]; // B
                bgra[i + 1] = rgba[i + 1]; // G
                bgra[i + 2] = rgba[i + 0]; // R
                bgra[i + 3] = rgba[i + 3]; // A
            }
            return bgra;
        }

        private static void Cleanup(int fbo, int tex, int rbo)
        {
            GL.DeleteFramebuffer(fbo);
            GL.DeleteTexture(tex);
            GL.DeleteRenderbuffer(rbo);
        }
    }
}
