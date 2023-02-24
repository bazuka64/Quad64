
using System;
using System.Diagnostics;

namespace Quad64.src
{
    internal class FrameTimer
    {
        public static int timer;

        public static int animTimer;
        public static int vmdTimer;
        public static int changeAnimTimer;
        public static double switchTimer;
        public static double coinTimer;

        public static int animFrame;
        public static int vmdFrame;
        public static int changeAnimFrame;
        public static int switchFrame;
        public static int coinFrame;

        static int animFPS = 30;
        static int vmdFPS = 30;
        static float changeAnimFPS = 0.2f;
        public static double switchFPS = 1;
        public static double coinFPS = 20;


        public static void update(TimeSpan obj)
        {
            

            timer += obj.Milliseconds;

            animTimer += obj.Milliseconds;
            if (animTimer > 1000 / (float)animFPS)
            {
                animFrame++;
                animTimer = 0;
            }

            vmdFrame = (int)(MainWindow.stopwatch.ElapsedMilliseconds / 1000f * vmdFPS);
            //Console.WriteLine(vmdFrame);
            //vmdTimer += obj.Milliseconds;
            //if (vmdTimer > 1000 / (float)vmdFPS)
            //{
            //    vmdFrame++;
            //    vmdTimer = 0;
            //}

            changeAnimTimer += obj.Milliseconds;
            if (changeAnimTimer > 1000 / changeAnimFPS)
            {
                changeAnimFrame++;
                changeAnimTimer = 0;
            }

            switchTimer += obj.Milliseconds;
            if (switchTimer > 1000 / (float)switchFPS)
            {
                switchFrame++;
                switchTimer = 0;
            }

            coinTimer += obj.Milliseconds;
            if (coinTimer > 1000 / (float)coinFPS)
            {
                coinFrame++;
                coinTimer = 0;
            }
        }
    }
}