/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

using System.Collections.Generic;

namespace XelberaSW.SpORM
{
    public interface IParameterConverter
    {
        object Convert(IDictionary<string, object> parameters);
    }
}
