using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;

namespace MozJpeg {
    class Program {
        static readonly string mozjpeg_arg = $@"/c {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\Tools\jpegtran.exe -copy all";

        static int total = 0, current = 0;
        static void Main(string[] args) {
            if (args == null) return;
            else {
                var jpgs = Getjpgs(args).ToArray();
                total = jpgs.Length;
                long TotalDelta = 0;
                Console.WriteLine($"合計:{total}");
                if (total == 0) return;
                Console.WriteLine("Start");
                
                jpgs.AsParallel().ForAll(f => {
                    try {
                        var tempf = Path.GetTempFileName();
                        var psi = new ProcessStartInfo() {
                            FileName = "cmd.exe", Arguments = $"{mozjpeg_arg} {f.WQ()} > {tempf.WQ()}",
                            UseShellExecute = false, CreateNoWindow = true,

                        };
                        Process.Start(psi).WaitForExit();


                        FileInfo fiI = new FileInfo(f), fiT = new FileInfo(tempf);
                        if (fiT.Length > 0) {
                            var delta = fiI.Length - fiT.Length;
                            if (delta != 0) Interlocked.Add(ref TotalDelta, fiI.Length - fiT.Length);
                            fiI.IsReadOnly = false;
                            fiI.Delete();
                            fiT.MoveTo(f);
                            Interlocked.Increment(ref current);
                        }
                        else {
                            Console.WriteLine($"error on: {f}");
                            fiT.Delete();
                        }
                        Console.Write($"\rProgress : {current}/{total} | {SizeSuffix(TotalDelta)} decreased");

                    }
                    catch (Exception e) {
                        Console.WriteLine($"{e.Message}{Environment.NewLine}on: {f}");
                    }
                });

            }
            SystemSounds.Asterisk.Play();
            Console.WriteLine($"\ncomplete");

            Console.WriteLine("\npress enter to exit");
            Console.ReadLine();
        }

        static readonly string[] SizeSuffixes = { "バイト", "KB", "MB", "GB", "TB" };
        static public string SizeSuffix(Int64 value, int decimalPlaces = 1) {
            if (decimalPlaces < 0) throw new ArgumentOutOfRangeException("decimalPlaces");
            if (value < 0) return $"-{SizeSuffix(-value)}";
            if (value == 0) return "0 バイト";
            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000) {
                mag += 1;
                adjustedSize /= 1024;
            }
            Console.WriteLine(adjustedSize.ToString());
            return $"{adjustedSize:n}{decimalPlaces} {SizeSuffixes[mag]}";
        }

        static IEnumerable<string> Getjpgs(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                string argument = args[i];
                if (File.GetAttributes(argument).HasFlag(FileAttributes.Directory)) {
                    foreach (var jpg in Directory.EnumerateFiles(argument, "*.*", SearchOption.AllDirectories).Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))) {
                        yield return jpg;
                    }
                }
                else if (argument.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) yield return argument;
            }
        }

    }
    public static class StringExtensionMethods {
        public static string WQ(this string text) => $"\"{text}\"";
    }


}
