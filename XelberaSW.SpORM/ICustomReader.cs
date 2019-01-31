/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System.Data;

namespace XelberaSW.SpORM
{
    public interface ICustomReader
    {
        void Read(IDataRecord record);
    }
}
