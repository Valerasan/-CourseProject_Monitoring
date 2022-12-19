using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Forms;
using System.Runtime.InteropServices;
using System.Management;
using Microsoft.Win32;
using LibreHardwareMonitor.Hardware;
using System.Reflection;
using System.Threading;


namespace CoursProject
{
    public partial class Form1 : MetroForm
    {
        private string tmpCPUTemperatureInfo = string.Empty;
        private string tmpCPUVoltageInfo = string.Empty;
        private string tmpRAMInfo = string.Empty;

        private string tmpGPUTemperatureInfo = string.Empty;
        private string tmpGPUVoltageInfo = string.Empty;

        private float CPU;
        private float RAM;
        private string RAMUsed;
        private string RAMAvailable;

        private ulong installedMemory;


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLength;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private void GetCPUTemeratureAndVoltage()
        {
            tmpCPUTemperatureInfo = string.Empty;
            tmpCPUVoltageInfo = string.Empty;

            Visitor updateVisitor = new Visitor();
            Computer computer = new Computer();
            computer.IsCpuEnabled = true;
            computer.Open();
            computer.Accept(updateVisitor);

            for (int i = 0; i < computer.Hardware.Count; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.Cpu)
                {
                    foreach (var sensor in computer.Hardware[i].Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            tmpCPUTemperatureInfo += sensor.Name + ":" + sensor.Value.ToString() + "\r";
                        }
                        else if (sensor.SensorType == SensorType.Voltage)
                        {
                            tmpCPUVoltageInfo += sensor.Name + ":" + Math.Round((double)sensor.Value, 3).ToString() + "\r";
                        }
                    }
                }
            }

            richTextBox1.Text = tmpCPUTemperatureInfo;
            richTextBox2.Text = tmpCPUVoltageInfo;
            computer.Close();
        }

        private void GetRAMLoad()
        {
            Visitor updateVisitor = new Visitor();
            Computer computer = new Computer();
            computer.IsMemoryEnabled = true;
            computer.Open();
            computer.Accept(updateVisitor);

            for (int i = 0; i < computer.Hardware.Count; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.Memory)
                {
                    foreach (var sensor in computer.Hardware[i].Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && !sensor.Name.Contains("Virtual Memory"))
                        {
                            RAM = (float)sensor.Value;
                            metroProgressBar2.Value = (int)sensor.Value;
                            chart1.Series["ОЗУ"].Points.AddY(sensor.Value);
                        }
                        else if (sensor.SensorType == SensorType.Data && !sensor.Name.Contains("Virtual") && !sensor.Name.Contains("Used"))
                        {
                            tmpRAMInfo += sensor.Name + ":" + sensor.Value.ToString() + "\r";
                            RAMUsed = (Math.Round((float)sensor.Value, 1)).ToString();
                        }
                        else if (sensor.SensorType == SensorType.Data && !sensor.Name.Contains("Virtual") && !sensor.Name.Contains("Available"))
                        {
                            tmpRAMInfo += sensor.Name + ":" + sensor.Value.ToString() + "\r";
                            RAMAvailable = (Math.Round((float)sensor.Value, 1)).ToString();
                        }
                    }
                }
            }
            computer.Close();

        }


        private void GetGPUTemeratureAndVoltage()
        {
            tmpGPUTemperatureInfo = string.Empty;
            tmpGPUVoltageInfo = string.Empty;

            Visitor updateVisitor = new Visitor();
            Computer computer = new Computer();
            computer.IsGpuEnabled = true;
            computer.Open();
            computer.Accept(updateVisitor);

            for (int i = 0; i < computer.Hardware.Count; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.GpuAmd
                    || computer.Hardware[i].HardwareType == HardwareType.GpuNvidia
                    || computer.Hardware[i].HardwareType == HardwareType.GpuIntel)
                {
                    foreach (var sensor in computer.Hardware[i].Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            tmpGPUTemperatureInfo += sensor.Name + ":" + sensor.Value.ToString() + "\r";
                        }
                        else if (sensor.SensorType == SensorType.Voltage)
                        {
                            tmpGPUVoltageInfo += sensor.Name + ":" + Math.Round((double)sensor.Value, 3).ToString() + "\r";
                        }
                    }
                }
            }

            richTextBox3.Text = tmpGPUTemperatureInfo;
            richTextBox4.Text = tmpGPUVoltageInfo;

            computer.Close();

        }


        public Form1()
        {
            InitializeComponent();
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            MEMORYSTATUSEX memorystatusex = new MEMORYSTATUSEX();

            if (GlobalMemoryStatusEx(memorystatusex))
            {
                installedMemory = memorystatusex.ullTotalPhys;
            }

            metroLabel9.Text = Convert.ToString(installedMemory / 1000000000) + " Гб";

            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor");

            foreach (ManagementObject queryObj in searcher.Get())
            {
                metroLabel11.Text = ("CPU: " + queryObj["Name"]);
            }

            searcher.Query.QueryString = "SELECT * FROM Win32_VideoController";

            foreach (ManagementObject queryObj in searcher.Get())
            {

                metroLabel12.Text = ("GPU: " + queryObj["Name"]);
            }


            timer1.Interval = 1000;
            timer1.Start();
            backgroundWorker1.RunWorkerAsync();
        }




        private void timer1_Tick(object sender, EventArgs e)
        {
            CPU = performanceCPU.NextValue();

            metroProgressBar1.Value = (int)CPU;
            //metroProgressBar2.Value = (int)RAM;

            metroLabel2.Text = Convert.ToString(Math.Round(CPU, 1)) + " %";
            metroLabel3.Text = Convert.ToString(Math.Round(RAM, 1)) + " %";

            metroLabel7.Text = RAMAvailable + " Гб";
            metroLabel10.Text = RAMUsed + " Гб";

            chart1.Series["ЦП"].Points.AddY(CPU);



        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                GetCPUTemeratureAndVoltage();
                GetRAMLoad();
                GetGPUTemeratureAndVoltage();


                Thread.Sleep(1000);
            }
        }


    }
}


