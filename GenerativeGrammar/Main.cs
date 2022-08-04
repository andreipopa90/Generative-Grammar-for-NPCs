// See https://aka.ms/new-console-template for more information

using GenerativeGrammar.Grammar;
using GenerativeGrammar.Handlers;
using GenerativeGrammar.Model;

void Main()
{
    var levelLog = new Log();
    levelLog.PlayerTypes.Add("Bug");
    levelLog.PlayerTypes.Add("Dark");
    levelLog.PlayerTypes.Add("Dragon");
    Parser parser = new(levelLog);
    var lines = parser.ReadGrammarFile(
        Path.Combine(@"..", "..", "..", "Grammar", "Grammar.txt"));
    var tree = parser.HandleLines(lines.ToList());
    var generator = new Generator(tree, parser.LevelLog);
    generator.StartGeneration();
    Console.WriteLine(generator.Npcs[^1]);
    Console.WriteLine("Finished");
}

Main();