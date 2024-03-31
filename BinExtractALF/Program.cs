// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using BinExtractALF;
using EushullyExtractionUtils;

// ReSharper disable MustUseReturnValue

ConsoleUtils.PrintWarning("BinExtractAlf v0.01, version 4 code based on exs4alf v1.01 by asmodean");
var arguments = args.SkipWhile(a => a.Equals(nameof(BinExtractALF), StringComparison.InvariantCultureIgnoreCase)).ToArray();
var timer = Stopwatch.StartNew();
var success = Processing.Run(arguments);
if (success) ConsoleUtils.Print($"Completed in {timer.Elapsed:g}");
else ConsoleUtils.PrintError($"Completed with errors in {timer.Elapsed:g}");