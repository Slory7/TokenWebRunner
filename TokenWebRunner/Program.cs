using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TokenWebRunner.TaskCenter;

namespace TokenWebRunner
{
    static class Program
    {
        static void Main(string[] args)
        {
            string taskName = null;
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify the task name:");
                taskName = Console.ReadLine();
            }
            else
                taskName = args[0];

            var task = new TaskProcessor(taskName);
            var result = task.Run();

            Console.WriteLine(result);
        }
    }
}
