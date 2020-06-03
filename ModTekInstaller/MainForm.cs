using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModTekInstaller
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            foreach(string f in System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                //MessageBox.Show(f);
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox.Text = openFileDialog.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox.Text.Length <= 0) {
                MessageBox.Show("You should select game's executable path");
                return;
            };
            string gameDir = Path.GetDirectoryName(textBox.Text);
            string StreamingAssets = Path.Combine(Path.Combine(gameDir, "BattleTech_Data"),"StreamingAssets");
            string versionFile = Path.Combine(StreamingAssets, "version.json");
            if (File.Exists(versionFile) == false)
            {
                MessageBox.Show("Wrong path","Path you provided is wrong. Instalation FAIL");
                return;
            }
            string prefix = "\"ProductVersion\": \"";
            string versionFull = File.ReadAllText(versionFile);
            int prefixIndex = versionFull.IndexOf(prefix);
            if (prefixIndex < 0) {
                MessageBox.Show("Wrong path", "Can't detect Battletech version. Path you provided is wrong. Instalation FAIL");
                return;
            }
            int versionEndIndex = versionFull.IndexOf("\"", prefixIndex + prefix.Length);
            string version = versionFull.Substring(prefixIndex + prefix.Length, versionEndIndex - prefixIndex - prefix.Length);
            if (version != "1.9.1") {
                MessageBox.Show("Wrong BT version","Wrong BT version. Found :"+version+" require 1.9.1");
                return;
            }
            if (Directory.Exists(Path.Combine(gameDir, "Mods")) == false)
            {
                Directory.CreateDirectory(Path.Combine(gameDir, "Mods"));
            }
            string modtekDir = Path.Combine(Path.Combine(gameDir, "Mods"),"ModTek");
            bool replace = true;
            if(Directory.Exists(modtekDir))
            {
                DialogResult res = MessageBox.Show("Replace?", "Mods/ModTek directory exists. Replace?", MessageBoxButtons.YesNo);
                if (res == DialogResult.No) { replace = false; } else
                {
                    Directory.Delete(modtekDir, true);
                }
            }
            if (replace) {
                Stream modtekZip = typeof(MainForm).Assembly.GetManifestResourceStream("ModTekInstaller.Resources.ModTek.zip");
                if (modtekZip != null)
                {
                    string modtekZipDest = Path.Combine(Path.Combine(gameDir, "Mods"),"ModTek.zip");
                    if (File.Exists(modtekZipDest)) { File.Delete(modtekZipDest); }
                    using (FileStream destination = File.Open(modtekZipDest, FileMode.Create))
                    {
                        modtekZip.CopyTo(destination);
                    }
                    if (File.Exists(modtekZipDest) == false)
                    {
                        MessageBox.Show("Install Fail", modtekZipDest+ " not found");
                        return;
                    }
                    Directory.CreateDirectory(modtekDir);
                    ZipFile.ExtractToDirectory(modtekZipDest, modtekDir);
                    File.Delete(modtekZipDest);
                }
                else
                {
                    MessageBox.Show("Install Fail","No ModTek.zip in resources");
                    return;
                }
            }
            string BTAssembly = gameDir.PA("BattleTech_Data").PA("Managed").PA("Assembly-CSharp.dll");
            if (File.Exists(BTAssembly) == false)
            {
                MessageBox.Show("Can't find BT Assembly-CSharp.dll "+BTAssembly);
                return;
            }
            Log.BaseDirectory = gameDir;
            Log.InitLog();
            Log.M.TWL(0, "Inited");
            int ret = Injector.Main(gameDir);
            if(ret == Injector.RC_NORMAL)
            {
                MessageBox.Show("Success");
            }
            else
            {
                MessageBox.Show("Fail to patch Assembly-CSharp.dll. More info in "+ Log.BaseDirectory.PA("ModTekInstaller_main_log.txt"));
            }
        }
    }
    public class BTVersion
    {
      public string ProductName { get; set; }
        public string ProductVersion { get; set; }
        public string Platform { get; set; }
        public string BuildRevision { get; set; }
        public string BuildBranch { get; set; }
        public string BuildDate { get; set; }
        public string BuildStamp { get; set; }
        public string BuildNumber { get; set; }
    }

    public static class PathCombineHelper
    {
        public static string PA(this string path, string add) { return Path.Combine(path, add); }
    }
}
