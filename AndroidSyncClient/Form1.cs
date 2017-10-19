using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpAdbClient;
using System.IO;
using System.Threading;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using AndroidSyncClient.Properties;
using System.Reflection;
using SharpAdbClient.DeviceCommands;
using MaterialSkin;
using MaterialSkin.Controls;


namespace AndroidSyncClient

{
    public partial class Form1 : MaterialForm
    {

        public Form1()
        {
         
            InitializeComponent();
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800 /*Color1*/, Primary.BlueGrey900 /*Fensterfarbe*/, 
                                                              Primary.BlueGrey500 /*Color2*/, Accent.DeepOrange400 /*TabAccent*/, 
                                                              TextShade.WHITE);
        }

        //Globally defined variables tempPath and resourceFolder
        string tempPath = System.IO.Path.GetTempPath();
        string resFolder = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "Resources");
        string rootFolder = System.Windows.Forms.Application.StartupPath;

        // ADB TCP-ServerEndPoint listening on 127.0.0.1:5037
        TcpClient client = new TcpClient();
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5037);

        //ThreadSafe Label-Set
        public delegate void ChangeText(Control control, String text);
        public void SetText(Control control, String text) {
            if (control.InvokeRequired) {
                control.BeginInvoke(new ChangeText(SetText),control,text);
            }
            else {
                control.Text = text;
            }
        }
        
        //ThreadSafe ProgressBar-Set
        public delegate void ChangeProgress(int min, int max, int value);
        public void SetProgress(int min, int max, int value)
        {
            if (materialProgressBar1.InvokeRequired)
            {
                materialProgressBar1.BeginInvoke(new ChangeProgress(SetProgress), min, max, value);
            }
            else
            {
                materialProgressBar1.Minimum = min;
                materialProgressBar1.Maximum = max;
                materialProgressBar1.Value = value;
            }
        }

        //Enable-disable buttons
        public delegate void VoidAction();
        public void DisableButtons()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new VoidAction(DisableButtons));
            }
            else
            {
                foreach (Control c in this.Controls)
                {
                    if (c.GetType() == typeof(Button))
                    {
                        c.Enabled = false;
                    }
                }
            }
        }
        public void EnableButtons()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new VoidAction(EnableButtons));
            }
            else
            {
                foreach (Control c in this.Controls)
                {
                    if (c.GetType() == typeof(Button))
                    {
                        c.Enabled = true;
                    }
                }
            }
        }
        
        //On Form Load
        private void Form1_Load(object sender, EventArgs e)
        {
            AdbServer AdbServer = new AdbServer();
            
               //Fingerprint must have been accepted on the device for this to work.
               //ADB driver must be installed. Otherwise it will not recognize the phone.

               //Stard ADB-Server
               AdbServer.StartServer(resFolder +@"\adb.exe", restartServerIfNewer: true);
        }

        private void materialFlatButton1_Click(object sender, EventArgs e)
        {
            //Geräteinformationen abrufen
            var devices = AdbClient.Instance.GetDevices();
            foreach (var device in devices)
            {
             
                materialSingleLineTextField1.Text = device.Name;
            }

            //Monitor auf TCP Basis erstellen
            var monitor = new DeviceMonitor(new AdbSocket(serverEndPoint));

            monitor.DeviceConnected += this.OnDeviceConnected;
            monitor.DeviceDisconnected += this.OnDeviceDisonnected;
            monitor.Start();
        }

        //Method call of this.OnDeviceConnected
        void OnDeviceConnected(object sender, DeviceDataEventArgs e)
        {
            SetText(this.materialLabel1, "The device " + materialSingleLineTextField1.Text + " has connected.");
        }

        //Methode aufgerufen von this.OnDeviceDisconnected
        void OnDeviceDisonnected(object sender, DeviceDataEventArgs e)
        {
            SetText(this.materialLabel1, "The device " + materialSingleLineTextField1.Text + " has disconnected.");
        }

        //Send command via adb shell to phone
        private void materialFlatButton2_Click(object sender, EventArgs e)
        {
            var device = AdbClient.Instance.GetDevices().First();
            var receiver = new ConsoleOutputReceiver();

            AdbClient.Instance.ExecuteRemoteCommand(textBox2.Text, device, receiver);
            textBox3.Text = (receiver.ToString());
        }
        
        //Download test app "Idealo" from phone
        private void materialFlatButton3_Click(object sender, EventArgs e)
        {
            var device = AdbClient.Instance.GetDevices().First();

            saveFileDialog1.Filter = "Android-Application|*.apk";
            saveFileDialog1.Title = "Save Application";
            saveFileDialog1.ShowDialog();

            using (SyncService service = new SyncService(new AdbSocket(serverEndPoint), device))
            using (Stream stream = (System.IO.FileStream)saveFileDialog1.OpenFile())
            {
                service.Pull("/data/app/de.idealo.android-1/base.apk", stream, null, CancellationToken.None);
            }
        }

        //Make full backup
        private void materialFlatButton4_Click(object sender, EventArgs e)
        {

        }

        //Photos Backup
        private void materialFlatButton5_Click(object sender, EventArgs e)
        {
            CrossThread.RunAsync(FolderBackup, "/storage/emulated/0/DCIM");
            DisableButtons();
        }


        //Backup whatsapp folder
        private void materialFlatButton6_Click(object sender, EventArgs e)
        {
            CrossThread.RunAsync(FolderBackup, "/storage/emulated/0/WhatsApp");
            DisableButtons();
        }


        // Screenshot Backup
        private void materialFlatButton7_Click(object sender, EventArgs e)
        {
            CrossThread.RunAsync(FolderBackup, "/storage/emulated/0/Pictures/Screenshots");
        }


        //Install app to phone
        private void materialFlatButton8_Click(object sender, EventArgs e)
        {
            var device = AdbClient.Instance.GetDevices().First();
            var receiver = new ConsoleOutputReceiver();

            PackageManager manager = new PackageManager(device);
            manager.InstallPackage(@"C:\Users\Florian\Desktop\idealo.apk", reinstall: true);
            
        }


        //Execute adb command
        private void materialFlatButton9_Click(object sender, EventArgs e)
        {
            string adbcommand = textBox1.Text;
            CrossThread.RunAsync(executeAdbCommand, adbcommand);

        }
        

        //Kill task "adb.exe" on program exit
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var process in Process.GetProcessesByName("adb"))
            {
                process.Kill();
            }           
        }

        //Bad Method FolderBackup -- rec find folder
        public void FolderBackup(String folder)
        {
            DisableButtons();

            DeviceData device = AdbClient.Instance.GetDevices().First();
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            String command = "ls -R "; // MessageBox.Show(command);

            AdbClient.Instance.ExecuteRemoteCommand(command + folder, device, receiver);

            String[] files = (receiver.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            String directory = "";
            String directyOnPc = "";
            Stream stream = null;
            String[] folderSplit = folder.Split('/');
            String rootDirect = "";

            for (int i = 0; i < folderSplit.Length - 1; i++)
            {
                if (i > 0)
                {
                    rootDirect += "/";
                }
                rootDirect += folderSplit[i];
            }

            for (int i = 0; i <= files.Length - 2; i++)
            {
                try
                {
                    if (files[i].StartsWith("/") && files[i].EndsWith(":"))
                    {
                        directory = files[i].Replace(":", "") + "/";
                        directyOnPc = rootFolder + directory.Replace(rootDirect, "").Replace("/", @"\");

                        if (!Directory.Exists(directyOnPc))
                        {
                            Directory.CreateDirectory(directyOnPc);
                        }
                    }

                    else
                    {
                        SyncService service = new SyncService(new AdbSocket(serverEndPoint), device);
                        stream = File.OpenWrite(directyOnPc + files[i]);

                        service.Pull(directory + files[i], stream, null, CancellationToken.None);
                        stream.Dispose();
                    }
                }

                catch (Exception exception)
                {
                    //  MessageBox.Show(files[i]);
                     Console.WriteLine(exception.ToString());
                    try { stream.Dispose(); }

                    catch (Exception ex){ Console.WriteLine(ex.ToString()); }

                    if (File.Exists(directyOnPc + files[i]))
                      {
                            File.Delete(directyOnPc + files[i]);
                      }

                    else
                    {
                        if (files[i] == directyOnPc)
                        {
                           
                        }

                        else {
                            //MessageBox.Show(exception.Message + "  - - - " + directyOnPc + files[i]);
                        }  
                    }
                }
                SetProgress(0, files.Length - 2, i);
            }
            EnableButtons();
        }

        //Method executeAdbCommand
        public void executeAdbCommand (string c1)
        {

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine("cd Resources");
            cmd.StandardInput.WriteLine(c1);
            cmd.StandardInput.Close();
            //cmd.WaitForExit();
            Console.WriteLine(cmd.StandardOutput.ReadToEnd());
            SetProgress(0, 1 , 1);

        }
        
    }
    
}