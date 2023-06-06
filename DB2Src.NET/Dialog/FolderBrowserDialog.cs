using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Db2Source
{
    /// <summary>
    /// FolderBrowserDialog クラスは、フォルダーを選択する機能を提供するクラスです。
    /// <para>
    /// <see cref="Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialog"/> クラスを利用したフォルダーの選択に近い機能を提供します。
    /// </para>
    /// </summary>
    public class FolderBrowserDialog
    {
        #region DllImports
        private static class NativeMethods
        {

            [DllImport("shell32.dll")]
            public static extern int SHILCreateFromPath([MarshalAs(UnmanagedType.LPWStr)] string pszPath, out IntPtr ppIdl, ref uint rgflnOut);

            [DllImport("shell32.dll")]
            public static extern int SHCreateShellItem(IntPtr pidlParent, IntPtr psfParent, IntPtr pidl, out IShellItem ppsi);
        }
        #endregion

        #region Private Classes & Interfaces

        [ComImport]
        [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialogInternal { }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(); // 省略宣言
            void GetParent(); // 省略宣言
            void GetDisplayName([In] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes();  // 省略宣言
            void Compare();  // 省略宣言
        }

        [ComImport]
        [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            uint Show([In] IntPtr parent); // IModalWindow
            void SetFileTypes();  // 省略宣言
            void SetFileTypeIndex([In] uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(); // 省略宣言
            void Unadvise();
            void SetOptions([In] _FILEOPENDIALOGOPTIONS fos);
            void GetOptions(out _FILEOPENDIALOGOPTIONS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int alignment);
            void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid();  // 省略宣言
            void ClearClientData();
            void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
            void GetResults([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenum); // 省略宣言
            void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppsai); // 省略宣言
        }

        #endregion

        #region Fields

        private const uint ERROR_CANCELLED = 0x800704C7;

        #endregion

        #region Properties

        /// <summary>
        /// ユーザーによって選択されたフォルダーのパスを取得または設定します。
        /// </summary>
        public string SelectedPath { get; set; }

        /// <summary>
        /// ダイアログ上に表示されるタイトルのテキストを取得または設定します。
        /// </summary>
        public string Title { get; set; }

        #endregion

        #region Initializes

        /// <summary>
        /// <see cref="FolderBrowserDialog"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public FolderBrowserDialog() { }

        #endregion

        #region Events

        #endregion

        #region Public Methods

        public DialogResult ShowDialog()
        {
            return ShowDialog(IntPtr.Zero);
        }

        public DialogResult ShowDialog(Window owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("指定したウィンドウは null です。オーナーを正しく設定できません。");
            }

            var handle = new WindowInteropHelper(owner).Handle;

            return ShowDialog(handle);
        }

        public DialogResult ShowDialog(IntPtr owner)
        {
            var dialog = new FileOpenDialogInternal() as IFileOpenDialog;

            try
            {
                IShellItem item;
                string selectedPath;

                dialog.SetOptions(_FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS | _FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM);

                if (!string.IsNullOrEmpty(SelectedPath))
                {
                    IntPtr idl = IntPtr.Zero; // path の intptr
                    uint attributes = 0;

                    if (NativeMethods.SHILCreateFromPath(SelectedPath, out idl, ref attributes) == 0)
                    {
                        if (NativeMethods.SHCreateShellItem(IntPtr.Zero, IntPtr.Zero, idl, out item) == 0)
                        {
                            dialog.SetFolder(item);
                        }

                        if (idl != IntPtr.Zero)
                        {
                            Marshal.FreeCoTaskMem(idl);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(Title))
                {
                    dialog.SetTitle(Title);
                }

                var hr = dialog.Show(owner);

                // 選択のキャンセルまたは例外
                if (hr == ERROR_CANCELLED) return DialogResult.Cancel;
                if (hr != 0) return DialogResult.Abort;

                dialog.GetResult(out item);

                if (item != null)
                {
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out selectedPath);
                    SelectedPath = selectedPath;
                }
                else
                {
                    return DialogResult.Abort;
                }

                return DialogResult.OK;
            }
            finally
            {
                Marshal.FinalReleaseComObject(dialog);
            }
        }

        #endregion
    }
    /// <summary>
    /// <see cref="DialogResult"/> 列挙型は、ダイアログ ボックスの戻り値を示す識別子を表します。
    /// </summary>
    public enum DialogResult: int
    {
        /// <summary>
        /// ダイアログ ボックスの戻り値は Nothing です。モーダル ダイアログ ボックスの実行が継続します。
        /// </summary>
        None = 0,
        /// <summary>
        /// ダイアログ ボックスの戻り値は OK です。
        /// </summary>
        OK = 1,
        /// <summary>
        /// ダイアログ ボックスの戻り値は Cancel です。
        /// </summary>
        Cancel = 2,
        /// <summary>
        /// ダイアログ ボックスの戻り値は Abort です。
        /// </summary>
        Abort = 3,
        /// <summary>
        /// ダイアログ ボックスの戻り値は Retry です。
        /// </summary>
        Retry = 4,
        /// <summary>
        /// ダイアログ ボックスの戻り値は Ignore です。
        /// </summary>
        Ignore = 5,
        /// <summary>
        /// ダイアログ ボックスの戻り値は Yes です。
        /// </summary>
        Yes = 6,
        /// <summary>
        /// ダイアログ ボックスの戻り値は No です。
        /// </summary>
        No = 7
    }
    /// <summary>
    /// SIGDN クラスは、IShellItem::GetDisplayName および SHGetNameFromIDList を使用して取得するアイテムの表示名の形式を定義します。
    /// </summary>
    public enum SIGDN: uint
    {
        SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
        SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
        SIGDN_FILESYSPATH = 0x80058000,
        SIGDN_NORMALDISPLAY = 0,
        SIGDN_PARENTRELATIVE = 0x80080001,
        SIGDN_PARENTRELATIVEEDITING = 0x80031001,
        SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
        SIGDN_PARENTRELATIVEPARSING = 0x80018001,
        SIGDN_URL = 0x80068000
    }

    /// <summary>
    /// <see cref="_FILEOPENDIALOGOPTIONS"/> 列挙型は、[開く] または [保存] ダイアログで使用できるオプションのセットを定義します。
    /// </summary>
    [Flags]
    public enum _FILEOPENDIALOGOPTIONS: uint
    {
        FOS_OVERWRITEPROMPT = 0x00000002,
        FOS_STRICTFILETYPES = 0x00000004,
        FOS_NOCHANGEDIR = 0x00000008,
        /// <summary>
        /// ファイルではなくフォルダを選択できる [開く] ダイアログボックスを表示します。
        /// </summary>
        FOS_PICKFOLDERS = 0x00000020,
        /// <summary>
        /// ファイルシステムのアイテムを返却します。
        /// </summary>
        FOS_FORCEFILESYSTEM = 0x00000040,
        FOS_ALLNONSTORAGEITEMS = 0x00000080,
        FOS_NOVALIDATE = 0x00000100,
        FOS_ALLOWMULTISELECT = 0x00000200,
        FOS_PATHMUSTEXIST = 0x00000800,
        FOS_FILEMUSTEXIST = 0x00001000,
        FOS_CREATEPROMPT = 0x00002000,
        FOS_SHAREAWARE = 0x00004000,
        FOS_NOREADONLYRETURN = 0x00008000,
        FOS_NOTESTFILECREATE = 0x00010000,
        FOS_HIDEMRUPLACES = 0x00020000,
        FOS_HIDEPINNEDPLACES = 0x00040000,
        FOS_NODEREFERENCELINKS = 0x00100000,
        FOS_DONTADDTORECENT = 0x02000000,
        FOS_FORCESHOWHIDDEN = 0x10000000,
        FOS_DEFAULTNOMINIMODE = 0x20000000,
        FOS_FORCEPREVIEWPANEON = 0x40000000,
        FOS_SUPPORTSTREAMABLEITEMS = 0x80000000
    }

    //private void Button_Click3(object sender, RoutedEventArgs e)
    //{
    //    var result = Dialogs.DialogResult.None;
    //    var browser = new Dialogs.FolderBrowserDialog();

    //    browser.Title = "フォルダーを選択してください";
    //    browser.SelectedPath = Path3Lable.Content.ToString();

    //    // ウィンドウが取得できるときは設定する
    //    var obj = sender as DependencyObject;

    //    if (obj != null)
    //    {
    //        var window = Window.GetWindow(obj);

    //        if (window != null) result = browser.ShowDialog(window);
    //    }
    //    else
    //    {
    //        result = browser.ShowDialog(IntPtr.Zero);
    //    }

    //    if (result == Dialogs.DialogResult.OK)
    //    {
    //        Path3Lable.Content = browser.SelectedPath;
    //    }
    //}
}