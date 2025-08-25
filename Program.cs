using System.CommandLine;
using Watercooler;

Console.WriteLine("Watercooler - DELTARUNE At-Runtime Mod Compiler");
Console.WriteLine("-----------------------------------------------");

RootCommand rootCommand = new();

// watercooler.exe compile --input <in> --output <out> --project <proj_1> --project <proj_2> ...
Command compile = new("compile", "Compile a Watercooler Project into a data file or xdelta.");
Option<string> opt_input = new("--input", "-i") { Description = "The data file which you want to apply the patches to.", Required = true };
Option<string> opt_output = new("--output", "-o") { Description = "Output file, which the patched data file or xdelta patch will be saved to.", Required = true };
Option<string[]> opt_projects = new("--project", "-p") { Description = "Watercooler Project files to apply to the input file.", Arity = ArgumentArity.OneOrMore, AllowMultipleArgumentsPerToken = true, Required = true };

compile.Options.Add(opt_input);
compile.Options.Add(opt_output);
compile.Options.Add(opt_projects);
compile.SetAction(parseResult =>
{
    Compiler compileJob = new(
        parseResult.GetValue(opt_projects),
        parseResult.GetValue(opt_input),
        parseResult.GetValue(opt_output));
});

rootCommand.Subcommands.Add(compile);

rootCommand.Parse(args).Invoke();