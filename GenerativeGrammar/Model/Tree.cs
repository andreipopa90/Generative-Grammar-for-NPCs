using System.Text;
using GenerativeGrammar.Model;

namespace GenerativeGrammar.Grammar;

public struct Tree
{
    public string Name { get; set; }
    public List<Node> Nodes { get; set; }
    public Dictionary<string, int> GlobalVariables { get; set; }

    public Tree()
    {
        Name = "";
        Nodes = new List<Node>();
        GlobalVariables = new Dictionary<string, int>();
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        foreach (var node in Nodes)
        {
            result.Append(node.Name).Append('[');
            foreach (var n in node.PossibleNeighbours)
            {
                result.Append(n).Append(", ");
            }
            result = result.Remove(result.Length - 2, 2);
            result.Append("] A[");
            if (node.ActualNeighbours.Count > 0)
            {
                foreach (var n in node.ActualNeighbours)
                {
                    result.Append(n.Trim()).Append(", ");
                }
            }
            result = result.Remove(result.Length - 2, 2);
            result.Append("]\n");
        }

        return result.ToString();
    }
}