using libsm64sharp;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Quad64.src
{
    internal static class Sm64Audio
    {

        public static void Start(ISm64Context sm64Context)
        {
            Task.Run(() =>
            {
                var stopwatch = new Stopwatch();

                var device = ALC.OpenDevice(null);
                var context = ALC.CreateContext(device, (int[])null);
                ALC.MakeContextCurrent(context);

                int source = AL.GenSource();
                int[] buffers = AL.GenBuffers(2);

                AL.SourceQueueBuffers(source, buffers);
                AL.SourcePlay(source);

                short[] audioBuffer = new short[2 * 2 * 544];
                
                while (true)
                {
                    stopwatch.Restart();

                    int processed = 0;
                    AL.GetSource(source, ALGetSourcei.BuffersProcessed, out processed);

                    while (processed-- != 0)
                    {
                        int buffer = AL.SourceUnqueueBuffer(source);

                        // read samples from libsm64
                        var numSamples = sm64Context.TickAudio(0, 1100, audioBuffer);

                        AL.BufferData(buffer, ALFormat.Stereo16, audioBuffer, 32000);

                        AL.SourceQueueBuffer(source, buffer);
                        AL.GetSource(source, ALGetSourcei.SourceState, out int state);
                        if((ALSourceState)state != ALSourceState.Playing)
                            AL.SourcePlay(source);
                    }

                    var targetSeconds = 1.0 / 30;
                    var targetTicks = targetSeconds * Stopwatch.Frequency;
                    // Expensive, but more accurate than Thread.sleep
                    var i = 0;
                    while (stopwatch.ElapsedTicks < targetTicks)
                    {
                        ++i;
                    }
                }
            });
        }
    }
}