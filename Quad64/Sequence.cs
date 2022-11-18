using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
                        seqTable   instsTable nameTable
ASA                     0x03E00000 0x007F0000 0x007F1000
SS3 SotSC               0x03E00000 0x007F0000 0x007F1000
SM64 LAST IMPACT        0x03E00000 0x007F0000 0x007F1000
Star Revenge 3.5        0x02F00000 0x011FF000 0x011FE000
  　                   (0x03F00000)要チェック
Star Revenge 6.25 LA    0x03E00000 0x007F0000 0x007F1000
STAR REVENGE 8 SOH      0x03E00000 0x007F0000 0x007F1000
SUPER MARIO NEW STAR    0x03E00000 0x007F0000 0x007F1000
SM Treasure World       0x03E00000 0x007F0000 0x007F1000
TW DREAM EDITION        0x03E00000 0x007F0000 0x007F1000
SECRET BOOK 64          0x01210000 0x007F0000 0x007F1000
SUPER MARIO 64 LAND     0x01210000 0x007F0000 0x007F1000
through the age         0x0327CD70 0x03297830 not exist?
SM64 THE GREEN STARS    0x02F00000 0x007F0000 0x007F1000
SUPER MARIO 64 (74)     0x02F00000 0x007F0000 0x007F1000
SM64 Star Road          0x02F00000 0x007F0000 0x007F1000
SUPER MARIO 64          0x007B0860 0x007CC620 not exist?
*/

namespace Quad64
{
    class RomAddress
    {
        public uint seqTable;
        public uint instsTable;
        public uint nameTable;
    }

    class Sequence
    {
        public int id;
        public string name;
        public byte[] data;
        public List<byte> insts = new List<byte>();
        public bool defaultSeq;

        public Sequence(int id)
        {
            this.id = id;
        }

        public void Play()
        {
            // stop music

            // if No music then return
            if (id == 0) return;

            // write m64 out
            File.WriteAllBytes($"../../../../midi/{ROM.Instance.romName}/{id} {insts[0]} {name}.m64", data);

            // convert music
            OutputMIDI midi = new OutputMIDI();
            MidiFile midiFile = midi.ConvertToMIDI(this);

            // write midi out
            string fileName = $"../../../../midi/{ROM.Instance.romName}/{id} {insts[0]} {name}.mid";
            midiFile.Write(fileName, true);

            // play music
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

        // Set original Names
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
