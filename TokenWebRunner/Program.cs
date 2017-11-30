using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TokenWebRunner.TaskCenter;

namespace TokenWebRunner
{
    static class Program
    {
        static int Main(string[] args)
        {
            string taskName = null;
            bool isFromWindowsScheduler = false;
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify the task name:");
                taskName = Console.ReadLine();
            }
            else
            {
                taskName = args[0];
                isFromWindowsScheduler = true;
            }

            var task = new TaskProcessor(taskName);
            var result = task.Run();

            Console.WriteLine(result.Message);

            if (!result.IsSuccess && isFromWindowsScheduler)
                throw new ApplicationException($"TokenWebRunner: {taskName} is failed.");

            return result.IsSuccess ? 0 : 1;
        }
    }
}
