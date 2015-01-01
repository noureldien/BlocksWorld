using System;

namespace BlocksWorldBuzzle
{
    public class Node
    {
        public char[,] Data { get; set; }
        public Node Parent { get; set; }
        public Node[] Children { get; set; }
        public bool Visited { get; set; }

        private Node()
        {
            Visited = false;
        }
        
        public Node(char[,] data, Node parent)
            : this()
        {
            Data = data;
            Parent = parent;
        }

        public override string ToString()
        {
            return String.Format("'{0}' '{1}' '{2}' '{3}'", Data[0, 0], Data[0, 1], Data[1, 0], Data[1, 1]);
        }
    }
}
