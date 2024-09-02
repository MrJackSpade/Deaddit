using System.Collections;
using System.Reflection;

public class DeepCopyHelper
{
    /// <summary>
    /// List of types to reference copy instead of deep copying
    /// </summary>
    public List<Type> ReferenceCopyTypes { get; } = [];

    /// <summary>
    /// Method to deep copy and return a new instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="original"></param>
    /// <returns></returns>
    public T DeepCopy<T>(T original)
    {
        return (T)this.DeepCopyInternal(original, []);
    }

    /// <summary>
    /// Method to copy contents from source to target
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void DeepCopy<T>(T source, T target)
    {
        if (source == null || target == null)
        {
            throw new ArgumentNullException("Source or Target cannot be null");
        }

        this.DeepCopyInternal(source, target, []);
    }

    private object DeepCopyInternal(object original, Dictionary<object, object> visited)
    {
        if (original == null)
        {
            return null;
        }

        Type typeToClone = original.GetType();

        // Check if the type should be reference copied
        if (ReferenceCopyTypes.Contains(typeToClone))
        {
            return original;
        }

        // Handle primitive types, enums, and strings
        if (typeToClone.IsPrimitive || typeToClone.IsEnum || typeToClone == typeof(string))
        {
            return original;
        }

        // Handle cases where the object has already been copied (to handle circular references)
        if (visited.ContainsKey(original))
        {
            return visited[original];
        }

        // Handle arrays
        if (typeToClone.IsArray)
        {
            Type? elementType = typeToClone.GetElementType();
            Array? array = original as Array;
            Array copiedArray = Array.CreateInstance(elementType, array.Length);
            visited[original] = copiedArray;

            for (int i = 0; i < array.Length; i++)
            {
                object copiedValue = this.DeepCopyInternal(array.GetValue(i), visited);
                copiedArray.SetValue(copiedValue, i);
            }

            return copiedArray;
        }

        // Handle collections that implement IList (e.g., List<T>)
        if (typeof(IList).IsAssignableFrom(typeToClone))
        {
            Type listType = typeToClone.GetGenericArguments().Length > 0 ? typeof(List<>).MakeGenericType(typeToClone.GetGenericArguments()) : typeToClone;
            IList? copiedList = (IList)Activator.CreateInstance(listType);
            visited[original] = copiedList;

            IList? originalList = original as IList;
            foreach (object? item in originalList)
            {
                object copiedItem = this.DeepCopyInternal(item, visited);
                copiedList.Add(copiedItem);
            }

            return copiedList;
        }

        // Handle objects (deep copy reference types)
        object? clone = Activator.CreateInstance(typeToClone);
        visited[original] = clone;

        foreach (FieldInfo field in typeToClone.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
        {
            object? fieldValue = field.GetValue(original);
            object copiedValue = this.DeepCopyInternal(fieldValue, visited);
            field.SetValue(clone, copiedValue);
        }

        return clone;
    }

    private void DeepCopyInternal(object source, object target, Dictionary<object, object> visited)
    {
        if (source == null || target == null)
        {
            return;
        }

        Type typeToClone = source.GetType();

        if (ReferenceCopyTypes.Contains(typeToClone))
        {
            return;
        }

        if (typeToClone.IsPrimitive || typeToClone.IsEnum || typeToClone == typeof(string))
        {
            return;
        }

        if (visited.ContainsKey(source))
        {
            return;
        }

        visited[source] = target;

        if (typeToClone.IsArray)
        {
            Type? elementType = typeToClone.GetElementType();
            Array? arraySource = source as Array;
            Array? arrayTarget = target as Array;

            for (int i = 0; i < arraySource.Length; i++)
            {
                object copiedValue = this.DeepCopyInternal(arraySource.GetValue(i), visited);
                arrayTarget.SetValue(copiedValue, i);
            }
        }
        else if (typeof(IList).IsAssignableFrom(typeToClone))
        {
            IList? originalList = source as IList;
            IList? targetList = target as IList;
            targetList.Clear();

            foreach (object? item in originalList)
            {
                object copiedItem = this.DeepCopyInternal(item, visited);
                targetList.Add(copiedItem);
            }
        }
        else
        {
            foreach (FieldInfo field in typeToClone.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                object? fieldValue = field.GetValue(source);
                object copiedValue = this.DeepCopyInternal(fieldValue, visited);
                field.SetValue(target, copiedValue);
            }
        }
    }
}
