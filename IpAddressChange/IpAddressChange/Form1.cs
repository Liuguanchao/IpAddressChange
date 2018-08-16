using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Management;
using System.Text;
using System.Windows.Forms;

namespace IpAddressChange
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //读取文件，获取返回值
            ReturnValue rv = ReadFile.GetFilePath();
            if (!rv.isbo)
            {
                MessageBox.Show(rv.msg);
                return;
            }
            //清空tab控件
            tabControl1.Controls.Clear();
            //获取所有文本
            string[] lines = File.ReadAllLines(rv.msg, Encoding.UTF8);

            TabPage tabpage = null;
            TextBox textbox = null;
            string iptext = string.Empty;
            foreach (string line in lines)
            {
                if (line.Contains("#"))
                {
                    //添加tab
                    if (tabpage != null)
                    {
                        textbox = CreateTextBox(iptext);
                        iptext = string.Empty;
                        tabpage.Controls.Add(textbox);
                        tabControl1.Controls.Add(tabpage);
                        tabpage.ResumeLayout(false);
                        tabpage.PerformLayout();
                    }
                    //初始化 
                    tabpage = CreateTabPage(line.Replace(" ", "").Replace("#", ""));
                    continue;
                }
                iptext += line.Replace(" ", "") + "\r\n";
            }
            if (tabpage != null)
            {
                textbox = CreateTextBox(iptext);
                tabpage.Controls.Add(textbox);
                tabControl1.Controls.Add(tabpage);
                tabControl1.ResumeLayout(false);
                tabpage.ResumeLayout(false);
                tabpage.PerformLayout();
            }

            //绑定下拉列表
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (ManagementObject mo in moc)
            {
                if (mo["IPEnabled"].ToString() == "True")
                {
                    map.Add(mo["MACAddress"].ToString(), mo["Description"].ToString());
                }
            }
            BindingSource bs = new BindingSource();
            bs.DataSource = map;
            comboBox1.DataSource = bs;
            comboBox1.DisplayMember = "Value";
            comboBox1.ValueMember = "Key";

        }
        private TabPage CreateTabPage(string tabname)
        {
            TabPage tabpage = new TabPage();
            tabpage.Location = new System.Drawing.Point(4, 28);
            tabpage.Size = new System.Drawing.Size(315, 242);
            tabpage.TabIndex = 0;
            tabpage.Text = tabname;
            tabpage.UseVisualStyleBackColor = true;
            return tabpage;
        }

        private TextBox CreateTextBox(string value)
        {
            TextBox tx = new TextBox();
            tx.Dock = System.Windows.Forms.DockStyle.Fill;
            tx.Location = new System.Drawing.Point(0, 0);
            tx.Multiline = true;
            tx.Text = value;
            tx.Size = new System.Drawing.Size(315, 242);
            tx.TabIndex = 0;
            return tx;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Control.ControlCollection cc = tabControl1.SelectedTab.Controls;
            string strMac = comboBox1.SelectedValue.ToString();
            List<string> lines = new List<string>();

            foreach (Control c in cc)
            {

                if ((c.GetType().ToString().Equals("System.Windows.Forms.TextBox")))
                {
                    string[] a = { "\r", "\n" };
                    lines.AddRange(c.Text.Split(a, StringSplitOptions.RemoveEmptyEntries));
                    break;
                }
            }
            SetNetworkAdapter(lines, strMac);
        }

        private void SetNetworkAdapter(List<string> lines, string strMac)
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            ManagementBaseObject inPar = null;
            ManagementBaseObject outPar = null;

            foreach (ManagementObject mo in moc)
            {

                if (mo["IPEnabled"].ToString() == "True"
                    && mo["MACAddress"].ToString() == strMac)//根据Mac地址确认要修改的网络适配器
                {
                    if (lines[0] == "XXX.XXX.XXX.XXX")//ip自动获取
                    {
                        if (lines[1] == "XXX.XXX.XXX.XXX")//dns自动获取
                        {

                            mo.InvokeMethod("SetDNSServerSearchOrder", null);
                            mo.InvokeMethod("EnableStatic", null);
                            mo.InvokeMethod("SetGateways", null);
                            mo.InvokeMethod("EnableDHCP", null);
                            break;

                        }
                        else//dns配置
                        {
                            ////IP/网关地址自动获取 
                            //inPar = mo.GetMethodParameters("EnableStatic");
                            //inPar["IPAddress"] = new string[] { };
                            //inPar["SubnetMask"] = new string[] { };
                            //outPar = mo.InvokeMethod("EnableStatic", inPar, null);

                            ////设置网关地址 
                            //inPar = mo.GetMethodParameters("SetGateways");
                            //inPar["DefaultIPGateway"] = new string[] { };  
                            //outPar = mo.InvokeMethod("SetGateways", inPar, null);

                            inPar = mo.GetMethodParameters("SetDNSServerSearchOrder");
                            inPar["DNSServerSearchOrder"] = new string[] { lines[1], lines[2] };
                            outPar = mo.InvokeMethod("SetDNSServerSearchOrder", inPar, null);
                            //开启DHCP
                            mo.InvokeMethod("EnableDHCP", null);
                        }
                    }
                    else//IP地址设置
                    {

                        //设置ip地址和子网掩码 
                        inPar = mo.GetMethodParameters("EnableStatic");
                        inPar["IPAddress"] = new string[] { lines[0] };// 1.备用 2.IP
                        inPar["SubnetMask"] = new string[] { lines[1] };
                        outPar = mo.InvokeMethod("EnableStatic", inPar, null);

                        //设置网关地址 
                        inPar = mo.GetMethodParameters("SetGateways");
                        inPar["DefaultIPGateway"] = new string[] { lines[2] }; // 1.网关;2.备用网关
                        outPar = mo.InvokeMethod("SetGateways", inPar, null);

                        if (lines[3] == "XXX.XXX.XXX.XXX")//
                        { 

                            //重置DNS为空
                            mo.InvokeMethod("SetDNSServerSearchOrder", null);
                            mo.InvokeMethod("EnableStatic", null);
                            mo.InvokeMethod("SetGateways", null);
                            //开启DHCP
                            mo.InvokeMethod("EnableDHCP", null);

                            //设置ip地址和子网掩码 
                            inPar = mo.GetMethodParameters("EnableStatic");
                            inPar["IPAddress"] = new string[] { lines[0] };// 1.备用 2.IP
                            inPar["SubnetMask"] = new string[] { lines[1] };
                            outPar = mo.InvokeMethod("EnableStatic", inPar, null);

                            //设置网关地址 
                            inPar = mo.GetMethodParameters("SetGateways");
                            inPar["DefaultIPGateway"] = new string[] { lines[2] }; // 1.网关;2.备用网关
                            outPar = mo.InvokeMethod("SetGateways", inPar, null);

                        }
                        else
                        { 
                            //设置DNS 
                            inPar = mo.GetMethodParameters("SetDNSServerSearchOrder");
                            inPar["DNSServerSearchOrder"] = new string[] { lines[3], lines[4] }; // 1.DNS 2.备用DNS
                            outPar = mo.InvokeMethod("SetDNSServerSearchOrder", inPar, null);
                            break; 
                        }

                    }
                }
            }
        }
    }
}
