using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WinDesktopAppOnCloud
{
    public class Point
    {

        public Point()
        {
            this.X = 0d;
            this.Y = 0d;
        }

        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
