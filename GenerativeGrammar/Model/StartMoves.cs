namespace GenerativeGrammar.Model
{
    [Serializable]
    public class StartMoves
    {
        public string Name { get; set; }
        public List<string> LearnSet { get; set; }
    }
}