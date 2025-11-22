using Microsoft.UI.Xaml;

using System;
using System.Globalization;
using System.Runtime.InteropServices;

using WinRT.Interop;

namespace Tlumach.Sample.WinUI
{
    public static partial class LocaleChangeHook
    {
        // Constants
        private const int GWLP_WNDPROC = -4;
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint WM_INPUTLANGCHANGE = 0x0051;

        // Keep a reference to avoid GC collecting the delegate:
        private static WndProcDelegate? _newWndProc;
        private static IntPtr _oldWndProc = IntPtr.Zero;
        private static IntPtr _hwnd = IntPtr.Zero;

        public static event EventHandler? SystemLocaleChanged;

        public static void Attach(Window window)
        {
            if (window is null)
                return;

            _hwnd = WindowNative.GetWindowHandle(window);

            _newWndProc = WndProc;
            _oldWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));

            window.Closed += (_, __) =>
            {
                if (_oldWndProc != IntPtr.Zero && _hwnd != IntPtr.Zero)
                {
                    SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _oldWndProc);
                    _oldWndProc = IntPtr.Zero;
                    _newWndProc = null;
                    _hwnd = IntPtr.Zero;
                }
            };
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_SETTINGCHANGE)
            {
                if (lParam != IntPtr.Zero)
                {
                    var s = Marshal.PtrToStringUni(lParam);
                    if (string.Equals(s, "intl", StringComparison.OrdinalIgnoreCase))
                    {
                        CultureInfo.CurrentCulture.ClearCachedData();
                        CultureInfo.CurrentUICulture.ClearCachedData();
                        SystemLocaleChanged?.Invoke(null, EventArgs.Empty);
                    }
                }
            }
            else if (msg == WM_INPUTLANGCHANGE)
            {
                // If needed, react to input language changes here.
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        // ---------------- P/Invoke section (no CsWin32 needed) ----------------

        // Window procedure delegate
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // user32.dll imports
        // SetWindowLongPtrW is not available on 32-bit; provide a thunk.
        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
#pragma warning disable CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes
        private static partial int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static partial IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
#pragma warning restore CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            }
            else
            {
                // On 32-bit, SetWindowLong returns previous LONG. Cast both ways.
#pragma warning disable CA2020 // Prevent behavioral change
                int prev = SetWindowLong32(hWnd, nIndex, unchecked((int)dwNewLong));
#pragma warning restore CA2020 // Prevent behavioral change
                return new IntPtr(prev);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
#pragma warning disable CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes
        private static extern IntPtr CallWindowProc(
#pragma warning restore CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes
            IntPtr lpPrevWndFunc,
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam);
    }
}
