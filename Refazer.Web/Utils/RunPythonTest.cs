using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tutor;

namespace Refazer.Web.Utils
{
    public class RunPythonTest
    {
        public static bool Execute(List<string> testList, string code)
        {
            String script = code;

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
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process process = Process.Start(processStartInfo);

                if (process == null)
                {
                    return false;
                }
                
                if (!CheckProcessFinished(process))
                {
                    process.Kill();
                    return false;
                }
                
                bool succeed = process.ExitCode == 0;
                process.Close();
                return succeed;
            }
            catch (TestCaseException e)
            {
                ShowTraceError(e);
                return false;
            }
            catch (RuntimeBinderException e)
            {
                ShowTraceError(e);
                return false;
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
    }
}