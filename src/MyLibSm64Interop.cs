using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Quad64.src
{
    internal static class MyLibSm64Interop
    {
        private const string SM64_DLL = "lib\\sm64-x86.dll";

        // can not call
        [DllImport(SM64_DLL)]
        public static extern void sm64_play_music(
            byte player, 
            ushort seqArgs, 
            ushort fadeTimer);
    }
}
