namespace GenerativeGrammar.NPC;

public class Npc
{
    public Dictionary<string, int> BaseStats { get; set; }
    public Dictionary<string, int> EVS { get; set; }
    public List<string> Types { get; set; }
    public string Nature { get; set; }
    public List<string> Moves { get; set; }
    public List<string> Affixes { get; set; }
    public Dictionary<string, int> Attributes { get; set; }

    public Npc()
    {
        BaseStats = new Dictionary<string, int>();
        EVS = new Dictionary<string, int>();
        Types = new List<string>();
        Nature = "";
        Moves = new List<string>();
        Affixes = new List<string>();
        Attributes = new Dictionary<string, int>();
    }
}