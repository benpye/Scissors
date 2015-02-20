using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scissors;
using static Scissors.NativeMethods;
using System.Runtime.InteropServices;

namespace Scissors.Hello
{
    class Program
    {
        static double Adder(params object[] objs)
        {
            double t = 0.0;
            foreach(var d in objs)
            {
                t += (double)d;
            }

            return t;
        }

        static void Print(string fmt, params object[] o)
        {
            Console.WriteLine(fmt, o);
        }

        static void Main(string[] args)
        {
            var e = new Engine();
            var ctx = e._ctx;

            var code = MarshalHelper.StringToNative("print('Hello world!');");
            var code2 = MarshalHelper.StringToNative("mod.printer('2+3={0}', mod.adder(2,3));mod.printer('hi {0}', 0 - 'a');");
            var adder = MarshalHelper.StringToNative("adder");
            var printer = MarshalHelper.StringToNative("printer");

            duk_push_string(ctx, "Program.cs");
            duk_eval_raw(ctx, code, UIntPtr.Zero, CompileFlag.Eval | CompileFlag.NoSource | CompileFlag.StrLen);

            duk_push_global_object(ctx);
            duk_push_object(ctx);
            // This is likely unsafe our delegates could probably be removed
            FunctionListEntry[] fs = new FunctionListEntry[] {
                new FunctionListEntry() { Key = adder, Value = e.WrapMethod(new Func<object[], double>(Adder)), NArgs = (int)(-1) },
                new FunctionListEntry() { Key = printer, Value = e.WrapMethod(new Action<string, object[]>(Print)), NArgs = (int)(-1) },
                new FunctionListEntry() { Key = IntPtr.Zero, Value = null, NArgs = 0 }
            };
            duk_put_function_list(ctx, -1, fs);
            duk_put_prop_string(ctx, -2, "mod");
            //duk_push_c_function(ctx, Adder, (int)(-1));
            //duk_put_prop_string(ctx, -2, adder);
            duk_pop(ctx);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            duk_push_string(ctx, "Program.cs");
            duk_eval_raw(ctx, code2, UIntPtr.Zero, CompileFlag.Eval | CompileFlag.NoSource | CompileFlag.StrLen);
            duk_pop(ctx);

            e.Dispose();

            Marshal.FreeHGlobal(code);
            Marshal.FreeHGlobal(code2);
            Marshal.FreeHGlobal(adder);
            Marshal.FreeHGlobal(printer);

            Console.ReadLine();
        }
    }
}
