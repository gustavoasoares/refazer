using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SubmissionGrader;

namespace TutorGradeService
{
    public static class CommandServer
    {
        private static readonly Uri ServiceUri = new Uri("net.pipe://localhost/Pipe");
        private const string PipeName = "TutorGradeService";

        private static readonly Grader Service = new Grader();
        private static ServiceHost _host;

        public static void Start()
        {
            _host = new ServiceHost(Service, ServiceUri);
            _host.AddServiceEndpoint(typeof(ICommandService), new NetNamedPipeBinding(), PipeName);
            _host.Open();
        }

        public static void Stop()
        {
            if ((_host == null) || (_host.State == CommunicationState.Closed)) return;

            _host.Close();
            _host = null;
        }
    }
}
