using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace Quad64
{
    class Program
    {
        static void Main()
        {
            NativeWindowSettings nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(1920, 1080),
                Title = "Quad64",
                StartVisible = false,
            };

            Window window = new Window(GameWindowSettings.Default, nativeWindowSettings);
            window.Run();
        }
    }
}