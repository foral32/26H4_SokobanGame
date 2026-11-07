using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BoxOrVoid.Study.LevelEditing
{
    // Windows Common Item Dialog. The Unity window remains responsive while the
    // shell runs its modal message loop on a dedicated single-threaded apartment.
    internal static class WindowsLevelPicker
    {
        public static Task<string> PickAsync(bool folders, string initialFolder)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var completion = new TaskCompletionSource<string>();
            IntPtr owner = GetActiveWindow();
            var thread = new Thread(() =>
            {
                try { completion.SetResult(Select(owner, folders, initialFolder)); }
                catch (Exception error) { completion.SetException(error); }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return completion.Task;
#else
            throw new PlatformNotSupportedException();
#endif
        }

        // Slots in the Windows SDK IFileDialog (including IUnknown and
        // IModalWindow) and IShellItem ABIs. Use native interfaces directly to
        // avoid depending on managed COM interop support in the Unity player.
        private enum DialogSlot { Show=3, SetFileTypes=4, SetOptions=9, GetOptions=10, SetFolder=12, SetTitle=17, GetResult=20 }
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int PointerArgument(IntPtr self, IntPtr argument);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int SetFlags(IntPtr self, uint flags);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetFlags(IntPtr self, out uint flags);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int SetFilter(IntPtr self, uint count, ref Filter filter);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetItem(IntPtr self, out IntPtr item);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetName(IntPtr self, uint kind, out IntPtr name);
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] private struct Filter { public string Name, Pattern; }
        [DllImport("user32")] private static extern IntPtr GetActiveWindow();
        [DllImport("ole32")] private static extern int CoInitializeEx(IntPtr reserved, uint mode);
        [DllImport("ole32")] private static extern void CoUninitialize();
        [DllImport("ole32")] private static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid, out IntPtr instance);
        [DllImport("shell32", CharSet=CharSet.Unicode, ExactSpelling=true)] private static extern int SHCreateItemFromParsingName(string path, IntPtr context, ref Guid iid, out IntPtr item);

        private static T Method<T>(IntPtr instance, int slot) where T : Delegate
        {
            IntPtr address = Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }

        private static string Select(IntPtr owner, bool folders, string initialFolder)
        {
            Marshal.ThrowExceptionForHR(CoInitializeEx(IntPtr.Zero, 2));
            IntPtr dialog = IntPtr.Zero, folder = IntPtr.Zero, result = IntPtr.Zero, path = IntPtr.Zero;
            IntPtr title = Marshal.StringToCoTaskMemUni(folders ? "选择导出文件夹" : "导入关卡");
            try
            {
                var clsid = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
                var iid = new Guid("D57C7288-D4AD-4768-BE02-9D969532D960");
                Marshal.ThrowExceptionForHR(CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out dialog));
                Marshal.ThrowExceptionForHR(Method<GetFlags>(dialog,(int)DialogSlot.GetOptions)(dialog,out var flags));
                const uint fileSystem=0x40, folderMode=0x20, existingFile=0x1000, preserveDirectory=8;
                Marshal.ThrowExceptionForHR(Method<SetFlags>(dialog,(int)DialogSlot.SetOptions)(dialog,flags|fileSystem|preserveDirectory|(folders?folderMode:existingFile)));
                if (!folders)
                {
                    var filter = new Filter { Name="关卡文件 (*.json)", Pattern="*.json" };
                    Marshal.ThrowExceptionForHR(Method<SetFilter>(dialog,(int)DialogSlot.SetFileTypes)(dialog,1,ref filter));
                }
                var shellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
                if (Directory.Exists(initialFolder) && SHCreateItemFromParsingName(initialFolder,IntPtr.Zero,ref shellItem,out folder)>=0)
                    Marshal.ThrowExceptionForHR(Method<PointerArgument>(dialog,(int)DialogSlot.SetFolder)(dialog,folder));
                Marshal.ThrowExceptionForHR(Method<PointerArgument>(dialog,(int)DialogSlot.SetTitle)(dialog,title));
                int hr = Method<PointerArgument>(dialog,(int)DialogSlot.Show)(dialog,owner);
                if (hr == unchecked((int)0x800704C7)) return null;
                Marshal.ThrowExceptionForHR(hr);
                Marshal.ThrowExceptionForHR(Method<GetItem>(dialog,(int)DialogSlot.GetResult)(dialog,out result));
                const int getDisplayName=5;
                const uint fileSystemPath=0x80058000;
                Marshal.ThrowExceptionForHR(Method<GetName>(result,getDisplayName)(result,fileSystemPath,out path));
                return Marshal.PtrToStringUni(path);
            }
            finally
            {
                Marshal.FreeCoTaskMem(path);
                Marshal.FreeCoTaskMem(title);
                if (result!=IntPtr.Zero) Marshal.Release(result);
                if (folder!=IntPtr.Zero) Marshal.Release(folder);
                if (dialog!=IntPtr.Zero) Marshal.Release(dialog);
                CoUninitialize();
            }
        }
    }
}
