using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandApache3.src
{
    public class NamedTask
    {
        public string Name { get; }
        public Task Task { get; }

        public NamedTask(string name, Func<Task> taskFunc)
        {
            Name = name;
            Task = new Task(async () => await taskFunc());
        }
    }
}
