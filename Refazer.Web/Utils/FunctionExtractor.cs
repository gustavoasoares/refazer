using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Refazer.Web.Utils
{
    public class FunctionExtractor
    {
        public static string ExtractPythonFunction(string code, string functionName)
        {
            string signature = @"^def( )+" + functionName + "\\(";
            string pyFunction = "";
            int pyFunctionLevel = 0;
            bool collectLines = false;

            List<Tuple<string, int>> identationLevelList = GetIdentationLevelList(code);

            foreach (Tuple<string, int> codeLine in identationLevelList)
            {
                if (collectLines)
                {
                    if (codeLine.Item2 > pyFunctionLevel)
                    {
                        pyFunction = pyFunction + "\n" + codeLine.Item1;
                    }
                    else
                    {
                        collectLines = false;
                        break;
                    }
                }

                var match = Regex.Match(codeLine.Item1, signature);

                if (match.Success)
                {
                    collectLines = true;
                    pyFunctionLevel = codeLine.Item2;
                    pyFunction = pyFunction + codeLine.Item1;
                }
            }

            return pyFunction;
        }

        private static List<Tuple<string, int>> GetIdentationLevelList(string code)
        {
            List<Tuple<string, int>> result = new List<Tuple<string, int>>();

            string[] codeLines = code.Split(new string[] { "\r\n", "\n" },
                StringSplitOptions.None);

            foreach (string line in codeLines)
            {
                if (!String.IsNullOrEmpty(line))
                {
                    result.Add(Tuple.Create(line, GetIndentationLevel(line)));
                }
            }

            return result;
        }

        private static int GetIndentationLevel(string codeLine)
        {
            var match = Regex.Match(codeLine, @"[^\s]");

            if (match.Success)
            {
                return match.Index;
            }

            return 0;
        }
    }
}