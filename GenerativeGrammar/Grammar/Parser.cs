using GenerativeGrammar.Model;
using GenerativeGrammar.NPC;

namespace GenerativeGrammar.Grammar
{

	public class Parser
	{
		private Dictionary<Node, string> Augments { get; set; }
		private Tree GenerativeTree { get; set; }
		private Log LevelLog { get; set; }

		private Parser(Log levelLog)
		{
			Augments = new Dictionary<Node, string>();
			GenerativeTree = new Tree();
			LevelLog = levelLog;
		}

		private IEnumerable<string> ReadGrammarFile(string file)
		{
			var lines = File.ReadAllLines(file);
			return lines;
		}

		private Tree HandleLines(List<string> lines)
		{
			Node previousNode = default;
			lines = lines.FindAll(e => !string.IsNullOrEmpty(e.Trim()));
			foreach (var line in lines)
			{
				var trimmedLine = line.Trim();
				var sides = trimmedLine.Split(":=");
				
				if (sides.Length == 2)
				{
					var node = HandleNodeLine(sides);
					previousNode = node;
				} else
				{
					
					Augments[previousNode] = trimmedLine;
				}
			}
			
			HandleAugments();
			
			return GenerativeTree;
		}

		private Node HandleNodeLine(IReadOnlyList<string> sides)
		{
			var node = HandleLeftSide(sides[0].Trim());
			var possibleNeighbours = HandleRightSide(sides[1].Trim());
			node.PossibleNeighbours = possibleNeighbours;
			if (node.PossibleNeighbours.Count == 1)
			{
				node.ActualNeighbours = node.PossibleNeighbours[0].Split(" ~ ").ToList();
			}
			GenerativeTree.Nodes.Add(node);
			return node;
		}
		
		private Node HandleLeftSide(string side)
		{
			var parts = side.Split("(");
			var node = new Node();
			if (parts.Length == 2)
			{
				node.Variables = parts[1].Trim().Replace(")", "").Split(", ").ToList();
				
			}
			node.Name = parts[0].Trim();
			return node;
		}
		
		private List<string> HandleRightSide(string side)
		{
			var neighbours = side.Split("|");
			return neighbours.Select(e => e.Trim()).ToList();
		}
		
		private void HandleAugments()
		{
			foreach (var key in Augments.Keys)
			{
				var augment = Augments[key].Split(":", 2);
				switch (augment[0])
				{
					case "global":
						HandleGlobalVariables(augment[1]);
						break;
					case "from":
						HandleSourceFile(key, augment[1]);
						break;
					case "condition":
						HandleConditions(key, augment[1]);
						break;
						
				}
			}
		}

		private void HandleConditions(Node node, string s)
		{
			var conditions = s.Trim().Split(" | ");
			foreach (var condition in conditions)
			{
				node.Conditions.Add(condition.Trim());
			}
		}

		private void HandleSourceFile(Node node, string s)
		{
			node.Source = s.Trim();
		}

		private void HandleGlobalVariables(string s)
		{
			var variables = s.Trim().Split(" | ");
			foreach (var variable in variables)
			{
				var expression = HandleNeighbourCondition(variable.Trim());
				var leftSide = expression.Trim().Split("<-")[0];
				var rightSide = expression.Trim().Split("<-")[1];
				GenerativeTree.GlobalVariables.Add(leftSide.Trim(), int.Parse(rightSide.Trim()));
			}
		}

		private string HandleNeighbourCondition(string rightSide)
		{
			var sides = rightSide.Split(" ? ");
			if (sides.Length == 2)
			{
				var condition = sides[0].Trim();
				var trueCondition = sides[1].Split(" : ")[0].Trim();
				var falseCondition = sides[1].Split(" : ")[1].Trim();
				return falseCondition;
			}
			return rightSide;
		}

		private static void Main()
		{
			// Parser parser = new(new Log());
			// var lines = parser.ReadGrammarFile(
			// 	Path.Combine(@"..", "..", "..", "Grammar", "Grammar.txt"));
			// var tree = parser.HandleLines(lines.ToList());
			//
			// var generator = new Generator(tree, parser.LevelLog);
			// generator.GenerateFromTree(tree.Nodes[0]);
			// var levelLog = new Log();
			// levelLog.PlayerTypes.Add("Bug");
			// levelLog.PlayerTypes.Add("Dark");
			// levelLog.PlayerTypes.Add("Dragon");
			// var generator = new Generator(new Tree(), levelLog);
			// const string condition = "(\"Dragon\" IN LOGS.PlayerTypes OR \"Dark\" NOT IN LOGS.PlayerTypes) => " +
			//                          "\"Water\" IN LOGS.PlayerTypes";
			// Console.WriteLine(generator.HandleCondition(condition));

			for (var i = 1; i <= 100; i++)
			{
				var index = Generator.PickIndexFromWeightedList(new List<int> {-10, 0, 1, 1, 1});
				Console.WriteLine(index);
			}

		}
	}
}
