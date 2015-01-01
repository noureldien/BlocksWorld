using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlocksWorld
{
    public struct Index
    {
        public int Y { get; set; }

        public int X { get; set; }

        public Index(int y, int x)
            : this()
        {
            Y = y;
            X = x;
        }
    }
}
