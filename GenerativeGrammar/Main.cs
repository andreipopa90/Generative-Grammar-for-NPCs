// See https://aka.ms/new-console-template for more information

using GenerativeGrammar.Grammar;
using GenerativeGrammar.Handlers;
using GenerativeGrammar.Model;

void Main()
{
    var generator = new Generator();
    var levelLog = Log.GetInstance();
    levelLog.PlayerTypes.Add("Bug");
    levelLog.PlayerTypes.Add("Dark");
    levelLog.PlayerTypes.Add("Dragon");
    levelLog.PlayerDefense = "Special";
    levelLog.PlayerAttack = "Special";
    generator.StartGeneration(levelLog);
    Console.WriteLine(generator.Npcs[^1]);
    Console.WriteLine("Finished");
}

Main();