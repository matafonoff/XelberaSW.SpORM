using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using XelberaSW.SpORM.Internal.DelegateBuilders;
using XelberaSW.SpORM.Utilities;

namespace XelberaSW.SpORM.Internal
{
    class DefaultParameterConverter : IParameterConverter
    {
        private readonly ValueConverter _valueConverter = new ValueConverter();

        /// <inheritdoc />
        object IParameterConverter.Convert(IDictionary<string, object> parameters)
        {
            throw new MissingMethodException(typeof(DefaultParameterConverter).FullName, nameof(Convert));
        }

        public ParameterExpression Convert(MethodContext methodContext, ParameterExpression[] parameters, MethodInfo caller)
        {
            /* pThis, complexArgument */
            if (parameters.Length == 2 &&
                parameters[1].Type.IsClass &&
                !parameters[1].Type.IsSimpleType())
            {
                return parameters[1];
            }

            var paramType = DbContextProxyHelper.GetParamType(caller);
            var param = methodContext.AddVariable(paramType, "dispArgs");

            methodContext.Do(param.Assign(Expression.New(paramType)));

            var properties = paramType.GetProperties();

            for (var i = 0; i < properties.Length; i++)
            {
                var propAccess = Expression.Property(param, properties[i]);
                var paramAccess = (Expression)parameters[i + 1];

                if (propAccess.Type != paramAccess.Type)
                {
                    paramAccess = _valueConverter.GetConvertValueExpression(paramAccess, propAccess.Type);
                }

                methodContext.Do(Expression.Assign(propAccess, paramAccess));
            }


            return param;
        }
    }

}
