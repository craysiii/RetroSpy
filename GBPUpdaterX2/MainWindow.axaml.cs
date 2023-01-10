using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading;
using Avalonia.Threading;
using MessageBox.Avalonia.Enums;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Management;
using System.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace GBPUpdaterX2
{
    public partial class MainWindow : Window
    {
        public class COMPortInfo
        {
            public string? PortName { get; set; }
            public string? FriendlyName { get; set; }
        }

        private static string[] GetUSBCOMDevices()
        {
            List<string> list = new();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

                ManagementObjectSearcher searcher2 = new("SELECT * FROM Win32_PnPEntity");
                foreach (ManagementObject mo2 in searcher2.Get().Cast<ManagementObject>())
                {
                    if (mo2["Name"] != null)
                    {
                        string? name = mo2["Name"].ToString();
                        // Name will have a substring like "(COM12)" in it.
                        if (name != null && name.Contains("(COM"))
                        {
                            list.Add(name);
                        }
                    }
                }
                searcher2.Dispose();
            }

            // remove duplicates, sort alphabetically and convert to array
            string[] usbDevices = list.Distinct().OrderBy(s => s).ToArray();
            return usbDevices;
        }


        private static List<string> SetupCOMPortInformation()
        {
            List<COMPortInfo> comPortInformation = new List<COMPortInfo>();

            string[] portNames = SerialPort.GetPortNames();
            foreach (string s in portNames)
            {
                // s is like "COM14"
                COMPortInfo ci = new COMPortInfo
                {
                    PortName = s,
                    FriendlyName = s
                };
                comPortInformation.Add(ci);
            }

            string[] usbDevs = GetUSBCOMDevices();
            foreach (string s in usbDevs)
            {
                // Name will be like "USB Bridge (COM14)"
                int start = s.IndexOf("(COM", StringComparison.Ordinal) + 1;
                if (start >= 0)
                {
                    int end = s.IndexOf(")", start + 3, StringComparison.Ordinal);
                    if (end >= 0)
                    {
                        // cname is like "COM14"
                        string cname = s.Substring(start, end - start);
                        for (int i = 0; i < comPortInformation.Count; i++)
                        {
                            if (comPortInformation[i].PortName == cname)
                            {
                                comPortInformation[i].FriendlyName = s.Remove(start - 1).TrimEnd();
                            }
                        }
                    }
                }
            }

            List<string> ports = new List<string>();
            foreach (COMPortInfo port in comPortInformation)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || (port.PortName != null && port.FriendlyName != null && port.FriendlyName.Contains("Arduino")))
                {
                    ports.Add(port.PortName ?? "COMX");
                }
                else if (port.PortName != null && port.FriendlyName != null && (port.FriendlyName.Contains("CH340") || port.FriendlyName.Contains("CH341")))
                {
                    ports.Add(port.PortName);
                }
            }

            return ports;
        }

        private static void DownloadFirmware(string downloadDirectory)
        {
            HttpRequestMessage request = new();
            request.RequestUri = new Uri("https://github.com/retrospy/RetroSpy/releases/latest/download/GBP_Firmware.zip");

            HttpClient client = new HttpClient();
            var response = client.Send(request);
            using (var fs = new FileStream(
                Path.Combine(downloadDirectory, "GBP_Firmware.zip"),
                FileMode.CreateNew))
            {
                response.Content.ReadAsStream().CopyTo(fs);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateThread()
        {


            try
            {
                int serialNumber = 0;
                if (Dispatcher.UIThread.CheckAccess())
                {
                    try
                    {
                        serialNumber = Int32.Parse(txtboxSerialNumber.Text);
                    }
                    catch (Exception)
                    {
                        serialNumber = 0;
                    }

                    goButton.IsEnabled = false;
                    txtboxData.Text = string.Empty;
                    txtboxData.CaretIndex = int.MaxValue;
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            serialNumber = Int32.Parse(txtboxSerialNumber.Text);
                        }
                        catch (Exception)
                        {
                            serialNumber = 0;
                        }

                        this.goButton.IsEnabled = false;
                        txtboxData.Text = string.Empty;
                        txtboxData.CaretIndex = int.MaxValue;
                    });
                }



                string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                _ = Directory.CreateDirectory(tempDirectory);
                if (Dispatcher.UIThread.CheckAccess())
                {
                    txtboxData.Text += "Downloading latest firmware...";
                    txtboxData.CaretIndex = int.MaxValue;
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        txtboxData.Text += "Downloading latest firmware...";
                        txtboxData.CaretIndex = int.MaxValue;
                    });
                }

                DownloadFirmware(tempDirectory);

                if (Dispatcher.UIThread.CheckAccess())
                {
                    txtboxData.Text += "done.\n\n";
                    txtboxData.CaretIndex = int.MaxValue;
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        txtboxData.Text += "done.\n\n";
                        txtboxData.CaretIndex = int.MaxValue;
                    });
                }



                if (Dispatcher.UIThread.CheckAccess())
                {
                    txtboxData.Text += "Decompressing firmware package...";
                    txtboxData.CaretIndex = int.MaxValue;
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        txtboxData.Text += "Decompressing firmware package...";
                        txtboxData.CaretIndex = int.MaxValue;
                    });
                }

                ZipFile.ExtractToDirectory(Path.Combine(tempDirectory, "GBP_Firmware.zip"), tempDirectory);

                if (Dispatcher.UIThread.CheckAccess())
                {
                    txtboxData.Text += "done.\n\n";
                    txtboxData.Text += "Searching for GameBoy Printer Emulator...";
                    txtboxData.CaretIndex = int.MaxValue;
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        txtboxData.Text += "done.\n\n";
                        txtboxData.Text += "Searching for GameBoy Printer Emulator...";
                        txtboxData.CaretIndex = int.MaxValue;
                    });
                }


                SerialPort? _serialPort = null;

                List<string> arduinoPorts = SetupCOMPortInformation();
                string gbpemuPort = "";
                bool foundPort = false;

                foreach (string port in arduinoPorts)
                {
                    try
                    {
                        using (_serialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One)
                        {
                            Handshake = Handshake.None,

                            ReadTimeout = 500,
                            WriteTimeout = 500
                        })
                        {

                            try
                            {
                                _serialPort.Open();
                            }
                            catch (Exception)
                            {
                                continue;
                            }

                            try
                            {
                                _serialPort.Write("\x88\x33\x0F\x00\x00\x00\x0F\x00\x00");
                            }
                            catch (Exception)
                            {
                                _serialPort.Close();
                                continue;
                            }

                            string? result = null;
                            do
                            {
                                _serialPort.ReadTimeout = 2500;
                                result = _serialPort.ReadLine();
                            } while (result != null && !(result.StartsWith("// GAMEBOY PRINTER Emulator V3 : Copyright (C) 2020 Brian Khuu")
                                || result.StartsWith("// GAMEBOY PRINTER Emulator V3.2.1 (Copyright (C) 2022 Brian Khuu")
                                || result.StartsWith("// GAMEBOY PRINTER Packet Capture V3.2.1 (Copyright (C) 2022 Brian Khuu")
                                || result.StartsWith("d=debug, ?=help")));

                            foundPort = true;
                            gbpemuPort = port;
                            _serialPort.Close();
                        }
                    }
                    catch (Exception) { }
                }

                if (!foundPort)
                {
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        txtboxData.Text += "cannot find RetroSpy GameBoy Printer Emulator.\n\n";
                        AvaloniaMessageBox("RetroSpy", "Couldn't find RetroSpy Pixel.", ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                        txtboxData.CaretIndex = int.MaxValue;
                        this.goButton.IsEnabled = true;
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            txtboxData.Text += "cannot find RetroSpy GameBoy Printer Emulator.\n\n";
                            AvaloniaMessageBox("RetroSpy", "Couldn't find RetroSpy Pixel.", ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                            txtboxData.CaretIndex = int.MaxValue;
                            this.goButton.IsEnabled = true;
                        });
                    }
                }
                else
                {
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        txtboxData.Text += "found on " + gbpemuPort + ".\n\n";
                        txtboxData.Text += "Updating firmware...\n";
                        txtboxData.CaretIndex = int.MaxValue;
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            txtboxData.Text += "found on " + gbpemuPort + ".\n\n";
                            txtboxData.Text += "Updating firmware...\n";
                            txtboxData.CaretIndex = int.MaxValue;
                        });
                    }

                    ProcessStartInfo processInfo;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        processInfo = new ProcessStartInfo("cmd.exe",
                            "/c avrdude.exe -Cavrdude.conf -v -patmega328p -carduino -P" + gbpemuPort +
                            string.Format(" -b{0} -D -Uflash:w:firmware{1}.ino.hex:i",
                                serialNumber < 100007 ? "115200" : "57600", serialNumber < 100007 ? "" : "-old"))
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            WorkingDirectory = tempDirectory
                        };
                    }
                    else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        processInfo = new ProcessStartInfo("avrdude",
                            "-v -patmega328p -carduino -P" + gbpemuPort +
                            string.Format(" -b{0} -D -Uflash:w:firmware{1}.ino.hex:i",
                                serialNumber < 100007 ? "115200" : "57600", serialNumber < 100007 ? "" : "-old"))
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            WorkingDirectory = tempDirectory
                        };
                    }
                    else
                    {
                        throw new PlatformNotSupportedException();
                    }

                    StringBuilder sb = new StringBuilder();
                    Process? p = Process.Start(processInfo);
                    if (p != null)
                    {
                        p.OutputDataReceived += (sender, args1) => sb.AppendLine(args1.Data);
                        p.ErrorDataReceived += (sender, args1) => sb.AppendLine(args1.Data);
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        p.WaitForExit();
                    }

                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        txtboxData.Text += sb.ToString() + "\n";
                        txtboxData.Text += "..." + "done.\n\n";
                        txtboxData.CaretIndex = int.MaxValue;

                        try
                        {
                            if (sb.ToString().Contains("attempt 10 of 10"))
                                throw new Exception("Updating Failed.");

                            AvaloniaMessageBox("RetroSpy", "Update complete! Please reboot your device.", ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Info);

                        }
                        catch (Exception ex)
                        {
                            txtboxData.Text += "\nUpdater encountered an error.  Message: " + ex.Message + "\n";
                            AvaloniaMessageBox("RetroSpy", ex.Message, ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                            this.goButton.IsEnabled = true;
                        }
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            txtboxData.Text += sb.ToString() + "\n";
                            txtboxData.Text += "..." + "done.\n\n";
                            txtboxData.CaretIndex = int.MaxValue;

                            try
                            {
                                if (sb.ToString().Contains("attempt 10 of 10"))
                                    throw new Exception("Updating Failed.");
                                
                                AvaloniaMessageBox("RetroSpy", "Update complete! Please reboot your device.", ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Info);

                            }
                            catch(Exception ex)
                            {
                                txtboxData.Text += "\nUpdater encountered an error.  Message: " + ex.Message + "\n";
                                AvaloniaMessageBox("RetroSpy", ex.Message, ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                                this.goButton.IsEnabled = true;
                            }


                        });
                    }

                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        this.goButton.IsEnabled = true;
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            this.goButton.IsEnabled = true;
                        });
                    }

                }

            }
            catch (Exception ex)
            {

                if (Dispatcher.UIThread.CheckAccess())
                {
                    txtboxData.Text += "\nUpdater encountered an error.  Message: " + ex.Message + "\n";
                    AvaloniaMessageBox("RetroSpy", ex.Message, ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                    this.goButton.IsEnabled = true;
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        txtboxData.Text += "\nUpdater encountered an error.  Message: " + ex.Message + "\n";
                        AvaloniaMessageBox("RetroSpy", ex.Message, ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Error);
                        this.goButton.IsEnabled = true;
                    });
                }
            }

        }

        private void GoButton_Click(object? sender, RoutedEventArgs? e)
        {
            Thread thread = new(UpdateThread);
            thread.Start();
        }

        private static void AvaloniaMessageBox(string? title, string message, ButtonEnum buttonType, MessageBox.Avalonia.Enums.Icon iconType)
        {
            using var source = new CancellationTokenSource();
            _ = MessageBox.Avalonia.MessageBoxManager
            .GetMessageBoxStandardWindow(title ?? "Unknown Title Argument", message, buttonType, iconType)
                        .Show().ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
        }
    }
}