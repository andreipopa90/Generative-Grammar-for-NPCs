using System.Collections.Generic;
using System.Linq;
using Fare;

namespace GenerativeGrammar.Grammar
{

	public class Parser
	{
		public Dictionary<Node, List<string>> Augments { get; set; }
		public Tree GenerativeTree { get; set; }

		private Parser()
		{
			Augments = new Dictionary<Node, List<string>>();
			GenerativeTree = new Tree
			{
				Name = "Enemy Wave",
				Nodes = new List<Node>()
			};
		}

		private IEnumerable<string> ReadGrammarFile(string file)
		{
			var lines = File.ReadAllLines(file);
			return lines;
		}

		private void HandleLines(List<string> lines)
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
					if (!Augments.ContainsKey(previousNode)) Augments[previousNode] = new List<string>();
					Augments[previousNode].Add(trimmedLine);
				}
			}
			
			HandleAugments();
		}

		private void HandleAugments()
		{
			foreach (var key in Augments.Keys)
			{
				bool condition = false;
				bool global = false;
				bool source = false;
				
				foreach (var augment in Augments[key])
				{
					if (augment.Contains("from"))
					{
						source = true;
						condition = false; 
						global = false;
					}

					if (augment.Contains("global"))
					{
						source = false;
						condition = false; 
						global = true;
					}
					
					if (augment.Contains("condition"))
					{
						source = false;
						condition = false; 
						global = true;
					}
				}
			}
			Console.WriteLine("Hello");
		}

		private Node HandleNodeLine(IReadOnlyList<string> sides)
		{
			var node = HandleLeftSide(sides[0].Trim());
			var possibleNeighbours = HandleRightSide(sides[1].Trim());
			node.PossibleNeighbours = possibleNeighbours;
			if (node.PossibleNeighbours.Count == 1)
			{
				node.ActualNeighbours = node.PossibleNeighbours[0].Split("~").ToList();
			}
			GenerativeTree.Nodes.Add(node);
			return node;
		}

		private List<string> HandleRightSide(string side)
		{
			var neighbours = side.Split(" | ");
			return neighbours.ToList();
		}

		private Node HandleLeftSide(string side)
		{
			var parts = side.Split("(");
			var node = new Node
			{
				Name = parts[0].Trim(),
				Variables = new List<string>(),
				Weights =  new List<int>(),
				PossibleNeighbours = new List<string>(),
				ActualNeighbours = new List<string>(),
				Conditions = new List<string>(),
				Source = new List<string>(),
				GlobalVariables = new List<string>()
			};
			if (parts.Length == 2)
			{
				node.Variables = parts[1].Trim().Replace(")", "").Split(", ").ToList();
			} 
			return node;
		}

		private static void Main()
		{
			Parser parser = new();
			var lines = parser.ReadGrammarFile(
				Path.Combine(@"..", "..", "..", "Grammar", "Grammar.txt"));
			parser.HandleLines(lines.ToList());
			Console.WriteLine(parser.GenerativeTree);
		}
	}
}
