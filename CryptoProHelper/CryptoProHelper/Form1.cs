using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace CryptoProHelper
{
    public partial class Form1 : Form
    {
        string[] startArgs;
        bool startArgsInstallError = false;
        List<string> certs = new List<string>();
        string SID = "";
        bool installComplete = false;
        int installedCerts = 0;

        // Multi thread
        bool isRegistry = true;
        string dLetter = "";
        public Form1(string[] args)
        {
            InitializeComponent();

            if (!Directory.Exists(@"C:\Program Files\Crypto Pro\CSP")) { MessageBox.Show("Проверьте путь:\nC:\\Program Files\\Crypto Pro\\CSP", "CryptoPro не обнаружен", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            startArgs = args;
            this.Width = 500;
            getSID();

            comboBox1.SelectedIndex = 0;
            string[] drives = Directory.GetLogicalDrives();
            foreach (string str in drives)
            {
                if (!str.Contains("C:")) { comboBox1.Items.Add(str); }
                if (str.Contains("D:")) { comboBox1.SelectedIndex = comboBox1.Items.Count - 1; }
            }
            #region Settings

            this.TopMost = Properties.Settings.Default.topMost;
            checkBox1.Checked = this.TopMost;

            double temp = Convert.ToDouble(Properties.Settings.Default.opacity) / 10;
            this.Opacity = temp;
            trackBar1.Value = Convert.ToInt32((temp - 0.5) * 10);

            checkBox2.Checked = false; // Не убирать!
            checkBox2.Checked = Properties.Settings.Default.autoPass;

            autoinstBox.Checked = Properties.Settings.Default.autoInst;
            #endregion
        }

        #region Interface
        private void Form1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.Width == 500)
            {
                this.Width = 850;
            }
            else
            {
                this.Width = 500;
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Process.Start(@"C:\Program Files\Crypto Pro\CSP\cpconfig.cpl");
        }

        private void установитьКорневыеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[] resf;
            resf = Properties.Resources.Certificates_Kontur_Admin;
            string pathh = Path.GetTempPath() + @"\CryptoProHelper\RootCerts.exe";
            File.WriteAllBytes(pathh, resf);
            Process.Start(pathh);
        }

        private void открытьВеткуРеестраToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RegistryKey myKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit");
            myKey.SetValue("LastKey", "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Crypto Pro\\Settings\\Users\\" + SID + "\\Keys\\", RegistryValueKind.String);
            myKey.Close();

            Process proc = new Process();
            proc.StartInfo.FileName = "regedit";
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();
        }

        private void плагинChromeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"https://chrome.google.com/webstore/detail/cryptopro-extension-for-c/iifchhfnnmpdbibifmljnfjhpififfog?hl=ru");
        }

        private void cAdESYandexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showToolTip("Плагин на рабочем столе");
            Clipboard.SetText("browser://extensions");
            byte[] resf;
            resf = Properties.Resources.yaPlugin;
            File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\yaPlugin.crx", resf);
        }

        private void проверкаCAdESToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"https://disaipe.github.io/crypto-js/example.html");
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopPassDemon();
            Properties.Settings.Default.topMost = checkBox1.Checked;
            Properties.Settings.Default.opacity = Convert.ToInt32(this.Opacity * 10);
            Properties.Settings.Default.contPass = textBox1.Text;
            Properties.Settings.Default.autoPass = checkBox2.Checked;
            Properties.Settings.Default.autoInst = autoinstBox.Checked;
            Properties.Settings.Default.Save();
        }

        async void button1_Click(object sender, EventArgs e)
        {
            clearErrors();
            if (certs.Count < 1)
            {
                showToolTip("Добавьте сертификаты");
            }
            else
            {
                if (comboBox1.SelectedIndex != 0) { isRegistry = false; dLetter = comboBox1.SelectedItem.ToString(); }
                progressBar1.Maximum = certs.Count;
                button1.Text = "Идёт установка";
                label3.Text = "0 из " + certs.Count;
                await Task.Run(() =>
                {
                    for (int i = 0; i < certs.Count; i++)
                    {
                        gogogo(certs[i], i); // Скармливаем пути до ЭЦП и пытаемся их ставить
                        Invoke((Action)(() =>
                        {
                            label3.Text = installedCerts.ToString() + " из " + certs.Count;
                            progressBar1.Value++;
                        }));
                    }
                });
                label3.Text = "Установлено " + installedCerts + " из " + certs.Count;
                installedCerts = 0;
                certs.Clear(); // Установили все серты? Чистим за собой
                installComplete = true;
                progressBar1.Value = 0;
                button1.Text = "Установить";
                if (autoinstBox.Checked && !startArgsInstallError) { Environment.Exit(1); }
            }
        }

        void clearErrors()
        {
            string tag = "";
            List<TreeNode> nodesToRemove = new List<TreeNode>();
            foreach (TreeNode node in treeView1.Nodes)
            {
                tag = "";
                if (node.Tag != null) { tag = node.Tag.ToString(); }
                if (tag == "Err")
                {
                    nodesToRemove.Add(node);
                }
            }

            foreach (TreeNode node in nodesToRemove)
            {
                treeView1.Nodes.Remove(node);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(Cursor.Position);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = checkBox1.Checked;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            this.Opacity = 0.5 + trackBar1.Value * 0.1;
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            if (installComplete) { treeView1.Nodes.Clear(); installComplete = false; }
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string file in files)
            {
                addWays(file);
            }
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                try
                {
                    if (textBox1.Text != "")
                    {
                        startPassDemon(textBox1.Text);
                        checkBox2.Checked = true;
                    }
                    else
                    {
                        checkBox2.Checked = false;
                        MessageBox.Show("Введите пароль от контейнера", "Ошибка запуска автоввода", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                catch (Exception ex)
                {
                    checkBox2.Checked = false;
                    MessageBox.Show(ex.Message, "PasswordDemon", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                foreach (Process proc in Process.GetProcessesByName("CryptoProPass"))
                {
                    proc.Kill();
                }
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            foreach (string item in certs)
            {
                listBox2.Items.Add(item);
            }
        }
        #endregion

        private void autoinstBox_Click(object sender, EventArgs e)
        {
            if (autoinstBox.Checked)
            {
                MessageBox.Show("ЭЦП будут автоматически устанавливаться при drag & drop'e на cryptoprohelper.exe", "Автоустановка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #region Start Args
            try
            {
                if (startArgs.Length > 0)
                {
                    foreach (string file in startArgs)
                    {
                        addWays(file);
                    }
                    if (autoinstBox.Checked)
                    {
                        button1_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Start Args", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            #endregion
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "Реестр")
            {
                button3.Text = "";
                return;
            }
            button3.Text = comboBox1.SelectedItem.ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text != "")
            {
                Process.Start(button3.Text);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("certmgr.msc");
        }

        void updCout()
        {
            label3.Text = "К установке: " + certs.Count;
        }

        private void pfxГенераторToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 f = new Form2();
            f.Show();
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (treeView1.SelectedNode.Index != -1)
            {
                certs.RemoveAt(treeView1.SelectedNode.Index);
                treeView1.Nodes.RemoveAt(treeView1.SelectedNode.Index);
            }
            updCout();
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            if (installComplete) { treeView1.Nodes.Clear(); installComplete = false; }
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string file in files)
            {
                addWays(file);
            }
        }

        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Form3 f = new Form3();
            f.Show();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://t.me/pa4H232");
        }
    }
}
