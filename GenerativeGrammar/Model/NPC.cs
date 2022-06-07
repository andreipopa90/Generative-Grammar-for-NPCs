namespace GenerativeGrammar.Model;

public class Npc
{
    public BaseStat BaseStats { get; set; }
    public Dictionary<string, int> EVS { get; set; }
    public List<string> Types { get; set; }
    public Nature Nature { get; set; }
    public List<Move> Moves { get; set; }
    public List<string> Affixes { get; set; }
    public Dictionary<string, int> Attributes { get; set; }

    public Npc()
    {
        BaseStats = new BaseStat();
        EVS = new Dictionary<string, int>();
        Types = new List<string>();
        Nature = new Nature();
        Moves = new List<Move>();
        Affixes = new List<string>();
        Attributes = new Dictionary<string, int>();
    }
}