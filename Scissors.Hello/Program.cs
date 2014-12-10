using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scissors;
using Scissors.NativeMethods;
using System.Runtime.InteropServices;

namespace Scissors.Hello
{
    class Program
    {
        static int Adder(IntPtr ctx)
        {
            int i;
            int n = duk_get_top(ctx);
            double res = 0.0;

            for(i = 0; i < n; i++)
            {
                res += duk_to_number(ctx, i);
            }

            duk_push_number(ctx, res);
            return 1;
        }

        static double Add(double a, double b)
        {
            return a + b;
        }

        static void Print(string str)
        {
            Console.WriteLine("Printing: \{str}");
        }

        static void Main(string[] args)
        {
            var e = new Engine();
            var ctx = e._ctx;

            var code = MarshalHelper.UTF8ToNative("print('Hello world!');");
            var code2 = MarshalHelper.UTF8ToNative("mod.printer('2+3=' + mod.adder(2, 3));");
            var adder = MarshalHelper.UTF8ToNative("adder");
            var printer = MarshalHelper.UTF8ToNative("printer");

            duk_push_string(ctx, "Program.cs");
            duk_eval_raw(ctx, code, UIntPtr.Zero, CompileFlag.Eval | CompileFlag.NoSource | CompileFlag.StrLen);

            duk_push_global_object(ctx);
            duk_push_object(ctx);
            FunctionListEntry[] fs = new FunctionListEntry[] {
                new FunctionListEntry() { Key = adder, Value = e.WrapMethod(new Func<double, double, double>(Add)), NArgs = 2 },
                new FunctionListEntry() { Key = printer, Value = e.WrapMethod(new Action<string>(Print)), NArgs = 1 },
                new FunctionListEntry() { Key = IntPtr.Zero, Value = null, NArgs = 0 }
            };
            duk_put_function_list(ctx, -1, fs);
            duk_put_prop_string(ctx, -2, "mod");
            //duk_push_c_function(ctx, Adder, (int)(-1));
            //duk_put_prop_string(ctx, -2, adder);
            duk_pop(ctx);

            duk_push_string(ctx, "Program.cs");
            duk_eval_raw(ctx, code2, UIntPtr.Zero, CompileFlag.Eval | CompileFlag.NoSource | CompileFlag.StrLen);
            duk_pop(ctx);

            duk_destroy_heap(ctx);

            Marshal.FreeHGlobal(code);
            Marshal.FreeHGlobal(code2);
            Marshal.FreeHGlobal(adder);
            Marshal.FreeHGlobal(printer);

            Console.ReadLine();
        }
    }
}
