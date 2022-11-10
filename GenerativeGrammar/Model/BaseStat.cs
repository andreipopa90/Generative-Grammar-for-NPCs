namespace GenerativeGrammar.Model
{
    [Serializable]
    public class BaseStat
    {
        public string Name { get; set; }
        public string KeyName { get; set; }
        public int Hp { get; set; }
        public int Atk { get; set; }
        public int Def { get; set; }
        public int Spa { get; set; }
        public int Spd { get; set; }
        public int Spe { get; set; }
        public List<string> Types { get; set; }
    }
}