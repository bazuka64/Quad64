using System;

namespace Quad64
{
    public class Object3D
    {
        public byte act;
        public byte modelID { get; set; }
        public string s_modelID { get => "0x" + modelID.ToString("X2"); }
        public short posX, posY, posZ;
        public short rotX, rotY, rotZ; // degree
        public uint behParam, behAddr;

        public uint animsAddr;
    }
}