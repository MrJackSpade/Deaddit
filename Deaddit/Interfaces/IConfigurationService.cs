using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Interfaces
{
    public interface IConfigurationService
    {
        T Read<T>() where T : class;

        void Write<T>(T obj) where T : class;
    }
}