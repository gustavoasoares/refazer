using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TutorGradeService
{
    public partial class GradeService : ServiceBase
    {
        public GradeService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (!EventLog.SourceExists("TutorGradeService"))
                    EventLog.CreateEventSource("TutorGradeService", "Application");

                EventLog.WriteEntry("TutorGradeService", "Starting Command Server", EventLogEntryType.Information);

                var thread = new Thread(CommandServer.Start);

                thread.Start();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("TutorGradeService", ex.ToString(), EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            CommandServer.Stop();
        }
    }
}
