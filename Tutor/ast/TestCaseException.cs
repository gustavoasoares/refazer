using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor
{
    public class TestCaseException : Exception
    {
        public TestCaseException(Exception e) : base(e.Message, e)
        {
        }
    }
}
