using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CryptoProHelper
{
    partial class Form1
    {
        void showToolTip(string text, string title = "")
        {
            Invoke((Action)(() =>
            {
                notifyIcon1.BalloonTipTitle = title;
                notifyIcon1.BalloonTipText = text;
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon1.ShowBalloonTip(3000);
            }));
        }
        void showError(string txt)
        {
            Invoke((Action)(() =>
            {
                treeView1.Nodes.Add(txt, txt);
                treeView1.Nodes[treeView1.Nodes.Count - 1].Tag = "Err";
                treeView1.Nodes[treeView1.Nodes.Count - 1].BackColor = Color.Red;
                treeView1.Nodes[treeView1.Nodes.Count - 1].ForeColor = Color.White;
                SystemSounds.Hand.Play();
            }));
        }

        void replaceListBox(int pos, string two, int type = 0)
        {
            Invoke((Action)(() =>
            {
                string buf = two + treeView1.Nodes[pos].Text;
                treeView1.Nodes.RemoveAt(pos);
                treeView1.Nodes.Insert(pos, buf, buf);
                switch (type)
                {
                    case 0: // Normal
                        treeView1.Nodes[pos].BackColor = Color.White;
                        treeView1.Nodes[pos].ForeColor = Color.Black;
                        break;
                    case 1: // Success
                        treeView1.Nodes[pos].BackColor = Color.Green;
                        treeView1.Nodes[pos].ForeColor = Color.White;
                        break;
                    case 2: // Error
                        treeView1.Nodes[pos].BackColor = Color.Red;
                        treeView1.Nodes[pos].ForeColor = Color.White;
                        break;
                    case 3: // Info (mini error)
                        treeView1.Nodes[pos].BackColor = Color.Orange;
                        treeView1.Nodes[pos].ForeColor = Color.Black;
                        break;
                }
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
    }
}
