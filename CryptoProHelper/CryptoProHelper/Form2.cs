using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoProHelper
{
    public partial class Form2 : Form
    {
        List<string> IDs = new List<string>();
        public bool installComplete;
        public Form2()
        {
            InitializeComponent();
            GetOtpechatok();
        }

        async void button1_Click(object sender, EventArgs e)
        {
            if (IDs.Count < 1)
            {
                printError("Серты не найдены");
            }
            else
            {
                button1.Text = "Идёт генерация";
                await Task.Run(() =>
                {
                    for (int i = 0; i < IDs.Count; i++)
                    {
                        gogogo(IDs[i].Split(';')[0], IDs[i].Split(';')[1], i); // Скармливаем пути до ЭЦП и пытаемся их ставить
                    }
                });
                installComplete = true;
                button1.Text = "Сгенерировать!";
            }

        }
        void gogogo(string code, string name, int pos)
        {
            bool setError = true;
            string arg = "$mypwd = ConvertTo-SecureString -String \"" + textBox1.Text + "\" -Force -AsPlainText\r\nget-childitem -path cert:\\CurrentUser\\My\\" + code + "  | Export-PfxCertificate -FilePath " + textBox2.Text + "\\" + name.Split(' ')[0] + ".pfx" + " -Password $mypwd";
            Process proc = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = arg,
                StandardOutputEncoding = Encoding.GetEncoding(866),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });

            string output = proc.StandardOutput.ReadToEnd();

            string[] data = output.Split('\n');
            foreach (var item in data)
            {
                //Console.WriteLine(item);
                if (item.Contains("-a-")) { setError = false; }
            }
            if (!setError)
            {
                replListBox(pos, " - успех!");
            }
            else
            {
                replListBox(pos, " - ошибка!");
            }
        }
        void replListBox(int pos, string two)
        {
            Invoke((Action)(() =>
            {
                string buf = listBox1.Items[pos].ToString();
                listBox1.Items.RemoveAt(pos);
                listBox1.Items.Insert(pos, buf + two);
            }));
        }
        void printError(string type)
        {
            Invoke((Action)(() =>
            {
                listBox1.Items.Add(type);
                SystemSounds.Hand.Play();
            }));
        }
        public void GetOtpechatok()
        {
            try
            {
                Process proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "Get-ChildItem -path cert:CurrentUser\\My",
                    StandardOutputEncoding = Encoding.GetEncoding(866),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                });

                string output = proc.StandardOutput.ReadToEnd();
                string[] outp = output.Split('\n');

                foreach (var item in outp)
                {
                    if (item.Contains("CN="))
                    {
                        string[] items = item.Split(new string[] { "  " }, StringSplitOptions.None);
                        string item2 = items[1].Split(',')[0].Replace("CN=", "");
                        IDs.Add(items[0] + ";" + item2);
                        listBox1.Items.Add(item2);
                    }
                }
                if (IDs.Count < 1) { listBox1.Items.Add("Не удалось получить отпечатки сертификатов"); }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "GetOtpechatok()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            textBox2.Text = path;
        }
    }
}
