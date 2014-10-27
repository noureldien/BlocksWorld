using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlocksWorldBuzzle
{
    public enum GameState
    {
        Stopped = 0,
        Started = 1,
        Paused = 2,
    }

    public enum SearchType
    {
        _1_DepthFirst = 1,
        _2_BreadthFirst = 2,
        _3_IterativeDeepeningDF = 3,
        _4_IterativeDeepeningBF = 4,
        _5_Heuristic = 5,
        _6_Dummy = 6,
    }
}
