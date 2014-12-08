using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Scissors
{
    class UTF8Marshaler : ICustomMarshaler
    {
        public void CleanUpManagedData(object ManagedObj)
        {
            return;
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData != IntPtr.Zero)
                Marshal.FreeHGlobal(pNativeData);
        }

        public int GetNativeDataSize()
        {
            return IntPtr.Size;
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj == null)
                return IntPtr.Zero;
            else if (!(ManagedObj is string))
                throw new MarshalDirectiveException("\{nameof(UTF8Marshaler)} must be used on a string.");

            return MarshalHelper.UTF8ToNative(ManagedObj as string);
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return MarshalHelper.NativeToUTF8(pNativeData);
        }

        private ICustomMarshaler _marshaler = null;
        public ICustomMarshaler GetInstance(string cookie)
        {
            if (_marshaler == null)
                _marshaler = new UTF8Marshaler();

            return _marshaler;
        }
    }
}
