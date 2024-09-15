using System.Reflection;
using System.Reflection.Emit;

namespace Deaddit.Utils
{

    public static class PageFactory
    {
        private static readonly AssemblyBuilder assemblyBuilder;
        private static readonly ModuleBuilder moduleBuilder;

        static PageFactory()
        {
            AssemblyName assemblyName = new("DynamicAssembly");
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
        }

        private static void CreateConstructor(TypeBuilder typeBuilder, Type baseType, object[] constructorArgs)
        {
            ConstructorInfo[] constructors = baseType.GetConstructors();
            ConstructorInfo matchingConstructor = null;

            foreach (ConstructorInfo ctor in constructors)
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                if (parameters.Length == constructorArgs.Length)
                {
                    bool isMatch = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (constructorArgs[i] != null &&
                            !parameters[i].ParameterType.IsAssignableFrom(constructorArgs[i].GetType()))
                        {
                            isMatch = false;
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        matchingConstructor = ctor;
                        break;
                    }
                }
            }

            if (matchingConstructor == null)
            {
                throw new ArgumentException("No matching constructor found in the base class for the provided arguments.");
            }

            ParameterInfo[] ctorParams = matchingConstructor.GetParameters();
            Type[] parameterTypes = ctorParams.Select(p => p.ParameterType).ToArray();

            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                parameterTypes);

            ILGenerator il = constructorBuilder.GetILGenerator();

            // Load 'this' and all constructor arguments
            il.Emit(OpCodes.Ldarg_0);
            for (int i = 1; i <= ctorParams.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }

            // Call base constructor
            il.Emit(OpCodes.Call, matchingConstructor);

            // Return
            il.Emit(OpCodes.Ret);
        }

        public static T CreateProxy<T>(params object[] constructorArgs) where T : class
        {
            Type baseType = typeof(T);
            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                $"{baseType.Name}Proxy",
                TypeAttributes.Public | TypeAttributes.Class,
                baseType);

            // Create constructor
            CreateConstructor(typeBuilder, baseType, constructorArgs);

            MethodInfo[] methods = baseType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName) // Exclude property getters and setters
                .ToArray();

            foreach (MethodInfo method in methods)
            {
                if (method.ReturnType != typeof(void))
                {
                    continue;
                }

                if (method.DeclaringType.Assembly != typeof(T).Assembly)
                {
                    continue;
                }

                if (method.IsFinal)
                {
                    continue; // Skip virtual and sealed methods
                }

                if (!method.IsVirtual)
                {
                    throw new Exception($"Method '{method.Name}' on class '{typeof(T)}' must be marked as virtual or sealed.");
                }

                OverrideMethod(typeBuilder, method);
            }

            Type proxyType = typeBuilder.CreateType();

            // Find a constructor that matches the provided arguments
            // Create instance using the constructor we defined
            return (T)Activator.CreateInstance(proxyType, constructorArgs);
        }

        private static void OverrideMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            Type[] parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                method.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig,
                method.ReturnType,
                parameterTypes);

            ILGenerator il = methodBuilder.GetILGenerator();

            // Load ExceptionHandler.CaptureException method
            MethodInfo captureExceptionMethod = typeof(ExceptionHelper).GetMethod(
                "CaptureException",
                new[] { method.ReturnType == typeof(void) ? typeof(Action) : typeof(Func<Task>) });

            // Create delegate for base method
            il.Emit(OpCodes.Ldarg_0);
            for (int i = 1; i <= parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }

            il.Emit(OpCodes.Ldftn, method);
            il.Emit(OpCodes.Newobj, method.ReturnType == typeof(void) ?
                typeof(Action).GetConstructor(new[] { typeof(object), typeof(IntPtr) }) :
                typeof(Func<Task>).GetConstructor(new[] { typeof(object), typeof(IntPtr) }));

            // Call CaptureException
            il.EmitCall(OpCodes.Call, captureExceptionMethod, null);

            // Return
            il.Emit(OpCodes.Ret);
        }
    }
}
