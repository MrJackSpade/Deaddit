using Deaddit.Utils.DeepCopy.Copiers;
using Deaddit.Utils.DeepCopy.Interfaces;
using System.Collections;
using System.Reflection;

namespace Deaddit.Utils.DeepCopy
{
    public class DeepCopyHelper
    {
        private readonly Dictionary<Type, ITypeCopier> _typeCopiers = [];

        public DeepCopyHelper()
        {
            this.Add(new ColorCopier());
            this.Add(new StringCopier());
        }

        public void Add<T>(AbstractCopier<T> abstractCopier) where T : class
        {
            _typeCopiers.Add(typeof(T), abstractCopier);
        }

        public T? Copy<T>(T obj) where T : class, new()
        {
            return (T)this.CopyInternal(obj);
        }

        public object? CopyInternal(object? obj)
        {
            if (obj is null)
            {
                return null;
            }

            Type typeToClone = obj.GetType();

            if (_typeCopiers.TryGetValue(typeToClone, out ITypeCopier? typeCopier))
            {
                return typeCopier.Copy(obj);
            }

            //For non-reference types, return the original. Its always a copy
            if (!typeToClone.IsClass)
            {
                return obj;
            }

            if (typeToClone.IsArray)
            {
                //This causes too much bullshit.
                throw new NotSupportedException("Arrays as targets are not supported");
            }
            else if (typeof(IList).IsAssignableFrom(typeToClone))
            {
                IList? originalList = obj as IList;
                IList? targetList = (IList)Activator.CreateInstance(typeToClone)!;

                foreach (object element in originalList)
                {
                    targetList.Add(this.CopyInternal(element));
                }

                return targetList;
            }
            else
            {
                object target = Activator.CreateInstance(typeToClone)!;

                foreach (FieldInfo field in typeToClone.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    object? sourceFieldValue = field.GetValue(obj);

                    field.SetValue(target, this.CopyInternal(sourceFieldValue));
                }

                return target;
            }
        }

        public T? Fill<T>(T source, T target) where T : class
        {
            return this.FillInternal(source, target) as T;
        }

        public object? FillInternal(object? source, object? target)
        {
            if (source is null)
            {
                return null;
            }

            Type typeToFill = source.GetType();

            if (_typeCopiers.TryGetValue(typeToFill, out ITypeCopier typeCopier))
            {
                return typeCopier.Fill(source, target);
            }

            //For non-reference types, return the original. Its always a copy
            if (!typeToFill.IsClass)
            {
                return source;
            }

            if (typeof(IList).IsAssignableFrom(typeToFill))
            {
                IList? originalList = source as IList;

                if (target is not IList targetList)
                {
                    return source;
                }

                while (targetList.Count > originalList.Count)
                {
                    targetList.RemoveAt(0);
                }

                for (int i = 0; i < originalList.Count; i++)
                {
                    if (targetList.Count <= i)
                    {
                        targetList.Add(originalList[i]);
                    }
                    else
                    {
                        targetList[i] = this.FillInternal(originalList[i], targetList[i]);
                    }
                }

                return targetList;
            }
            else
            {
                if (target is null)
                {
                    return source;
                }

                foreach (FieldInfo field in typeToFill.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    object? sourceFieldValue = field.GetValue(source);
                    object? targetFieldValue = field.GetValue(target);
                    field.SetValue(target, this.FillInternal(sourceFieldValue, targetFieldValue));
                }

                return target;
            }
        }
    }
}