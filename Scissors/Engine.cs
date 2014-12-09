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

        public Engine()
        {
            _ctx = duk_create_heap(
                AllocateMemory,
                ReallocateMemory,
                FreeMemory,
                IntPtr.Zero,
                FatalFunction
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
            string str = MarshalHelper.NativeToUTF8(msg);
            throw new FatalEngineException(str, code);
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
