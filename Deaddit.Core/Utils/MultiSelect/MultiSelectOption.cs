using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Core.Utils.MultiSelect
{
    public class MultiSelectOption
    {
        public string DisplayName { get; set; }

        public Func<Task> Func { get; set; }

        public MultiSelectOption(string name, Func<Task> func)
        {
            DisplayName = name;
            Func = func;
        }

        public MultiSelectOption(string template, string name, Func<Task> func)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (template.Contains("{0}"))
            {
                DisplayName = string.Format(template, name);
            }
            else
            {
                DisplayName = $"{template}{name}";
            }
        }
    }
}