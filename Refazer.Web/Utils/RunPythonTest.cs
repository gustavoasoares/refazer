using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tutor;

namespace Refazer.Web.Utils
{
    public class RunPythonTest
    {
        public static Tuple<bool, List<String>> Execute(List<string> testList, string code)
        {
            String script = code + Environment.NewLine + GetLogGenerateFunction();

            foreach (var test in testList)
            {
                script += Environment.NewLine + test;
            }

            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(
                    "python.exe", "-c \"" + script + "\"")
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process process = Process.Start(processStartInfo);
                List<String> logsList = new List<String>();

                while (!process.StandardError.EndOfStream)
                {
                    String line = process.StandardError.ReadLine();
                    logsList.Add(line);
                }

                if (process == null)
                {
                    return Tuple.Create(false, new List<String>());
                }
                
                if (!CheckProcessFinished(process))
                {
                    process.Kill();
                    return Tuple.Create(false, new List<String>());
                }

                bool succeed = process.ExitCode == 0;
                process.Close();
                return Tuple.Create(succeed, logsList);
            }
            catch (TestCaseException e)
            {
                ShowTraceError(e);
                return Tuple.Create(false, new List<String>());
            }
            catch (RuntimeBinderException e)
            {
                ShowTraceError(e);
                return Tuple.Create(false, new List<String>());
            }
        }

        private static bool CheckProcessFinished(Process process)
        {
            if (!process.HasExited)
            {
                process.WaitForExit(1500);
            }

            return process.HasExited;
        }

        private static void ShowTraceError(Exception e)
        {
            Trace.TraceError("Exception was thrown when running python tests");
            Trace.TraceError(e.Message);
        }

        private static String GetLogGenerateFunction() {
            String logGenerateFunction = @"
def logs_generate(description, expected, result):
    logs = '''
    Doctests for assignment

    >>> {0}

    # Error: expected
    #     {1}
    # but got
    #     {2}

    '''
    return logs.format(description, expected, result)
";

            return logGenerateFunction;
        } 
    }
}