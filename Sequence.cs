using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System;
using System.Windows.Controls;

namespace Quad64
{
    internal class Sequence
    {
        public int id;
        public string name { get; set; }
        public byte[] data;
        public List<byte> insts = new List<byte>();
        public bool defaultSeq;

        OutputDevice outputDevice;
        Playback playback;

        public Sequence(int id)
        {
            this.id = id;
        }

        public void Play(MediaPlayer mediaPlayer)
        {
            // if No music then return
            if (id == 0) return;

            // output m64
            File.WriteAllBytes($"../../../midi/{insts[0]} {name}.m64", data);

            // convert m64 to midi
            OutputMIDI outputMIDI = new OutputMIDI();
            Melanchall.DryWetMidi.Core.MidiFile midiFile = outputMIDI.ConvertToMIDI(this);

            // output midi
            string filename = $"../../../midi/{insts[0]} {name}.midi";
            midiFile.Write(filename, false);

            // play midi
            //mediaElement.LoadedBehavior = MediaState.Manual;
            //mediaElement.Source = new Uri(new FileInfo(filename).FullName);
            mediaPlayer.Open(new Uri(new FileInfo(filename).FullName));
            mediaPlayer.Play();
        }

        public class RomAddress
        {
            public uint seqTable;
            public uint instsTable;
            public uint nameTable;
        }

        public static Dictionary<string, RomAddress> addresses = new Dictionary<string, RomAddress>()
        {
            {"ASA"                 , new RomAddress(){seqTable=0x03E00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SS3 SotSC"           , new RomAddress(){seqTable=0x03E00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SM64 LAST IMPACT"    , new RomAddress(){seqTable=0x03E00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"Star Revenge 3.5"    , new RomAddress(){seqTable=0x02F00000, instsTable=0x011FF000, nameTable=0x011FE000}},
            {"Star Revenge 6.25 LA", new RomAddress(){seqTable=0x03E00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"STAR REVENGE 8 SOH"  , new RomAddress(){seqTable=0x03E00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SUPER MARIO NEW STAR", new RomAddress(){seqTable=0x03E00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SM Treasure World"   , new RomAddress(){seqTable=0x03E00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"TW DREAM EDITION"    , new RomAddress(){seqTable=0x03E00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SECRET BOOK 64"      , new RomAddress(){seqTable=0x01210000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SUPER MARIO 64 LAND" , new RomAddress(){seqTable=0x01210000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"THROUGH THE AGES"    , new RomAddress(){seqTable=0x0327CD70, instsTable=0x03297830, nameTable=0x00000000}},
            {"SM64 THE GREEN STARS", new RomAddress(){seqTable=0x02F00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SUPER MARIO 74"      , new RomAddress(){seqTable=0x02F00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SM64 Star Road"      , new RomAddress(){seqTable=0x02F00000, instsTable=0x007F0000, nameTable=0x007F1000}},
            {"SUPER MARIO 64"      , new RomAddress(){seqTable=0x007B0860, instsTable=0x007CC620, nameTable=0x00000000}},
        };

        // original sequence Names
        public static string[] tNames =  {
                "No Music",
                "Star Catch",
                "Title Screen",
                "Bob-Omb Battlefield",
                "Inside Castle",
                "Dire, Dire Docks",
                "Lethal Lava Land",
                "Bowser Battle",
                "Snow",
                "Slide",
                "Haunted House",
                "Piranha Plant Lullaby",
                "Hazy Maze Cave",
                "Star Select",
                "Wing Cap",
                "Metal Cap",
                "Bowser Message",
                "Bowser Course",
                "High Score",
                "Merry-Go-Round",
                "Start and End Race with Koopa the Quick",
                "Star Appears",
                "Boss Fight",
                "Take a Key",
                "Endless Stairs",
                "Final Boss",
                "Staff Credits",
                "Puzzle Solved",
                "Toad Message",
                "Peach Message",
                "Introduction Scene",
                "Last Star Fanfare",
                "Ending Scene",
                "File Select",
                "Lakitu Appears"
        };
    }
}