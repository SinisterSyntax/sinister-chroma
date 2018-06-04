using Colourful;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
