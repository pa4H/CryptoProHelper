using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Media;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace CryptoProHelper
{
    public partial class Form1 : Form
    {
        string[] startArgs;
        bool startArgsInstallError = false;
        List<string> certs = new List<string>();
        string filesDir = Path.GetTempPath() + @"\CryptoProHelper";
        string SID = "";
        bool installComplete = false;

        // Multi thread
        bool isRegistry = true;
        string dLetter = "";
        public Form1(string[] args)
        {
            InitializeComponent();
            if (!Directory.Exists(@"C:\Program Files\Crypto Pro\CSP")) { MessageBox.Show("Проверьте путь:\nC:\\Program Files\\Crypto Pro\\CSP", "CryptoPro не обнаружен", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            startArgs = args;
            this.Width = 500;
            try
            {
                if (!Directory.Exists(filesDir)) { Directory.CreateDirectory(filesDir); }
                if (!File.Exists(filesDir + @"\CryptoProPass.exe"))
                {
                    byte[] resf;
                    resf = Properties.Resources.CryptoProPass;
                    File.WriteAllBytes(filesDir + @"\CryptoProPass.exe", resf);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unpack CryptoProPass", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            getSID();

            comboBox1.SelectedIndex = 0;
            string[] drives = Directory.GetLogicalDrives();
            foreach (string str in drives)
            {
                if (!str.Contains("C:")) { comboBox1.Items.Add(str); }
                if (str.Contains("D:")) { comboBox1.SelectedIndex = comboBox1.Items.Count - 1; }
            }
            #region Settings
            if (!File.Exists(filesDir + @"\settings.txt"))
            {
                StreamWriter sw = new StreamWriter(filesDir + @"\settings.txt");
                sw.WriteLine("false");
                sw.WriteLine("10");
                sw.WriteLine("false");
                sw.WriteLine("false");
                sw.Close();
            }

            try
            {
                string[] sett = File.ReadAllLines(filesDir + @"\settings.txt");
                this.TopMost = sett[0] == "true" ? true : false;
                checkBox1.Checked = this.TopMost;

                double temp = Convert.ToDouble(sett[1]) / 10;
                this.Opacity = temp;
                trackBar1.Value = Convert.ToInt32((temp - 0.5) * 10);

                checkBox2.Checked = false; // Не убирать!
                checkBox2.Checked = sett[2] == "true" ? true : false;

                autoinstBox.Checked = sett[3] == "true" ? true : false;
            }
            catch (Exception exx)
            {
                Directory.Delete(filesDir, true);
                MessageBox.Show("Ошибка чтения файла настроек. Перезапустите программу", exx.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            #endregion
        }
        #region CryptoPro
        void gogogo(string folderPath, int pos)
        {
            checkContainers(0); // Проверяем какие контейнеры лежат на дисках
            if (!isRegistry) // Если не реестр
            {
                copy(folderPath + @"\" + getContainerName(folderPath), dLetter + getContainerName(folderPath)); // Копируем на D папку с закрытым ключем
            }
            else
            {
                byte[] data;
                string branch = "";

                if (is64())
                {
                    branch = "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Crypto Pro\\Settings\\Users\\" + SID + "\\Keys\\";
                }
                else
                {
                    branch = "HKEY_LOCAL_MACHINE\\SOFTWARE\\CryptoPro\\Settings\\Users\\" + SID + "\\Keys\\";
                }

                string certName = getCertName(folderPath);
                branch += certName.Substring(0, certName.Length - 4);

                data = File.ReadAllBytes(folderPath + @"\" + getContainerName(folderPath) + @"\header.key");
                Registry.SetValue(branch, "header.key", data);
                data = File.ReadAllBytes(folderPath + @"\" + getContainerName(folderPath) + @"\masks.key");
                Registry.SetValue(branch, "masks.key", data);
                data = File.ReadAllBytes(folderPath + @"\" + getContainerName(folderPath) + @"\masks2.key");
                Registry.SetValue(branch, "masks2.key", data);
                //data = File.ReadAllBytes(folderPath + @"\" + getContainerName(folderPath) + @"\name.key");
                //Registry.SetValue(branch, "name.key", data);
                data = File.ReadAllBytes(folderPath + @"\" + getContainerName(folderPath) + @"\primary.key");
                Registry.SetValue(branch, "primary.key", data);
                data = File.ReadAllBytes(folderPath + @"\" + getContainerName(folderPath) + @"\primary2.key");
                Registry.SetValue(branch, "primary2.key", data);
            }
            checkContainers(1); // Снова проверяем какие контейнеры лежат на дисках
            if (getNewContainer() == "")
            {
                replaceListBox(pos, " - уже установлено!");
                SystemSounds.Hand.Play();
                startArgsInstallError = true;
            }
            else
            {
                installCRT(folderPath + @"\" + getCertName(folderPath), getNewContainer(), pos); // Ставим .cer
            }

        }
        void installCRT(string certWay, string contName, int pos)
        {
            string a = "-inst -store uMy -file " + "\"" + certWay + "\" -cont \"" + contName + "\"";
            //a = "-inst -store uMy -file \"C:\\Users\\User\\Desktop\\Контур\\Козменков\\Козменков.cer\" -cont \"\\\\.\\FAT12_D\\FAT12\\B87BADC0_HDD\\RSQ7EKC.000\"";
            Process proc = Process.Start(new ProcessStartInfo
            {
                FileName = @"C:\Program Files\Crypto Pro\CSP\certmgr",
                Arguments = a,
                StandardOutputEncoding = Encoding.GetEncoding(866),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });

            string output = proc.StandardOutput.ReadToEnd();

            string[] data = output.Split('\n');
            foreach (var item in data)
            {
                if (item.Contains("Error"))
                {
                    if (!item.Contains("0x00000000"))
                    {
                        replaceListBox(pos, " - " + item);
                    }
                    else
                    {
                        replaceListBox(pos, " - успех!");
                    }
                }
            }
        }
        #endregion

        #region Macros
        void getSID()
        {
            try
            {
                Process proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "wmic", //
                    Arguments = "useraccount where name='" + Environment.UserName + "' get sid",
                    StandardOutputEncoding = Encoding.GetEncoding(866),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                });

                string output = proc.StandardOutput.ReadToEnd();
                string[] outp = output.Split('\n');
                SID = outp[1].Replace("\r", "").Replace(" ", "");
                if (SID == "") { listBox1.Items.Add("Не удалось получить SID. Функции связанные с реестром не будут работать"); }
                label1.Text = "SID: " + SID;
                label2.Text = "Name: " + Environment.UserName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "getSID()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        string getCertName(string way)
        {
            string[] files = Directory.GetFiles(way);
            foreach (var item in files)
            {
                if (item.Contains(".cer"))
                {
                    string[] buf = item.Split('\\');
                    return buf[buf.Length - 1]; // Возвращаем Иванов.cer
                }
            }
            return "";
        }
        string getContainerName(string way)
        {
            string[] dirs = Directory.GetDirectories(way);
            foreach (var item in dirs)
            {
                if (item.Contains(".00"))
                {
                    string[] buf = item.Split('\\');
                    return buf[buf.Length - 1]; // Возвращаем имя контейнера
                }
            }
            return "";
        }
        List<string> oldConts = new List<string>();
        List<string> newConts = new List<string>();
        void checkContainers(int numCont)
        {
            if (numCont == 0) { oldConts.Clear(); newConts.Clear(); }
            string searchString = "";
            if (isRegistry) // Реестр
            {
                searchString = "REGISTRY";
            }
            else // Диск D, E и тд
            {
                searchString = "FAT";
            }

            string a = "-keyset -enum_cont -verifycontext -fqcn";
            Process proc = Process.Start(new ProcessStartInfo
            {
                FileName = @"C:\Program Files\Crypto Pro\CSP\csptest.exe",
                Arguments = a,
                StandardOutputEncoding = Encoding.GetEncoding(866),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });

            string output = proc.StandardOutput.ReadToEnd();

            string[] data = output.Split('\n');
            foreach (var item in data)
            {
                if (item.Contains(searchString))
                {
                    switch (numCont)
                    {
                        case 0:
                            oldConts.Add(item);
                            break;
                        case 1:
                            newConts.Add(item);
                            break;
                    }
                }
            }
        }
        string getNewContainer()
        {
            for (int i = 0; i < newConts.Count; i++)
            {
                for (int ii = 0; ii < oldConts.Count; ii++)
                {
                    if (newConts[i] == oldConts[ii])
                    {
                        newConts[i] = "";
                    }
                }
            }
            foreach (var item in newConts)
            {
                if (item.Length > 1) { return item.Replace("\r", ""); }
            }
            return "";
        }
        void copy(string from, string to)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd";
            string a = "/c xcopy \"" + from + "\" \"" + to + "\" /Y /I";
            p.StartInfo.Arguments = a;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.WaitForExit();
        }

        void addWays(string way)
        {
            if (way.Contains(".cer")) // Если .cer, убираем его из пути.
            {
                string[] a = way.Split('\\');
                way = way.Replace("\\" + a[a.Length - 1], ""); // Убираем Иванов.cer
                if (getContainerName(way) == "") // Если в папке с сертом нет закрытого ключа, выводим ошибку
                {
                    showError("В папке с сертификатом отсутствует закрытый контейнер: " + way);
                    return;
                }
            }
            else // Если папка, чекаем её на наличие серта и добавляем. Если пусто, выводим ошибку
            {
                if (getContainerName(way) == "" && getCertName(way) == "")
                {
                    showError("Отсутствует сертификат и закрытый контейнер: " + way);
                    return;
                }
                else
                {
                    if (getContainerName(way) == "")
                    {
                        showError("Отсутствует закрытый контейнер: " + way);
                        return;
                    }
                    if (getCertName(way) == "")
                    {
                        showError("Отсутствует сертификат: " + way);
                        return;
                    }
                }
            }
            certs.Add(way); // Добавляем путь до папки, где лежит контейнер и .cer
            listBox1.Items.Add(getCertName(way));
        }
        void showError(string type)
        {
            Invoke((Action)(() =>
            {
                listBox1.Items.Add(type);
                SystemSounds.Hand.Play();
            }));
        }

        void replaceListBox(int pos, string two)
        {
            Invoke((Action)(() =>
            {
                string buf = listBox1.Items[pos].ToString().Split(' ')[0];
                listBox1.Items.RemoveAt(pos);
                listBox1.Items.Insert(pos, buf + two);
            }));
        }
        bool is64()
        {
            if (Directory.Exists("C:\\Program Files (x86)"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

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

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Process.Start("certmgr.msc");
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Process.Start(@"C:\Program Files\Crypto Pro\CSP\cpconfig.cpl");
        }

        private void установитьКорневыеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[] resf;
            resf = Properties.Resources.Certificates_Kontur_Admin;
            File.WriteAllBytes(filesDir + @"\RootCerts.exe", resf);
            Process.Start(filesDir + @"\RootCerts.exe");
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
            listBox1.Items.Add("Плагин на рабочем столе");
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
            if (Directory.Exists(filesDir))
            {
                StreamWriter sw = new StreamWriter(filesDir + @"\settings.txt");
                sw.WriteLine(checkBox1.Checked ? "true" : "false");
                sw.WriteLine((this.Opacity * 10).ToString());
                sw.WriteLine(checkBox2.Checked ? "true" : "false");
                sw.WriteLine(autoinstBox.Checked ? "true" : "false");
                sw.Close();
            }

            foreach (Process proc in Process.GetProcessesByName("CryptoProPass"))
            {
                proc.Kill();
            }
        }

        async void button1_Click(object sender, EventArgs e)
        {
            if (certs.Count < 1)
            {
                showError("Добавьте сертификаты");
            }
            else
            {
                if (comboBox1.SelectedIndex != 0) { isRegistry = false; dLetter = comboBox1.SelectedItem.ToString(); }
                progressBar1.Maximum = certs.Count;
                button1.Text = "Идёт установка";
                await Task.Run(() =>
                {
                    for (int i = 0; i < certs.Count; i++)
                    {
                        gogogo(certs[i], i); // Скармливаем пути до ЭЦП и пытаемся их ставить
                        Invoke((Action)(() =>
                        {
                            progressBar1.Value++;
                        }));
                    }
                });
                certs.Clear(); // Установили все серты? Чистим за собой
                installComplete = true;
                progressBar1.Value = 0;
                button1.Text = "Установить";
                if (autoinstBox.Checked && !startArgsInstallError) { Environment.Exit(1); }
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
            if (installComplete) { listBox1.Items.Clear(); installComplete = false; }
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string file in files)
            {
                addWays(file);
            }
        }
        async void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                try
                {
                    if (textBox1.Text != "")
                    {
                        await Task.Run(() =>
                        {
                            StreamWriter sw = new StreamWriter(filesDir + @"\autoPass.txt", false);
                            sw.Write(textBox1.Text);
                            sw.Close();

                            Process.Start(filesDir + @"\CryptoProPass.exe");
                        });
                        checkBox2.Checked = true;
                    }
                    else
                    {
                        checkBox2.Checked = false;
                        MessageBox.Show("Введите пароль от контейнеров", "autoPass.txt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                catch (Exception ex)
                {
                    checkBox2.Checked = false;
                    MessageBox.Show(ex.Message, "CryptoProPass.exe", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedIndex != -1)
            {
                certs.RemoveAt(listBox1.SelectedIndex);
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
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
    }
}
