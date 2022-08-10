using System.Text;

namespace GenerativeGrammar.Model;

public class Tree
{
    public Node Root { get; set; }
    public List<Node> Nodes { get; }
    public Dictionary<string, int> GlobalVariables { get; }
    public List<string> Parameters { get; }

    public Tree()
    {
        Root = new Node();
        Nodes = new List<Node>();
        GlobalVariables = new Dictionary<string, int>();
        Parameters = new List<string>();
    }
}