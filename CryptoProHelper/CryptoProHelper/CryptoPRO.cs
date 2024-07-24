using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CryptoProHelper
{
    partial class Form1
    {
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
                replaceListBox(pos, "Уже установлено -  ", 3);
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
                        replaceListBox(pos, "Error " + item + " -  ", 2);
                    }
                    else
                    {
                        replaceListBox(pos, "", 1);
                        installedCerts++;
                    }
                }
            }
        }
        #endregion
        #region Macros
        async void getSID()
        {
            await Task.Run(() =>
            {
                try
                {
                    Process proc = Process.Start(new ProcessStartInfo
                    {
                        FileName = "wmic",
                        Arguments = "useraccount where name='" + Environment.UserName + "' get sid",
                        StandardOutputEncoding = Encoding.GetEncoding(866),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    });

                    string output = proc.StandardOutput.ReadToEnd();
                    string[] outp = output.Split('\n');
                    SID = outp[1].Replace("\r", "").Replace(" ", "");
                    Invoke((Action)(() =>
                    {
                        if (SID == "") { showToolTip("Функции связанные с реестром не будут работать", "Не удалось получить SID"); }
                        label1.Text = "SID: " + SID;
                        label2.Text = "Name: " + Environment.UserName;
                    }));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "getSID()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
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

        void addWays(string way)
        {
            FileAttributes attr = File.GetAttributes(way);
            if (way.Contains(".cer")) // Если .cer, убираем его из пути.
            {
                string[] a = way.Split('\\');
                way = way.Replace("\\" + a[a.Length - 1], ""); // Убираем Иванов.cer
                if (getContainerName(way) == "") // Если в папке с сертом нет закрытого ключа, выводим ошибку
                {
                    showError("    В папке с сертификатом отсутствует закрытый контейнер: " + way);
                    return;
                }
            }
            else // Если папка, чекаем её на наличие серта и добавляем. Если пусто, выводим ошибку
            {
                if (!attr.HasFlag(FileAttributes.Directory)) // Если скормили НЕ папку, выходим
                { 
                    return;
                }
                if (getContainerName(way) == "" && getCertName(way) == "")
                {
                    showError("    Отсутствует сертификат и закрытый контейнер: " + way);
                    return;
                }
                else
                {
                    if (getContainerName(way) == "")
                    {
                        showError("    Отсутствует закрытый контейнер: " + way);
                        return;
                    }
                    if (getCertName(way) == "")
                    {
                        showError("    Отсутствует сертификат: " + way);
                        return;
                    }
                }
            }
            certs.Add(way); // Добавляем путь до папки, где лежит контейнер и .cer
            string txt = getCertName(way).Replace(".cer", "");
            treeView1.Nodes.Add(txt, txt);
            updCout();
        }

        #endregion
    }
}
