using System.Text;

namespace GenerativeGrammar.Model;

public class Tree
{
    public Node Root { get; set; }
    public List<Node> Nodes { get; set; }
    public Dictionary<string, int> GlobalVariables { get; set; }
    public List<string> Parameters { get; set; }

    public Tree()
    {
        Root = new Node();
        Nodes = new List<Node>();
        GlobalVariables = new Dictionary<string, int>();
        Parameters = new List<string>();
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