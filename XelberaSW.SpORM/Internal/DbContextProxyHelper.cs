/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using XelberaSW.SpORM.Internal.DelegateBuilders;
using XelberaSW.SpORM.Metadata;
using XelberaSW.SpORM.Utilities;

// ReSharper disable InconsistentlySynchronizedField
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace XelberaSW.SpORM.Internal
{
    public interface IGeneratedArgumentsContainer
    {
        IConnectionParametersProcessor ConnectionParameters { get; }
    }

    class DbContextProxyHelper
    {
        private static readonly ModuleBuilder _module;
        private readonly MethodInfo _dynamicInvoke = typeof(Delegate).GetMethod(nameof(Delegate.DynamicInvoke), BindingFlags.Public | BindingFlags.Instance);
        private static readonly Dictionary<string, MethodInfo> _proxies = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);

        private static readonly byte[] _pushInstructions = Enumerable.Range(0x50, 10).Select(x => (byte)x)
                                                                     .Append<byte>(0x6a, 0x68, 0xff, 0x0f)
                                                                     .ToArray();

        private static readonly AssemblyBuilder _assembly;

        static DbContextProxyHelper()
        {
            var assmName = new AssemblyName(nameof(DbContextProxyHelper) + "_" + Guid.NewGuid() + ".dll")
            {
                Version = new Version(0, 1),
                Flags = AssemblyNameFlags.EnableJITcompileOptimizer
            };

#if NETCOREAPP2_0 || !DEBUG
            _assembly = AssemblyBuilder.DefineDynamicAssembly(assmName, AssemblyBuilderAccess.Run);
            _module = _assembly.DefineDynamicModule(assmName.Name);
#else
            var _assembly = AssemblyBuilder.DefineDynamicAssembly(assmName, AssemblyBuilderAccess.RunAndSave);
            _module = _assembly.DefineDynamicModule(assmName.Name);
            _assembly.DefineVersionInfoResource("SpORM.Dynamic", "0.1-alpha", "ratek", "Xelbera SW", "SpORM by XelberaSW");
#endif

            _assembly.AddCustomAttribute<SecurityRulesAttribute, SecurityRuleSet>(SecurityRuleSet.Level1);
        }

        public static MethodInfo GetExistingProxy(MethodInfo caller)
        {
            var typeName = GetTypeName(caller);

            return Type.GetType(typeName)?.GetMethod(caller.Name, BindingFlags.Static | BindingFlags.Public);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public MethodInfo CreateProxy(MethodInfo caller, ILogger logger)
        {
            var typeName = GetTypeName(caller);

            var fullTypeName = "XelberaSW." + typeName;
            return _proxies.GetOrAddValueSafe(fullTypeName, x => CreateProxyInternal(caller, logger, x));
        }

        private MethodInfo CreateProxyInternal(MethodInfo caller, ILogger logger, string fullTypeName)
        {
            var methodName = caller.Name;
            var proxyType = Type.GetType(fullTypeName) ??
                            CreateProxyTypeInternal(caller, fullTypeName, methodName);

            var mi = proxyType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

            RuntimeHelpers.PrepareMethod(mi.MethodHandle);

            logger.LogDebug($"Replacing method {caller.DeclaringType.FullName}.{caller.Name} with {mi.DeclaringType.FullName}.{mi.Name}");
            RedirectMethodCall(caller, mi, logger);

            logger.LogInformation($"All calls to {caller.DeclaringType.FullName}.{caller.Name} are replaced by calls to {mi.DeclaringType.FullName}.{mi.Name}");


            return mi;
        }

        private void RedirectMethodCall(MethodInfo source, MethodInfo target, ILogger logger)
        {
            RuntimeHelpers.PrepareMethod(source.MethodHandle);
            RuntimeHelpers.PrepareMethod(target.MethodHandle);

            var pSrcMethod = source.MethodHandle.GetFunctionPointer();
            var pTargetMethod = target.MethodHandle.GetFunctionPointer();

            IntPtr pBody;

            //#if !DEBUG
            //            pSrcMethod += 2;
            //#endif
            const int bytesCount = 40;

            logger.LogTrace($@"Dump of {bytesCount} bytes of source (0x{pSrcMethod.ToInt64():x8}): {GetDump(pSrcMethod, bytesCount)}
Dump of {bytesCount} bytes of target (0x{pTargetMethod.ToInt64():x8}): {GetDump(pTargetMethod, bytesCount)}");

            var instruction = Marshal.ReadByte(pSrcMethod);

            if (instruction == 0xe9)
            {
                pBody = pSrcMethod + 5 + Marshal.ReadInt32(pSrcMethod + 1);
            }
            else if (_pushInstructions.Contains(instruction))
            {
                pBody = pSrcMethod;
            }
            else
            {
                Trace.WriteLine("Some shit happened!");
                pBody = pSrcMethod;
            }

            var offset = (int)(pTargetMethod.ToInt64() - pBody.ToInt64() - 5);

            var arr = Enumerable.Repeat((byte)0xe9, 1)
                                .Concat(BitConverter.GetBytes(offset))
                                .Concat(Enumerable.Repeat((byte)0xcc, 9)).ToArray();

            Marshal.Copy(arr, 0, pBody, arr.Length);
        }

        private static string GetDump(IntPtr ptr, int byteCount)
        {
            var buff = new byte[byteCount];

            Marshal.Copy(ptr, buff, 0, byteCount);

            return String.Join(" ", buff.Select(x => x.ToString("x2")));
        }

        private Type CreateProxyTypeInternal(MethodInfo caller, string fullTypeName, string methodName)
        {
            var typeBuilder = _module.DefineType(fullTypeName);

            var methodParameters = new[]
            {
                caller.DeclaringType
            }.Concat(caller.GetParameters().Select(x => x.ParameterType)).ToArray();

            var impl = DelegateBuilderBase.Build(caller);
            var implType = impl.GetType();

            var dataProviderField = typeBuilder.DefineField("_dataProvider", implType, FieldAttributes.Private | FieldAttributes.Static);

            var method = typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static, caller.ReturnType, methodParameters);
            method.SetImplementationFlags(MethodImplAttributes.NoInlining);

            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldsfld, dataProviderField);

            il.EmitLdc(methodParameters.Length);
            il.Emit(OpCodes.Newarr, typeof(object));

            for (int i = 0; i < methodParameters.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                il.EmitLdc(i);
                il.EmitLdarg(i);

                if (methodParameters[i].IsValueType)
                {
                    il.Emit(OpCodes.Box, methodParameters[i]);
                }

                il.Emit(OpCodes.Stelem_Ref);
            }

            il.Emit(OpCodes.Call, _dynamicInvoke);

            // TODO Replace dynamic invoke with direct invoke
            //for (int i = 0; i < methodParameters.Length; i++)
            //{
            //    il.EmitLdarg(i);
            //}

            //var invoke = implType.GetMethod("Invoke");
            //il.Emit(OpCodes.Callvirt, invoke);

            il.Emit(caller.ReturnType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, caller.ReturnType);

            il.Emit(OpCodes.Ret);

            var proxyType = typeBuilder.CreateType();

            var fld = proxyType.GetField(dataProviderField.Name, BindingFlags.NonPublic | BindingFlags.Static);
            fld.SetValue(null, impl);
            return proxyType;
        }

        public static Type GetParamType(MethodInfo caller)
        {
            var name = GetParamTypeName(caller);

            var type = Type.GetType(name);
            if (type == null)
            {
                lock (caller)
                {
                    type = Type.GetType(name);
                    if (type == null)
                    {
                        var typeBuilder = _module.DefineType(name, TypeAttributes.Class | TypeAttributes.Public);

                        var parameters = caller.GetParameters();

                        if (parameters.Length > 0 &&
                            parameters[parameters.Length - 1].ParameterType == typeof(CancellationToken))
                        {
                            Array.Resize(ref parameters, parameters.Length - 1);
                        }

                        foreach (var param in parameters)
                        {
                            var paramAttr = param.GetCustomAttribute<ParameterAttribute>();

                            var propName = paramAttr?.Name;
                            if (String.IsNullOrWhiteSpace(propName))
                            {
                                propName = Char.ToUpper(param.Name[0]) + param.Name.Substring(1);
                            }

                            var propType = paramAttr?.Type;
                            if (propType == null)
                            {
                                propType = param.ParameterType;
                            }

                            AddProperty(typeBuilder, propName, propType);
                        }

                        var prop = AddTimeoutField(typeBuilder);
                        ImplementGeneratedArgumentsContainer(typeBuilder, prop);

                        type = typeBuilder.CreateType();
                    }
                }
            }

            return type;
        }

        private static void ImplementGeneratedArgumentsContainer(TypeBuilder typeBuilder, PropertyBuilder backingProperty)
        {
            var ifaceType = typeof(IGeneratedArgumentsContainer);

            typeBuilder.AddInterfaceImplementation(ifaceType);

            var getter = backingProperty.GetMethod;

            var targetGetter = ifaceType.GetProperty(nameof(IGeneratedArgumentsContainer.ConnectionParameters)).GetMethod;

            var method = typeBuilder.DefineMethod($"{nameof(IGeneratedArgumentsContainer)}.{getter.Name}",
                                                    MethodAttributes.Private | MethodAttributes.HideBySig |
                                                    MethodAttributes.NewSlot | MethodAttributes.Virtual |
                                                    MethodAttributes.Final, 
                                                    targetGetter.CallingConvention, targetGetter.ReturnType, Type.EmptyTypes);
            var il = method.GetILGenerator();

            il.EmitLdarg(0); // this
            il.Emit(OpCodes.Call, backingProperty.GetMethod);
            il.Emit(OpCodes.Castclass, typeof(IConnectionParametersProcessor));
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(method, targetGetter);
        }

        private const string DB_CONNECION_PARAMETERS = "<>$db$connectionParameters>";

        private static PropertyBuilder AddTimeoutField(TypeBuilder typeBuilder)
        {
            var propType = typeof(ConnectionParameters);
            var propName = DB_CONNECION_PARAMETERS;

            var propBuilder = AddProperty(typeBuilder, propName, propType, false);

            var ctor = typeof(XmlIgnoreAttribute).GetConstructor(Type.EmptyTypes);
            propBuilder.SetCustomAttribute(new CustomAttributeBuilder(ctor, Array.Empty<object>()));

            return propBuilder;
        }

        private static PropertyBuilder AddProperty(TypeBuilder typeBuilder, string propName, Type propType, bool isPublic = true)
        {
            var field = typeBuilder.DefineField("_" + propName, propType, FieldAttributes.Private);

            var attributes = MethodAttributes.HideBySig;
            if (isPublic)
            {
                attributes |= MethodAttributes.Public;
            }
            else
            {
                attributes |= MethodAttributes.Private;
            }

            var getter = typeBuilder.DefineMethod("get_" + propName, attributes, propType, Type.EmptyTypes);
            {
                var il = getter.GetILGenerator();
                il.EmitLdarg(0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);
            }

            var setter = typeBuilder.DefineMethod("set_" + propName, attributes, typeof(void), new[] { propType });
            {
                var il = setter.GetILGenerator();
                il.EmitLdarg(0);
                il.EmitLdarg(1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);
            }

            var propBuilder = typeBuilder.DefineProperty(propName, PropertyAttributes.None, propType, Type.EmptyTypes);
            propBuilder.SetGetMethod(getter);
            propBuilder.SetSetMethod(setter);
            return propBuilder;
        }

        private static string GetParamTypeName(MethodInfo caller)
        {
            return GetTypeName(caller) + "[Args]";
        }

        private static string GetTypeName(MethodInfo caller)
        {
            return $"{caller.DeclaringType.FullName}::{caller.Name}";
        }

        public static PropertyInfo GetConnectionParamsProperty(Type type)
        {
            return type.GetProperty(DB_CONNECION_PARAMETERS, BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
