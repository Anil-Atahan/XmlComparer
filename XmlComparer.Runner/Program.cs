using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace XmlComparer.Runner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Showcase mode: if no arguments provided, run with detailed demo files
            if (args.Length == 0)
            {
                await RunShowcaseMode();
                return;
            }

            var options = RunnerApp.ParseArgs(args);
            int exitCode = await RunnerApp.Run(options);
            Environment.ExitCode = exitCode;
        }

        private static async Task RunShowcaseMode()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  XmlComparer Library - Output Format Showcase                  ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            // Define input files (will be copied by build)
            string originalFile = "sample1.xml";
            string modifiedFile = "sample2.xml";

            Console.WriteLine($"Comparing: {originalFile} → {modifiedFile}");
            Console.WriteLine(new string('=', Console.WindowWidth - 1));
            Console.WriteLine();

            // Define all output formats to showcase
            var showcaseFormats = new[]
            {
                new { Name = "TEXT", Format = "text", File = "showcase_diff.txt", Description = "Human-readable text format" },
                new { Name = "JSON", Format = "json", File = "showcase_diff.json", Description = "Machine-readable JSON with full details" },
                new { Name = "HTML", Format = "html", File = "showcase_diff.html", Description = "Interactive side-by-side comparison" },
                new { Name = "MARKDOWN", Format = "markdown", File = "showcase_diff.md", Description = "GitHub-compatible markdown" },
                new { Name = "CSV", Format = "csv", File = "showcase_diff.csv", Description = "Tabular data for spreadsheets" },
                new { Name = "UNIFIED DIFF", Format = "unified", File = "showcase_diff.diff", Description = "Git-style unified diff" }
            };

            foreach (var fmt in showcaseFormats)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"┌─ {fmt.Name}: {fmt.Description}");
                Console.ResetColor();
                Console.WriteLine($"│  Output: {fmt.File}");

                // Run comparison
                var compareArgs = new[]
                {
                    originalFile,
                    modifiedFile,
                    "--format", fmt.Format,
                    "--output", fmt.File,
                    "--verbose"
                };

                var options = RunnerApp.ParseArgs(compareArgs);
                var exitCode = await RunnerApp.Run(options);

                if (exitCode == 0 && File.Exists(fmt.File))
                {
                    var fileInfo = new FileInfo(fmt.File);
                    Console.WriteLine($"│  Size: {fileInfo.Length:N0} bytes");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"│  Status: ✓ Created successfully");
                    Console.ResetColor();

                    // For HTML files, offer to open in browser
                    if (fmt.Format == "html" && IsWindows())
                    {
                        Console.Write($"│  Open in browser? (Y/N): ");
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Y)
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = fmt.File,
                                    UseShellExecute = true
                                });
                                Console.WriteLine("│  Opened in browser");
                            }
                            catch { }
                        }
                        else
                        {
                            Console.WriteLine("│  Skipped opening browser");
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"│  Status: ✗ Failed (exit code: {exitCode})");
                    Console.ResetColor();
                }

                Console.WriteLine(new string('─', Console.WindowWidth - 1));
                Console.WriteLine();
            }

            // Summary
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                           Showcase Complete!                                 ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Generated files:");
            foreach (var fmt in showcaseFormats)
            {
                if (File.Exists(fmt.File))
                    Console.WriteLine($"  • {fmt.File,-30} ({fmt.Name})");
            }
            Console.WriteLine();
            Console.WriteLine("Try running with custom options:");
            Console.WriteLine($"  dotnet run --project XmlComparer.Runner -- {originalFile} {modifiedFile} --format html");
            Console.WriteLine($"  dotnet run --project XmlComparer.Runner -- {originalFile} {modifiedFile} --format json --output custom.json");
        }

        private static bool IsWindows()
        {
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        }
    }
}
