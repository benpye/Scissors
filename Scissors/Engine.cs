using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Scissors.NativeMethods;

namespace Scissors
{
    public class Engine : IDisposable
    {
        public IntPtr _ctx;

        private AllocFunction allocCallback;
        private ReallocFunction reallocCallback;
        private FreeFunction freeCallback;
        private FatalFunction fatalCallback;

        public Engine()
        {
            // Save these else our callbacks get GCd

            allocCallback = AllocateMemory;
            reallocCallback = ReallocateMemory;
            freeCallback = FreeMemory;
            fatalCallback = FatalFunction;

            _ctx = duk_create_heap(
                allocCallback,
                reallocCallback,
                freeCallback,
                IntPtr.Zero,
                fatalCallback
                );

            if (_ctx == IntPtr.Zero)
                throw new Exception("Could not initialize duktape heap");
        }

        // Memory allocation functions
        // This allows us to track the memory usage of duktape
        // TODO: Investigate having this in C or switching delegate to IntPtr
        //       to remove casts for size parameter

        private IntPtr AllocateMemory(IntPtr udata, UIntPtr size)
        {
            return Marshal.AllocHGlobal(size.ToIntPtr());
        }

        private IntPtr ReallocateMemory(IntPtr udata, IntPtr ptr, UIntPtr size)
        {
            if (ptr == IntPtr.Zero)
                return AllocateMemory(udata, size);

            return Marshal.ReAllocHGlobal(ptr, size.ToIntPtr());
        }

        private void FreeMemory(IntPtr udata, IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        private void FatalFunction(IntPtr ctx, ErrorCode code, IntPtr msg)
        {
            string str = MarshalHelper.StringFromNative(msg);
            throw new FatalEngineException(str, code);
        }

        private object MarshalArgument(Type t, int i)
        {
            if (t == typeof(bool))
                return duk_require_boolean(_ctx, i);
            else if (t == typeof(int))
                return duk_require_int(_ctx, i);
            else if (t == typeof(double))
                return duk_require_number(_ctx, i);
            else if (t == typeof(string))
                return duk_require_string(_ctx, i);
            else if (t == typeof(uint))
                return duk_require_uint(_ctx, i);
            else
                throw new NotImplementedException("No support for custom types.");
        }

        private object MarshalArgument(int i)
        {
            var t = duk_get_type(_ctx, i);

            switch(t)
            {
                case JSType.Boolean:
                    return duk_get_boolean(_ctx, i);
                case JSType.Null:
                    return null;
                case JSType.Number:
                    return duk_get_number(_ctx, i);
                case JSType.String:
                    return duk_get_string(_ctx, i);
                default:
                    throw new NotImplementedException("JS type \{t} not supported.");
            }
        }

        private int MarshalReturn(object o)
        {
            if (o == null)
                return 0;

            Type t = o.GetType();

            if (t == typeof(bool))
                duk_push_boolean(_ctx, (bool)o);
            else if (t == typeof(int))
                duk_push_int(_ctx, (int)o);
            else if (t == typeof(double))
                duk_push_number(_ctx, (double)o);
            else if (t == typeof(string))
                duk_push_string(_ctx, (string)o);
            else if (t == typeof(uint))
                duk_push_uint(_ctx, (uint)o);
            else
                throw new NotImplementedException("No support for custom types.");

            return 1;
        }

        // TODO: Investigate if doing this at runtime is sufficient or if we
        //       need to emit IL to do this at bind time
        //       IL generation is harder to maintain but likely faster

        public CFunction WrapMethod(Delegate method)
        {
            return new CFunction((IntPtr ctx) =>
            {
                var args = method.Method.GetParameters();

                // params is always the last arg, it will have this attribute
                bool varArgs = args[args.Length - 1].GetCustomAttributes(
                    typeof(ParamArrayAttribute), false).Length > 0;

                int n = args.Length;

                // Gets number of args passed, if vararg then this is on the
                // stack otherwise assume they called it right
                int varC = varArgs ? duk_get_top(_ctx) : n;

                object[] callArgs = new object[n];

                // Loop over C# parameters
                for (int i = 0; i < args.Length; i++)
                {
                    // If the last arg in a params method then loop over the
                    // remaining JS parameters
                    if (varArgs && (i + 1 == args.Length))
                    {
                        object[] pArr = new object[varC - i];
                        for (int j = 0; j < pArr.Length; j++)
                        {
                            pArr[j] = MarshalArgument(i);
                            i++;
                        }
                        callArgs[args.Length - 1] = pArr;
                    }
                    else
                        callArgs[i] = MarshalArgument(args[i].ParameterType,
                            i);
                }

                object ret = method.DynamicInvoke(callArgs);

                return MarshalReturn(ret);
            });
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).          
                }

                duk_destroy_heap(_ctx);

                _disposedValue = true;
            }
        }

        ~Engine()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
