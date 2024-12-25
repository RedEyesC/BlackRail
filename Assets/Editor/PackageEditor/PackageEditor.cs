
using UnityEditor;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

#if UNITY_EDITOR && UNITY_STANDALONE_WIN

//参考文章 https://www.cnblogs.com/moran-amos/p/11342095.html
//unity3d调用win32打开对话框
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public class OpenFileName
{
    public int structSize = 0;
    public IntPtr dlgOwner = IntPtr.Zero;
    public IntPtr instance = IntPtr.Zero;
    public String filter = null;
    public String customFilter = null;
    public int maxCustFilter = 0;
    public int filterIndex = 0;
    public IntPtr file = Marshal.AllocHGlobal(1024);
    public int maxFile = 1024;
    public IntPtr fileTitle = Marshal.AllocHGlobal(64);
    public int maxFileTitle = 64;
    public String initialDir = null;  //打开路径
    public String title = null;
    public int flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
    public short fileOffset = 0;
    public short fileExtension = 0;
    public String defExt = null;
    public IntPtr custData = IntPtr.Zero;
    public IntPtr hook = IntPtr.Zero;
    public String templateName = null;
    public IntPtr reservedPtr = IntPtr.Zero;

    public int reservedInt = 0;
    public int flagsEx = 0;
}

public class DllExpand
{
    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Unicode)]
    public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);


    public static OpenFileName ofn = new OpenFileName();

    public static string[] OpenFilePanel(string title, string extension, string directory = "")
    {

        ofn.structSize = Marshal.SizeOf(ofn);

        ofn.filter = string.Format("{0}(*.{1})\0*.{2}\0All Files\0*.*\0\0", extension, extension, extension);

        ofn.title = title;  //窗口名称

        ofn.defExt = extension;//默认输入文件的类型

        ofn.initialDir = directory;

        if (GetOpenFileName(ofn))
        {
            string longFilePath = Marshal.PtrToStringUni(ofn.file, 1024);
            longFilePath = longFilePath.Substring(0, longFilePath.IndexOf("\0\0"));

            string[] paths = longFilePath.Split('\0');
            string[] filepaths = null;
            if (paths.Length > 1)
            {
                filepaths = new string[paths.Length - 1];
                string prefixStr = paths[0];
                for (int i = 1; i < paths.Length; i++)
                {
                    filepaths[i - 1] = prefixStr + "\\" + paths[i];
                }

                return filepaths;
            }
            else
            {
                filepaths = new string[paths.Length];
                filepaths[0] = longFilePath;
                return filepaths;
            }

        }

        return null;
    }
}

#endif

namespace GameEditor.PackageEditor
{

    class PackageEditor
    {


#if UNITY_EDITOR && UNITY_STANDALONE_WIN

        [MenuItem("Assets/Import Package/Import Packages")]
        static void ImportPackage()
        {
            var path = DllExpand.OpenFilePanel("Import package", "unitypackage");

            if (path != null)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    AssetDatabase.ImportPackage(path[i], false);
                    Debug.Log("Import " + path[i] + " success");
                }

            }

        }

#endif

        [MenuItem("Assets/Export Packages", true)]
        static bool ExportPackageValidation()
        {
            for (var i = 0; i < Selection.objects.Length; i++)
            {
                if (AssetDatabase.GetAssetPath(Selection.objects[i]) != "")
                    return true;
            }

            return false;
        }



        [MenuItem("Assets/Export Packages")]
        static void ExportPackage()
        {

            //ExportPackageByCmd();

            var path = EditorUtility.SaveFilePanel("Save unitypackage", "", "", "unitypackage");
            if (path == "")
                return;

            var assetPathNames = new string[Selection.objects.Length];
            for (var i = 0; i < assetPathNames.Length; i++)
            {
                assetPathNames[i] = AssetDatabase.GetAssetPath(Selection.objects[i]);
            }

            assetPathNames = AssetDatabase.GetDependencies(assetPathNames);

            AssetDatabase.ExportPackage(assetPathNames, path, ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
        }
    }
}


