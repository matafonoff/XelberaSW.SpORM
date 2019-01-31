/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;
using System.Data;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SpParameterAttribute : Attribute
    {
        public SpParameterAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!name.StartsWith("@"))
            {
                name = "@" + name;
            }

            Name = name;
        }

        public string Name { get; }
        public DbType Type { get; set; }
        public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public int Size { get; set; }

        public bool ErrorInformation { get; set; }

    }
}
