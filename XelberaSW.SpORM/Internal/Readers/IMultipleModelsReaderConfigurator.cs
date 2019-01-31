/*
 * Copyright (c) 2018 Xelbera (Stepan Matafonov)
 * All rights reserved.
 */

namespace XelberaSW.SpORM.Internal.Readers
{
    interface IMultipleModelsReaderConfigurator
    {
        bool OneRecordWasReadAlready { get; set; }
    }
}
