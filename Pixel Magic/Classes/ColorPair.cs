using Colourful;
using System.Drawing;

namespace Pixel_Magic.Classes
{
    class ColorPair
    {
        public Color Color { get; set; }
        public LabColor LAB { get; set; }

        public ColorPair(Color color, LabColor lAB)
        {
            Color = color;
            LAB = lAB;
        }
    }
}
