using System.Data;

namespace XelberaSW.SpORM.Metadata
{
    public class ConnectionParameters: IConnectionParametersProcessor
    {
        public ConnectionParameters(IConnectionParameters parameters)
        {
            Timeout = parameters.Timeout;
        }

        public void Apply(IDbCommand command)
        {
            command.CommandTimeout = Timeout;
        }

        #region Implementation of IConnectionParametersContainer

        /// <inheritdoc />
        public int Timeout { get; set; }

        #endregion
    }
}