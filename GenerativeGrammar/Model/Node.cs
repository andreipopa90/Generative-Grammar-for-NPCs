using System.Text;

namespace GenerativeGrammar.Model;

public class Node
{
    public string Name { get; set; }
    public List<string> PossibleNeighbours { get; set; }
    public List<string> ActualNeighbours { get; set; }
    public List<string> Conditions { get; set; }
    public string Source { get; set; }
    public bool IsLeafNode { get; set; }

    public Node()
    {
        Name = string.Empty;
        PossibleNeighbours = new List<string>();
        ActualNeighbours = new List<string>();
        Conditions = new List<string>();
        Source = string.Empty;
        IsLeafNode = false;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(Name);
        return result.ToString();
    }
}