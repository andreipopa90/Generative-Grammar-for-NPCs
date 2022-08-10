using GenerativeGrammar.Handlers;
using GenerativeGrammar.Model;

namespace GenerativeGrammar.Grammar
{

	/**
	 * <summary>
	 * The <code>Parser</code> class takes care of reading the grammar from a file and creating a tree.
	 * </summary>
	 */
	public class Parser
	{
		private Dictionary<Node, List<string>> Augments { get; }
		private Tree GenerativeTree { get; }
		public Log LevelLog { get; }

		public Parser(Log levelLog)
		{
			Augments = new Dictionary<Node, List<string>>();
			GenerativeTree = new Tree();
			LevelLog = levelLog;
		}

		public IEnumerable<string> ReadGrammarFile(string file)
		{
			var lines = File.ReadAllLines(file);
			return lines;
		}

		public Tree HandleLines(List<string> lines)
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
					GenerativeTree.Root = node;
				} 
				else
				{
					AddAugmentsToNode(previousNode, trimmedLine);
				}
			}
			HandleAugments();
			SetTerminalNodes();
			
			return GenerativeTree;
		}

		private void AddAugmentsToNode(Node previousNode, string trimmedLine)
		{
			if (!Augments.ContainsKey(previousNode)) Augments.Add(previousNode, new List<string>());
			Augments[previousNode].Add(trimmedLine);
		}

		private void SetTerminalNodes()
		{
			foreach (var node in GenerativeTree.Nodes)
			{
				var isTerminal = true;

				var possibleNeighboursList = 
					from neighbour in node.PossibleNeighbours 
					select neighbour.Split(" ~ ") into possibleNeighbours 
					from n in possibleNeighbours 
					select n.Split(" : ", 2)[0].Trim().Split("] ");
				
				foreach (var n in possibleNeighboursList)
				{
					dynamic nodeValue = n;
					nodeValue = nodeValue.Length == 2 ? nodeValue[1] : nodeValue[0];
					if (GenerativeTree.Nodes.FindIndex(e => e.Name.Equals(nodeValue.Trim())) >= 0) 
						isTerminal = false;
				}
				

				node.IsTerminalNode = isTerminal;
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
			var handler = ExpressionHandler.GetInstance();
			handler.GenerativeTree = GenerativeTree;
			handler.LevelLog = LevelLog;
			var sides = rightSide.Split(" ? ");
			if (sides.Length != 2) return rightSide;

			var condition = sides[0].Trim();
			var conditionResult = handler.HandleCondition(condition);
			var trueCondition = sides[1].Split(" : ")[0].Trim();
			var falseCondition = sides[1].Split(" : ")[1].Trim();
			return conditionResult ? trueCondition : falseCondition;
		}

		
	}
}
