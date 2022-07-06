using GenerativeGrammar.Model;
using GenerativeGrammar.NPC;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace GenerativeGrammar.Grammar
{

	public class Parser
	{
		private Dictionary<Node, List<string>> Augments { get; set; }
		private Tree GenerativeTree { get; set; }
		private Log LevelLog { get; set; }

		private Parser(Log levelLog)
		{
			Augments = new Dictionary<Node, List<string>>();
			GenerativeTree = new Tree();
			LevelLog = levelLog;
		}

		private static IEnumerable<string> ReadGrammarFile(string file)
		{
			var lines = File.ReadAllLines(file);
			return lines;
		}

		private Tree HandleLines(List<string> lines)
		{
			Node previousNode = default!;
			lines = lines.FindAll(e => !string.IsNullOrEmpty(e.Trim()));
			foreach (var line in lines)
			{
				
				var trimmedLine = line.Trim();
				var sides = trimmedLine.Split(":=");
				
				if (sides.Length == 2)
				{
					var node = HandleNodeLine(sides);
					previousNode = node;
					if (lines.IndexOf(line) != 0) continue;
					var generativeTree = GenerativeTree;
					generativeTree.Root = node;
					GenerativeTree = generativeTree;
				} 
				else
				{
					if (!Augments.ContainsKey(previousNode)) Augments.Add(previousNode, new List<string>());
					Augments[previousNode].Add(trimmedLine);
				}
			}
			HandleAugments();
			SetLeafNodes();
			
			return GenerativeTree;
		}

		private void SetLeafNodes()
		{
			var leafNodes = new List<Node>();
			foreach (var node in GenerativeTree.Nodes)
			{
				var isLeaf = true;
				foreach (var neighbour in node.PossibleNeighbours)
				{
					var possibleNeighbours = neighbour.Split(" ~ ", 2);
					foreach (var n in possibleNeighbours)
					{
						dynamic nodeValue = n.Split(" : ", 2)[0].Trim().Split("] ");
						nodeValue = nodeValue.Length == 2 ? nodeValue[1] : nodeValue[0];
						if (GenerativeTree.Nodes.FindIndex(e => e.Name.Equals(nodeValue.Trim())) >= 0) 
							isLeaf = false;
					}
				}

				node.IsLeafNode = isLeaf;
				leafNodes.Add(node);
			}
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
				GenerativeTree.Parameters.AddRange(parts[1].Trim().Replace(")", "").Split(", ").ToList());
				
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
				var augments = Augments[key];
				foreach (var augment in augments.Select(augment => augment.Split(":", 2)))
				{
					switch (augment[0])
					{
						case "global":
							HandleGlobalVariables(augment[1]);
							break;
						case "from":
							HandleSourceFile(key.Name, augment[1]);
							break;
						case "condition":
							HandleConditions(key.Name, augment[1]);
							break;
						
					}
				}
			}
		}

		private void HandleConditions(string nodeName, string s)
		{
			var node = GenerativeTree.Nodes.Find(e => e.Name.Equals(nodeName));
			var conditions = s.Trim().Split(" | ");
			foreach (var condition in conditions)
			{
				node?.Conditions.Add(condition.Trim());
			}
		}

		private void HandleSourceFile(string nodeName, string s)
		{
			var index = GenerativeTree.Nodes.FindIndex(e => e.Name.Equals(nodeName));
			var node = GenerativeTree.Nodes[index];
			node.Source = s.Trim();
			GenerativeTree.Nodes[index] = node;
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
			if (sides.Length != 2) return rightSide;
			
			var condition = sides[0].Trim();
			var trueCondition = sides[1].Split(" : ")[0].Trim();
			var falseCondition = sides[1].Split(" : ")[1].Trim();
			return falseCondition;
		}

		private static void Main()
		{
			var levelLog = new Log();
			levelLog.PlayerTypes.Add("Bug");
			levelLog.PlayerTypes.Add("Dark");
			levelLog.PlayerTypes.Add("Dragon");
			Parser parser = new(levelLog);
			var lines = ReadGrammarFile(
				Path.Combine(@"..", "..", "..", "Grammar", "Grammar.txt"));
			var tree = parser.HandleLines(lines.ToList());
			var generator = new Generator(tree, parser.LevelLog);
			generator.StartGeneration();
		}
	}
}
