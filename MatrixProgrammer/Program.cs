using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatrixProgrammer {
    class Program {

        static public void Main(string[] args) {
            new Program().Run(args);
        }

        private OutputFormatter Formatter;
        private int MatrixSize = 4;
        private int O = 1;

        private List<Dictionary<string, string>> CachedNSets = new List<Dictionary<string, string>>();
        private StringBuilder GlobalStringBuilder = new StringBuilder();

        public void Run(string[] args) {
            CheckArguments(args);

            var stringBuilderOne = new StringBuilder();
            var stringBuilderTwo = new StringBuilder();

            Console.WriteLine("// " + (Formatter is CPPOutputFormatter ? "C++" : "C#") + " code for compute invertion " + MatrixSize + " x " + MatrixSize + " matrix by willnode and ThomasCZ");
            Console.WriteLine();

            WriteDeterminant(MatrixSize, stringBuilderOne);
            WriteInverse(MatrixSize, stringBuilderTwo);

            if (O >= 2)
                WriteCachedCodes();

            Console.WriteLine(Formatter.Determinant(stringBuilderOne.ToString()));
            Console.WriteLine(Formatter.Result(MatrixSize, stringBuilderTwo.ToString()));
        }

        private void CheckArguments(string[] args) {
            if (args.Length == 4) {
                for (int i = 0; i < args.Length; i += 2) {
                    switch (args[i]) {
                        case "-n":
                            if (!int.TryParse(args[i + 1], out MatrixSize)) {
                                Console.WriteLine("Wrong matrix size");
                                Environment.Exit(0);
                            }

                            O = Math.Max(MatrixSize - 2, 1);
                            break;
                        case "-f":
                            switch (args[i + 1]) {
                                case "cpp":
                                    Formatter = new CPPOutputFormatter();
                                    break;
                                case "cs":
                                    Formatter = new CSharpOutputFormatter();
                                    break;
                                default:
                                    Console.WriteLine("Invalid format");
                                    Environment.Exit(0);
                                    break;
                            }
                            break;
                        default:
                            Console.WriteLine("Illegal argument");
                            Environment.Exit(0);
                            break;
                    }
                }
            } else {
                Console.WriteLine("Following arguments can be used:");
                Console.WriteLine("\t-n [int] ... size of the matrix");
                Console.WriteLine("\t-f [cpp, cs] ... code formatting");
                Console.WriteLine("\t\t cpp ... C++ code style");
                Console.WriteLine("\t\t cs ... C# code style");
                Environment.Exit(0);
            }
        }

        private void WriteDeterminant(int n, StringBuilder stringBuilder) {
            var xRange = Enumerable.Range(0, n).ToArray();
            var yRange = Enumerable.Range(0, n).ToArray();
            WriteDeterminant(n, xRange, yRange, stringBuilder, true);
        }

        private void WriteDeterminant(int n, int[] xRange, int[] yRange, StringBuilder stringBuilder, bool newLine) {
            if (O >= n && O >= 2) {
                stringBuilder.Append(WriteOptimizedNDet(n, xRange, yRange));
                return;
            }

            bool plus = true;
            for (int i = 0; i < n; i++) {
                if (i > 0) {
                    if (newLine)
                        stringBuilder.Append("\n\t");
                    stringBuilder.Append(plus ? " + " : " - ");
                }

                int x = xRange[i];
                int y = yRange[0];
                stringBuilder.Append(Formatter.SourceMatrixElement(MatrixSize, y, x));
                plus = !plus;

                if (n > 1) {
                    stringBuilder.Append(" * ");

                    if (ShouldAddBracket(n))
                        stringBuilder.Append("(");

                    WriteDeterminant(n - 1, xRange.Where(j => j != x).ToArray(), yRange.Where(j => j != y).ToArray(), stringBuilder, false);
                    if (ShouldAddBracket(n))
                        stringBuilder.Append(")");
                }
            }
        }

        private bool ShouldAddBracket(int n) {
            return n > O + 1;
        }

        private string WriteOptimizedNDet(int n, int[] xRange, int[] yRange) {
            var stringBuilder = new StringBuilder();
            var chacheSets = GetCacheSets(n);
            var cacheVar = CreateCacheVarName(xRange, yRange);

            if (!chacheSets.ContainsKey(cacheVar)) {
                if (n > 2) {
                    bool plus = true;
                    for (int i = 0; i < n; i++) {
                        if (i > 0)
                            stringBuilder.Append(plus ? " + " : " - ");

                        int x = xRange[i];
                        int y = yRange[0];
                        stringBuilder.Append(Formatter.SourceMatrixElement(MatrixSize, y, x));
                        plus = !plus;

                        stringBuilder.Append(" * ");
                        stringBuilder.Append(WriteOptimizedNDet(n - 1, xRange.Where(j => j != x).ToArray(), yRange.Where(j => j != y).ToArray()));
                    }
                } else
                    stringBuilder.AppendFormat(Formatter.CacheContent(Formatter.SourceMatrixElement(MatrixSize, yRange[0], xRange[0]), Formatter.SourceMatrixElement(MatrixSize, yRange[1], xRange[1]), Formatter.SourceMatrixElement(MatrixSize, yRange[0], xRange[1]), Formatter.SourceMatrixElement(MatrixSize, yRange[1], xRange[0])));

                chacheSets[cacheVar] = Formatter.CacheMember(cacheVar, stringBuilder.ToString());
                stringBuilder.Clear();
            }

            return cacheVar;
        }

        private void WriteCachedCodes() {
            foreach (var i in CachedNSets) {
                if (i.Count == 0)
                    continue;

                foreach (var j in i)
                    Console.WriteLine(j.Value);

                Console.WriteLine();
            }
        }

        private Dictionary<string, string> GetCacheSets(int n) {
            while (CachedNSets.Count < n + 1)
                CachedNSets.Add(new Dictionary<string, string>());

            return CachedNSets[n];
        }

        private void WriteInverse(int n, StringBuilder stringBuilder) {
            var stringBuilderTwo = new StringBuilder();

            var xRange = Enumerable.Range(0, n).ToArray();
            var yRange = Enumerable.Range(0, n).ToArray();

            for (int y = 0; y < n; y++) {
                for (int x = 0; x < n; x++) {
                    var plus = (x + y) % 2 == 1 ? "-" : "";
                    WriteDeterminant(n - 1, yRange.Where(i => i != y).ToArray(), xRange.Where(i => i != x).ToArray(), stringBuilderTwo, false);
                    stringBuilder.AppendLine(Formatter.InverseMember(Formatter.MatrixElement(MatrixSize, y, x), plus, stringBuilderTwo.ToString()));
                    stringBuilderTwo.Clear();
                }
            }
        }

        private string CreateCacheVarName(int[] t, int[] u) {
            GlobalStringBuilder.Clear();
            GlobalStringBuilder.Append(((char)((Formatter.IsCacheMemberUppercase() ? '?' : '_') + t.Length)).ToString());
            GlobalStringBuilder.Append(string.Join("", t));
            GlobalStringBuilder.Append(string.Join("", u));
            return GlobalStringBuilder.ToString();
        }
    }
}