using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Scissors
{
    // Duktape uses NUL terminated UTF-8 strings throughout, this is
    // incompatible with P/Invoke and so must be done via Encoding.UTF8
    // TODO: Switch to using UTF8Marshaler - this will not be compatible with
    //       functions with a length parameter

    // We assume that duktape's duk_int_t and duk_small_int_t are both 32 bits
    // 32 vs 64 bit can be managed by using IntPtr and UIntPtr

    public enum Type : int
    {
        None = 0,
        Undefined = 1,
        Null = 2,
        Boolean = 3,
        Number = 4,
        String = 5,
        Object = 6,
        Buffer = 7,
        Pointer = 8
    }

    public enum TypeMask : uint
    {
        None = 1,
        Undefined = 2,
        Null = 4,
        Boolean = 8,
        Number = 16,
        String = 32,
        Object = 64,
        Buffer = 128,
        Pointer = 256,
        Throw = 1024
    }

    public enum CoercionHint : int
    {
        None = 0,
        String = 1,
        Number = 2
    }

    public enum EnumFlag : uint
    {
        IncludeNonEnumerable = 1,
        IncludeInternal = 2,
        OwnPropertiesOnly = 4,
        ArrayIndicesOnly = 8,
        SortArrayIndices = 16,
        NoProxyBehaviour = 32
    }

    public enum CompileFlag : uint
    {
        Eval = 1,
        Function = 2,
        String = 4,
        Safe = 8,
        NoResult = 16,
        NoSource = 32,
        StrLen = 64
    }

    public enum ThreadFlag : uint
    {
        NewGlobalEnv = 1
    }

    public enum StringPushFlag : uint
    {
        Safe = 1
    }

    public enum ErrorCode : int
    {
        Unimplemented = 50,
        Unsupported = 51,
        Interal = 52,
        Alloc = 53,
        Assertion = 54,
        Api = 55,
        Uncaught = 56,
        Error = 100,
        Eval = 101,
        Range = 102,
        Reference = 103,
        Syntax = 104,
        Type = 105,
        Uri = 106
    }

    public enum ReturnCode : int
    {
        Success = 0,
        Error = 1
    }

    public enum LogLevel : int
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5
    }

    public static class NativeMethods
    {
        private const string DLLNAME = "duktape.dll";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int CFunction(IntPtr ctx);

        // Memory allocation functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr AllocFunction(IntPtr udata, UIntPtr size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr ReallocFunction(IntPtr udata, IntPtr ptr, UIntPtr size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeFunction(IntPtr udata, IntPtr ptr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FatalFunction(IntPtr ctx, ErrorCode code, IntPtr msg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DecodeCharFunction(IntPtr udata, int codepoint);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int MapCharFunction(IntPtr udata, int codepoint);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SafeCallFunction(IntPtr ctx);

        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryFunctions
        {
            public AllocFunction Alloc;
            public ReallocFunction Realloc;
            public FreeFunction Free;
            public IntPtr Udata;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FunctionListEntry
        {
            public IntPtr Key;
            public CFunction Value;
            public int NArgs;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NumberListEntry
        {
            public IntPtr Key;
            public double Value;
        }

        // Context management

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_create_heap(
            AllocFunction alloc_func,
            ReallocFunction realloc_func,
            FreeFunction free_func,
            IntPtr udata,
            FatalFunction fatal_handler);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_destroy_heap(IntPtr ctx);

        // Memory management

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_alloc_raw(IntPtr ctx, UIntPtr size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_free_raw(IntPtr ctx, IntPtr ptr);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_realloc_raw(IntPtr ctx, IntPtr ptr, UIntPtr size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_alloc(IntPtr ctx, UIntPtr size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_free(IntPtr ctx, UIntPtr size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_realloc(IntPtr ctx, IntPtr ptr, UIntPtr size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_get_memory_functions(IntPtr ctx, out MemoryFunctions out_funcs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_gc(IntPtr ctx, uint flags);

        // Error handling

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_throw(IntPtr ctx);

        // Nasty undocumented hacks to call a vararg
        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_error_raw(IntPtr ctx, ErrorCode err_code, IntPtr filename, int line, IntPtr fmt, __arglist);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_fatal(IntPtr ctx, ErrorCode err_code, IntPtr err_msg);

        // Other state related functions

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_strict_call(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_constructor_call(IntPtr ctx);

        // Stack management

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_normalize_index(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_require_normalize_index(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_valid_index(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_require_valid_index(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_get_top(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_set_top(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_get_top_index(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_require_top_index(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_check_stack(IntPtr ctx, int extra);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_require_stack(IntPtr ctx, int extra);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_check_stack_top(IntPtr ctx, int top);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_require_stack_top(IntPtr ctx, int top);

        // Stack manipulation

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_swap(IntPtr ctx, int index1, int index2);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_swap_top(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_dup(IntPtr ctx, int from_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_dup_top(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_insert(IntPtr ctx, int to_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_replace(IntPtr ctx, int to_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_copy(IntPtr ctx, int from_index, int to_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_remove(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_xcopymove_raw(IntPtr to_ctx, IntPtr from_ctx, int count, bool is_copy);

        // Push operations

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_undefined(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_null(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_boolean(IntPtr ctx, bool val);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_true(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_false(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_number(IntPtr ctx, double val);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_nan(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_int(IntPtr ctx, int val);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_uint(IntPtr ctx, uint val);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_push_string(IntPtr ctx, IntPtr str);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_push_lstring(IntPtr ctx, IntPtr str, UIntPtr size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_pointer(IntPtr ctx, IntPtr p);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_push_sprintf(IntPtr ctx, IntPtr fmt, __arglist);

        // Unimplentable, last arg is va_list
        //[DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        //public static extern IntPtr duk_push_vsprintf(IntPtr ctx, IntPtr fmt, )

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_push_string_file_raw(IntPtr ctx, IntPtr path, StringPushFlag flags);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_this(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_current_function(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_current_thread(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_global_object(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_heap_stash(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_global_stash(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_thread_stash(IntPtr ctx, IntPtr target_ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_push_object(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_push_array(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_push_c_function(IntPtr ctx, CFunction func, int nargs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_push_thread_raw(IntPtr ctx, ThreadFlag flags);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_push_error_object_raw(IntPtr ctx, int err_code, IntPtr filename, int line, IntPtr fmt, __arglist);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_push_buffer(IntPtr ctx, UIntPtr size, bool dynamic);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_push_fixed_buffer(IntPtr ctx, UIntPtr size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_push_dynamic_buffer(IntPtr ctx, UIntPtr size);

        // Pop operations

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_pop(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_n(IntPtr ctx, int count);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_pop_2(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_pop_3(IntPtr ctx);

        // Type checks

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern Type duk_get_type(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_check_type(IntPtr ctx, int index, Type type);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern TypeMask duk_get_type_mask(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_check_type_mask(IntPtr ctx, int index, TypeMask mask);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_undefined(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_null(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_null_or_undefined(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_boolean(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_number(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_nan(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_string(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_object(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_buffer(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_pointer(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_array(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_function(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_c_function(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_ecmascript_function(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_bound_function(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_thread(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_callable(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_dynamic_buffer(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_fixed_buffer(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_is_primitive(IntPtr ctx, int index);

        // Get operations

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_get_boolean(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern double duk_get_number(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_get_int(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint duk_get_uint(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_get_string(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_get_lstring(IntPtr ctx, int index, UIntPtr out_len);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_get_buffer(IntPtr ctx, int index, UIntPtr out_size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_get_pointer(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern CFunction duk_get_c_function(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_get_context(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern UIntPtr duk_get_length(IntPtr ctx, int index);

        // Require operations

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_require_undefined(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_require_null(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_require_boolean(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern double duk_require_number(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_require_int(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint duk_require_uint(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_require_string(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_require_lstring(IntPtr ctx, int index, UIntPtr out_len);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_require_buffer(IntPtr ctx, int index, UIntPtr out_size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_require_pointer(IntPtr ctxc, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern CFunction duk_require_c_function(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_require_context(IntPtr ctx, int index);

        // Coercion operations

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_to_undefined(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_to_null(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_to_boolean(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern double duk_to_number(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_to_int(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint duk_to_uint(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_to_int32(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint duk_to_uint32(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt16 duk_to_uint16(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_to_string(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_to_lstring(IntPtr ctx, int index, UIntPtr out_len);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_to_buffer(IntPtr ctx, int index, UIntPtr out_size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_to_fixed_buffer(IntPtr ctx, int index, UIntPtr out_size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_to_dynamic_buffer(IntPtr ctx, int index, UIntPtr out_size);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_to_pointer(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_to_object(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_to_defaultvalue(IntPtr ctx, int index, CoercionHint hint);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_to_primitive(IntPtr ctx, int index, CoercionHint hint);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_safe_to_lstring(IntPtr ctx, int index, UIntPtr out_len);

        // Misc conversion

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_base64_encode(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_base64_decode(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_hex_encode(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_hex_decode(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_json_encode(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_json_decode(IntPtr ctx, int index);

        // Buffer

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr duk_resize_buffer(IntPtr ctx, int index, UIntPtr new_size);

        // Property access

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_get_prop(IntPtr ctx, int obj_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_get_prop_string(IntPtr ctx, int obj_index, IntPtr key);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_put_prop(IntPtr ctx, int obj_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_put_prop_string(IntPtr ctx, int obj_index, IntPtr key);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_put_prop_index(IntPtr ctx, int obj_index, uint arr_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_del_prop(IntPtr ctx, int obj_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_del_prop_string(IntPtr ctx, int obj_index, IntPtr key);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_del_prop_index(IntPtr ctx, int obj_index, uint arr_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_has_prop(IntPtr ctx, int obj_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_has_prop_string(IntPtr ctx, int obj_index, IntPtr key);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_has_prop_index(IntPtr ctx, int obj_index, uint arr_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_get_global_string(IntPtr ctx, IntPtr key);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_put_global_string(IntPtr ctx, IntPtr key);

        // Object prototype

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_get_prototype(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_set_prototype(IntPtr ctx, int index);

        // Object finalizer

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_get_finalizer(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_put_finalizer(IntPtr ctx, int index);

        // Global object

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_set_global_object(IntPtr ctx);

        // Duktape/C function magic value

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_get_magic(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_set_magic(IntPtr ctx, int index, int magic);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_get_current_magic(IntPtr ctx);

        // Module helpers: put multiple function or constant properties

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_put_function_list(IntPtr ctx, int obj_index, FunctionListEntry[] funcs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_put_number_list(IntPtr ctx, int obj_index, NumberListEntry[] numbers);

        // Variable access

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_get_var(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_put_var(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_del_var(IntPtr ctx);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_has_var(IntPtr ctx);

        // Object operations

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_compact(IntPtr ctx, int obj_index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_enum(IntPtr ctx, int obj_index, EnumFlag enum_flags);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_next(IntPtr ctx, int enum_index, bool get_value);

        // String manipulation

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_concat(IntPtr ctx, int count);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_join(IntPtr ctx, int count);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_decode_string(IntPtr ctx, int index, DecodeCharFunction callback, IntPtr udata);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_map_string(IntPtr ctx, int index, MapCharFunction callback, IntPtr udata);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_substring(IntPtr ctx, int index, UIntPtr start_char_offset, UIntPtr end_char_offset);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_trim(IntPtr ctx, int index);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_char_code_at(IntPtr ctx, int index, UIntPtr char_offset);

        // Ecmascript operators

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_equals(IntPtr ctx, int index1, int index2);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool duk_strict_equals(IntPtr ctx, int index1, int index2);

        // Function (method) calls

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_call(IntPtr ctx, int nargs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_call_method(IntPtr ctx, int nargs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_call_prop(IntPtr ctx, int obj_index, int nargs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode duk_pcall(IntPtr ctx, int nargs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode duk_pcall_method(IntPtr ctx, int nrags);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode duk_pcall_prop(IntPtr ctx, int obj_index, int nargs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_new(IntPtr ctx, int nargs);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode duk_safe_call(IntPtr ctx, SafeCallFunction func, int nargs, int nrets);

        // Compilations and evaluation

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_eval_raw(IntPtr ctx, IntPtr src_buffer, UIntPtr src_length, CompileFlag flags);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern int duk_compile_raw(IntPtr ctx, IntPtr src_buffer, UIntPtr src_length, CompileFlag flags);

        // Logging

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_log(IntPtr ctx, LogLevel level, IntPtr fmt, __arglist);

        // Debugging

        [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void duk_push_context_dump(IntPtr ctx);
    }
}
    