﻿/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System.Data;

namespace XelberaSW.SpORM.Metadata
{
    public interface IConnectionParametersProcessor: IConnectionParameters
    {
        void Apply(IDbCommand command);
    }
}
