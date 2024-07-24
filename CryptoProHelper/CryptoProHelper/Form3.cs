using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoProHelper
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GetCerts();
        }

        bool GetCerts() // certutil -store -user My
        {
            /* 
            Возвращает список вида:
                0: %SN% %G% %NotBefore% - %NotAfter% без часов:минут
                Контейнер: D:\10222013.000
                Субъект: данные_данные_данные
            */
            try
            {
                Process proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "certutil",
                    Arguments = "-store -user My",
                    StandardOutputEncoding = Encoding.GetEncoding(1251),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                });

                string output = proc.StandardOutput.ReadToEnd();
                string[] outp = output.Split('\n');

                foreach (var item in outp)
                {
                    treeView1.Nodes.Add(item, item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "GetOtpechatok()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        bool DelCert(int index) // certutil -delstore -user My 0 (где ноль номер сертификата)
        {
            return false;
        }

        string GetContPath(string filename) // Возвращает путь до контейнера. FAT12\CEE666A4_Data\DakgagDu.000\7410 Принимает на вход: DakgagDu.000
        {
            return "";
        }
    }
}
