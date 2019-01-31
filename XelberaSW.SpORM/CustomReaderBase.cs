/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System.Data;

namespace XelberaSW.SpORM
{
    public abstract class CustomReaderBase
    {
        public abstract void Read(object obj, IDataRecord record);
    }
}
