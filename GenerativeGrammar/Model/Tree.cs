using System.Collections.Generic;

namespace GenerativeGrammar.Model
{
    public class Tree
    {
        public Node Root { get; set; }
        public List<Node> Nodes { get; }
        public Dictionary<string, int> Attributes { get; }
        public List<string> Parameters { get; }

        public Tree()
        {
            Root = new Node();
            Nodes = new List<Node>();
            Attributes = new Dictionary<string, int>();
            Parameters = new List<string>();
        }
    }
}