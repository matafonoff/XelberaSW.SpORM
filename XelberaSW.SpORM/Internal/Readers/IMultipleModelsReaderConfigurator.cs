namespace XelberaSW.SpORM.Internal.Readers
{
    interface IMultipleModelsReaderConfigurator
    {
        bool OneRecordWasReadAlready { get; set; }
    }
}