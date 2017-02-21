using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenWebRunner
{
    public class TaskInfo
    {
        public string TaskName { get; set; }
        public string Description { get; set; }
        public string BaseUrl { get; set; }
        public string TokenUrl { get; set; }
        public string TokenParams { get; set; }
        public string RequestUrl { get; set; }
        public string RequestMethod { get; set; }
        public string RequestContentType { get; set; }
        public string RequestBody { get; set; }
        public int RequestTimeout { get; set; }
        public override string ToString()
        {
            return TaskName + "(" + Description + ")";
        }
    }
}
