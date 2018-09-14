using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace XelberaSW.SpORM.Internal.DelegateBuilders
{
    class MethodContext
    {
        public List<Expression> Expressions { get; } = new List<Expression>();
        public IReadOnlyList<ParameterExpression> Variables { get; } = new List<ParameterExpression>();
        public IReadOnlyList<ParameterExpression> Parameters { get; } = new List<ParameterExpression>();
        public MethodInfo DispatcherMethod { get; }
        public ConstantExpression ActionNameConstant { get; }
        //public ParameterExpression CancellationToken { get; private set; }

        private ParameterExpression _return;
        private bool _isSealed;
        public ParameterExpression This { get; private set; }

        public LabelTarget EndOfMethod { get; } = Expression.Label();

        public Type ReturnType { get; }

        public Type TaskType { get; }

        public bool IsAsync => TaskType != null;


        private void EnsureNotSealed()
        {
            if (_isSealed)
            {
                throw new InvalidOperationException("MethodContext is already sealed. You can not modify this instance.");
            }
        }

        public ParameterExpression Return<T>(string name = null)
        {
            return Return(typeof(T), name);
        }
        public ParameterExpression Return(Type t, string name = null)
        {
            if (_return != null)
            {
                throw new InvalidOperationException();
            }

            _return = AddVariable(t, name ?? "return");
            return _return;
        }

        public ParameterExpression AddVariable(Type t, string name = null)
        {
            var expr = string.IsNullOrWhiteSpace(name) ? Expression.Variable(t) : Expression.Variable(t, name);

            ((List<ParameterExpression>)Variables).Add(expr);

            return expr;
        }

        public ParameterExpression AddVariable<T>(string name = null)
        {
            return AddVariable(typeof(T), name);
        }
        public MethodContext(MethodInfo dispatcherMethod, string actionName, Type returnType)
        {
            DispatcherMethod = dispatcherMethod;
            ActionNameConstant = Expression.Constant(actionName);

            if (typeof(Task).IsAssignableFrom(returnType))
            {
                if (returnType.IsGenericType)
                {
                    returnType = returnType.GetGenericArguments().First();

                    TaskType = typeof(Task<>).MakeGenericType(returnType);
                }
                else
                {
                    TaskType = typeof(Task);
                    returnType = typeof(void);
                }
            }
            ReturnType = returnType;
        }

        public void Do(Expression expression)
        {
            Expressions.Add(expression);
        }

        public BlockExpression Build(ParameterExpression ret = null)
        {
            if (_return != null &&
                ret != null &&
                ret != _return)
            {
                throw new InvalidOperationException();
            }

            if (ret == null)
            {
                ret = _return;
            }

            var exrpessions = Expressions.ToList();
            var variables = Variables.ToList();

            exrpessions.Add(Expression.Label(EndOfMethod));

            if (ret != null)
            {
                exrpessions.Add(ret);

                if (!variables.Contains(ret))
                {
                    variables.Add(ret);
                }
            }

            var body = Expression.Block(variables.Distinct().ToArray(), exrpessions);
            return body;
        }

        public MethodContext AttachParameters(ParameterExpression[] parameters)
        {
            EnsureNotSealed();

            ((List<ParameterExpression>)Parameters).AddRange(parameters);
            return this;
        }

        public MethodContext UseThis(ParameterExpression pThis)
        {
            EnsureNotSealed();

            This = pThis;
            return this;
        }

        public MethodContext Seal()
        {
            //if (Parameters.Count > 0)
            //{
            //    var lastParameter = Parameters[Parameters.Count - 1];
            //    if (lastParameter.Type == typeof(CancellationToken))
            //    {
            //        CancellationToken = lastParameter;
            //        ((List<ParameterExpression>)Parameters).RemoveAt(Parameters.Count - 1);
            //    }
            //}

            _isSealed = true;
            return this;
        }
    }
}