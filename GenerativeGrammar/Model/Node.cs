using System.Text;

namespace GenerativeGrammar.Grammar;

public struct Node
{
    public string Name { get; set; }
    public List<string> Variables { get; set; }
    public List<int> Weights { get; set; }
    public List<string> PossibleNeighbours { get; set; }
    public List<string> ActualNeighbours { get; set; }
    public List<string> Conditions { get; set; }
    public string Source { get; set; }

    public Node()
    {
        Name = "";
        Variables = new List<string>();
        Weights = new List<int>();
        PossibleNeighbours = new List<string>();
        ActualNeighbours = new List<string>();
        Conditions = new List<string>();
        Source = "";
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(this.Name).Append(" [");
        foreach (var neighbour in PossibleNeighbours)
        {
            result.Append(neighbour).Append(" <> ");
        }

        return result.ToString();
    }
}