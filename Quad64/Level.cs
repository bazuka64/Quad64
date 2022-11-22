namespace Quad64
{
    internal class Level
    {
        public int levelID;
        public Area[] areas = new Area[8];

        public int curAreaID = -1;

        public Level(int levelID)
        {
            this.levelID = levelID;
        }
    }
}