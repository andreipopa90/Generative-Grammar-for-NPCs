using System.Collections.Generic;

namespace GenerativeGrammar.Model
{
    public class Npc
    {
        // public Dictionary<string, int> Attributes { get; }

        public Dictionary<string, List<dynamic>> ValuesOfNodes { get; set; }

        public Npc()
        {
            // Attributes = new Dictionary<string, int>();
            ValuesOfNodes = new Dictionary<string, List<dynamic>>();
        }

        public void GetNodesTerminalValues(string nodeName, ref List<dynamic> result, ref List<string> visited)
        {
            foreach (var nextNode in ValuesOfNodes[nodeName])
            {
                if (!ValuesOfNodes.ContainsKey(nextNode.ToString()))
                {
                    result.Add(nextNode);
                }
                else if (!visited.Contains(nextNode))
                {
                    visited.Add(nextNode);
                    GetNodesTerminalValues(nextNode.ToString(), ref result, ref visited);
                }
            }
        }
    }
}