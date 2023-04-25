using GenerativeGrammar.Model;
using Newtonsoft.Json;

namespace GenerativeGrammar.JsonParser
{
    public class JsonReader
    {

        private readonly string _pathRoot = Path.Combine(@"..", "..", "..", "JsonParser", "JsonFiles");

        public List<Move> ReadMovesJson()
        {
            StreamReader reader = new(Path.Combine(_pathRoot, "moves.json"));
            var json = reader.ReadToEnd();
            var moves = JsonConvert.DeserializeObject<List<Move>>(json);
            reader.Close();

            return moves;
        }

        public List<BaseStat> ReadBaseStatsJson()
        {
            StreamReader reader = new(Path.Combine(_pathRoot, "basestats.json"));
            var json = reader.ReadToEnd();
            var baseStats = JsonConvert.DeserializeObject<List<BaseStat>>(json);
            baseStats.Sort((a, b) => 
                (a.Atk + a.Def + a.Hp + a.Spa + a.Spd + a.Spe)
                .CompareTo(b.Atk + b.Def + b.Hp + b.Spa + b.Spd + b.Spe));
            reader.Close();

            return baseStats;
        }

        public List<Type> ReadTypeChartJson()
        {
            StreamReader reader = new(Path.Combine(_pathRoot, "typechart.json"));
            var json = reader.ReadToEnd();
            var typeChart = JsonConvert.DeserializeObject<List<Type>>(json);
            reader.Close();

            return typeChart;
        }

        public List<Nature> ReadNaturesJson()
        {
            StreamReader reader = new(Path.Combine(_pathRoot, "natures.json"));
            var json = reader.ReadToEnd();
            var natures = JsonConvert.DeserializeObject<List<Nature>>(json);
            reader.Close();

            return natures;
        }

        public List<StartMoves> ReadStartMovesJson()
        {
            StreamReader reader = new(Path.Combine(_pathRoot, "learnsets.json"));
            var json = reader.ReadToEnd();
            var startingMoves = JsonConvert.DeserializeObject<List<StartMoves>>(json);
            reader.Close();
			
            return startingMoves;
        }
		
    }
}