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
        public static IntPtr MarshalString(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            Array.Resize(ref bytes, bytes.Length + 1);
            bytes[bytes.Length - 1] = 0;
            var ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            return ptr;
        }

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

        static void Main(string[] args)
        {
            var e = new Engine();
            var ctx = e._ctx;

            var code = MarshalString("print('Hello world!');");
            var code2 = MarshalString("print('2+3=' + mod.addder(2, 3));");
            var file = MarshalString("Program.cs");
            var mod = MarshalString("mod");
            var adder = MarshalString("adder");

            duk_push_string(ctx, file);
            duk_eval_raw(ctx, code, UIntPtr.Zero, CompileFlag.Eval | CompileFlag.NoSource | CompileFlag.StrLen);

            duk_push_global_object(ctx);
            duk_push_object(ctx);
            FunctionListEntry[] fs = new FunctionListEntry[] {
                new FunctionListEntry() { Key = adder, Value = Adder, NArgs = (int)(-1) },
                new FunctionListEntry() { Key = IntPtr.Zero, Value = null, NArgs = 0 }
            };
            duk_put_function_list(ctx, -1, fs);
            duk_put_prop_string(ctx, -2, mod);
            //duk_push_c_function(ctx, Adder, (int)(-1));
            //duk_put_prop_string(ctx, -2, adder);
            duk_pop(ctx);

            duk_push_string(ctx, file);
            duk_eval_raw(ctx, code2, UIntPtr.Zero, CompileFlag.Eval | CompileFlag.NoSource | CompileFlag.StrLen);
            duk_pop(ctx);

            duk_destroy_heap(ctx);

            Marshal.FreeHGlobal(code);
            Marshal.FreeHGlobal(code2);
            Marshal.FreeHGlobal(file);
            Marshal.FreeHGlobal(mod);

            Console.ReadLine();
        }
    }
}
