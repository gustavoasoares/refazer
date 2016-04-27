using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace SubmissionGrader
{
    [ServiceContract(Namespace = "http://example.com/RoslynCodeExecution")]
    public interface ICommandService
    {
        [OperationContract]
        dynamic Execute(string code);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Grader : ICommandService
    {
        private CompiledCode comped;
        private dynamic result;

        public dynamic Execute(string script)
        {
            var options = new Dictionary<string, object>();
            options["LightweightScopes"] = true;
            var py = Python.CreateEngine(options);
            var source = py.CreateScriptSourceFromString(script);

            try
            {
               comped = source.Compile();                
            }
            catch (Exception)
            {
                py.Runtime.Shutdown();
                throw new Exception("program does not compile");
            }
            var thread = new Thread(Execute);
            thread.Start();
            if (thread.Join(1000))
            {
                thread.Abort();
                return "aborted";
            }
            return result;
        }

        private void Execute()
        {
            result = comped.Execute();
        }
    }
}
