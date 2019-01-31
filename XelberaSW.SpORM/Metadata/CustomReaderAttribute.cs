/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Data;
using System.Reflection;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomReaderAttribute : Attribute
    {
        public Type ReaderType { get; }
        public static MethodInfo ReadMethod { get; } = typeof(CustomReaderBase).GetMethod(nameof(CustomReaderBase.Read));

        public CustomReaderAttribute(Type readerType)
        {
            if (!typeof(CustomReaderBase).IsAssignableFrom(readerType))
            {
                throw new ArgumentException($"Parameter {nameof(readerType)} must inherit from {typeof(CustomReaderBase).FullName}");
            }

            if (readerType.IsAbstract)
            {
                throw new ArgumentException($"Parameter {nameof(readerType)} must be non-abstract class");
            }

            if (readerType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException($"Parameter {nameof(readerType)} must be a class with default constructor available");
            }

            ReaderType = readerType;
        }

        private class ExplicitReader : CustomReaderBase
        {
            /// <inheritdoc />
            public override void Read(object obj, IDataRecord record)
            {
                ((ICustomReader)obj).Read(record);
            }
        }


        internal static CustomReaderBase GetReader(Type t)
        {
            var readerType = t.GetCustomAttribute<CustomReaderAttribute>()?.ReaderType;

            if (readerType == null)
            {
                if (typeof(ICustomReader).IsAssignableFrom(t))
                {
                    return new ExplicitReader();
                }

                return null;
            }

            return (CustomReaderBase) Activator.CreateInstance(t);
        }

        internal static CustomReaderBase GetReader<T>()
        {
            return GetReader(typeof(T));
        }
    }


}
