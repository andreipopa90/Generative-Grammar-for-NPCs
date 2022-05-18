namespace GenerativeGrammar.Grammar;

public struct Node
{
    public string Name { get; set; }
    public List<string> Variables { get; set; }
    public List<int> Weights { get; set; }
    public List<string> PossibleNeighbours { get; set; }
    public List<string> ActualNeighbours { get; set; }
    public List<string> Conditions { get; set; }
    public List<string> Source { get; set; }
    public List<string> GlobalVariables { get; set; }
}