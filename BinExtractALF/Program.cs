// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using BinExtractALF;
// ReSharper disable MustUseReturnValue

Processing.PrintWarning("BinExtractAlf v0.01, version 4 code based on exs4alf v1.01 by asmodean");
Processing.PrintWarning($"Processing: {string.Join(Environment.NewLine, args)}");
var timer = Stopwatch.StartNew();
var success = Processing.Run(args);
if (success) Processing.Print($"Completed in {timer.Elapsed:g}");
else Processing.PrintError($"Completed with errors in {timer.Elapsed:g}");
return;
