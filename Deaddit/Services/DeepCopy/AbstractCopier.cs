using Deaddit.Services.DeepCopy.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deaddit.Services.DeepCopy
{
    public abstract class AbstractCopier<T> : ITypeCopier where T : class 
    {
        public abstract T Copy(T source);

        public abstract T Fill(T source, T destination);

        object ITypeCopier.Copy(object inCopy)
        {
            return this.Copy(inCopy as T);
        }

        object ITypeCopier.Fill(object source, object destination)
        {
            return this.Fill(source as T, destination as T);
        }
    }
}
