﻿/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System;

namespace XelberaSW.SpORM.Internal.Readers
{
    public interface IReader<in TSource, out TResult>
        //where TSource:IDataRecord
    {
        TResult Read(TSource data);

        Func<TSource, TResult> GetReader();
    }
}
