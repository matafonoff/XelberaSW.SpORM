using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace XelberaSW.SpORM.Internal
{
    static class Extensions
    {
        private static readonly MethodInfo _writeLine = typeof(Trace).GetMethod(nameof(Trace.WriteLine), BindingFlags.Public | BindingFlags.Static, null, new[]
        {
            typeof(object)
        }, null);

        public static void ConsoleWriteLine(this ILGenerator il, string text)
        {
            il.Emit(OpCodes.Ldstr, text);
            il.Emit(OpCodes.Call, _writeLine);
        }

        public static void EmitLdarg(this ILGenerator il, int value)
        {
            switch (value)
            {
                case 0: il.Emit(OpCodes.Ldarg_0); break;
                case 1: il.Emit(OpCodes.Ldarg_1); break;
                case 2: il.Emit(OpCodes.Ldarg_2); break;
                case 3: il.Emit(OpCodes.Ldarg_3); break;
                default: il.Emit(OpCodes.Ldarg, value); break;
            }
        }

        public static void EmitLdc(this ILGenerator il, int value)
        {
            switch (value)
            {
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default: il.Emit(OpCodes.Ldc_I4, value); break;
            }
        }
    }
}
