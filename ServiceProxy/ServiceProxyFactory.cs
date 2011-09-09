using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
namespace ServiceProxy
{
    /// <summary>
    /// A factory class that creates proxy types that will have their target created when the proxy type is instantiated. 
    /// </summary>
    public class ServiceProxyFactory
    {
        private readonly ConcurrentDictionary<Type, Type> _proxyCache = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Creates a proxy type that creates the actual target using the given <paramref name="targetFactory"/>.
        /// </summary>
        /// <param name="serviceContractType">The service contract for which to create a proxy type.</param>
        /// <param name="targetFactory">The function delegate that will be used to create the invocation target.</param>
        /// <returns></returns>
        public Type GetProxyType(Type serviceContractType, Func<object> targetFactory)
        {
            if (!serviceContractType.IsInterface)
                throw new ArgumentOutOfRangeException("serviceContractType","The service type must be an interface");
            return _proxyCache.GetOrAdd(serviceContractType, s => CreateProxyType(serviceContractType, targetFactory));
        }

        private static Type CreateProxyType(Type serviceType, Func<object> targetFactory)
        {
            TypeBuilder typeBuilder = GetTypeBuilder(serviceType);
            FieldBuilder targetFactoryField = DefineTargetFactoryField(typeBuilder);
            FieldBuilder targetField = DefineTargetField(serviceType, typeBuilder);
            ImplementParameterlessConstructor(serviceType, typeBuilder, targetFactoryField, targetField);
            ImplementMethods(serviceType, typeBuilder, targetField);
            Type proxyType = typeBuilder.CreateType();
            AssignTargetFactory(proxyType, targetFactory);
            return proxyType;
        }

        private static void AssignTargetFactory(Type proxyType, Func<object> targetFactory)
        {
            proxyType.InvokeMember("TargetFactory", BindingFlags.Public | BindingFlags.Static | BindingFlags.SetField, null, null,
                                   new object[] { targetFactory });
        }

        private static FieldBuilder DefineTargetField(Type serviceContractType, TypeBuilder typeBuilder)
        {
            return typeBuilder.DefineField("_target", serviceContractType, FieldAttributes.InitOnly | FieldAttributes.Private);
        }

        private static void ImplementParameterlessConstructor(Type serviceType, TypeBuilder typeBuilder, FieldBuilder targetFactoryField, FieldBuilder targetField)
        {
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, CallingConventions.Standard, Type.EmptyTypes);
            ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldsfld, targetFactoryField);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Func<object>).GetMethod("Invoke"));
            ilGenerator.Emit(OpCodes.Castclass, serviceType);
            ilGenerator.Emit(OpCodes.Stfld, targetField);
            ilGenerator.Emit(OpCodes.Ret);

        }

        private static FieldBuilder DefineTargetFactoryField(TypeBuilder typeBuilder)
        {
            return typeBuilder.DefineField("TargetFactory", typeof(Func<object>),
                                           FieldAttributes.Public | FieldAttributes.Static);
        }

        private static void ImplementMethods(Type serviceContractType, TypeBuilder typeBuilder, FieldBuilder targetField)
        {
            foreach (MethodInfo targetMethod in GetTargetMethods(serviceContractType))
            {
                MethodBuilder methodBuilder = GetMethodBuilder(typeBuilder, targetMethod);
                ILGenerator ilGenerator = methodBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, targetField);
                for (int i = 1; i <= targetMethod.GetParameters().Length; ++i)
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                ilGenerator.Emit(OpCodes.Callvirt, targetMethod);
                ilGenerator.Emit(OpCodes.Ret);
            }
        }

        private static MethodBuilder GetMethodBuilder(TypeBuilder typeBuilder, MethodInfo targetMethod)
        {
            return typeBuilder.DefineMethod(targetMethod.Name, targetMethod.Attributes ^ MethodAttributes.Abstract,
                                            targetMethod.ReturnType,
                                            targetMethod.GetParameters().Select(p => p.ParameterType).ToArray());
        }

        private static TypeBuilder GetTypeBuilder(Type baseType)
        {
            return GetModuleBuilder().DefineType(baseType.Name + Guid.NewGuid(), TypeAttributes.Public | TypeAttributes.Class, null, new[] { baseType });
        }

        private static ModuleBuilder GetModuleBuilder()
        {
            AssemblyBuilder assemblybuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            return assemblybuilder.DefineDynamicModule(Guid.NewGuid().ToString());
        }

        private static IEnumerable<MethodInfo> GetTargetMethods(Type serviceType)
        {
            return serviceType.GetMethods();
        }
    }
}
