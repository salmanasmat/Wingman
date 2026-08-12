using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Wingman.Models;

namespace Wingman.Services
{
    public class NetworkIntelService
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        public async Task FetchNetworkDetailsAsync(SystemState state)
        {
            try
            {
                // 1. Local IP & MAC
                string localIp = GetLocalIpAddress();
                string mac = GetMacAddress();

                // 2. Public IP
                string publicIp = await GetPublicIpAddressAsync();

                // 3. Default Gateway
                string gateway = GetDefaultGateway();

                // 4. WiFi Info
                var (ssid, signal, radio, auth) = GetWifiDetails();

                lock (state.Lock)
                {
                    state.LocalIp = localIp;
                    state.PublicIp = publicIp;
                    state.Mac = mac;
                    state.Gateway = gateway;
                    state.WifiSsid = ssid;
                    state.WifiSignal = signal;
                    state.WifiRadio = radio;
                    state.WifiAuth = auth;
                }
            }
            catch (Exception ex)
            {
                LoggingService.WriteLog($"Net Info Fetch Error: {ex.Message}", "ERROR");
            }
        }

        private string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        {
                            return ip.ToString();
                        }
                    }
                }
                catch { }
            }
            return "127.0.0.1";
        }

        private string GetMacAddress()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        string address = nic.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(address) && address.Length == 12)
                        {
                            return Regex.Replace(address, ".{2}", "$0:").TrimEnd(':');
                        }
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private async Task<string> GetPublicIpAddressAsync()
        {
            try
            {
                string ip = await HttpClient.GetStringAsync("https://api.ipify.org");
                return ip.Trim();
            }
            catch
            {
                return "Offline";
            }
        }

        private string GetDefaultGateway()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up)
                    {
                        var props = nic.GetIPProperties();
                        foreach (var gw in props.GatewayAddresses)
                        {
                            if (gw.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return gw.Address.ToString();
                            }
                        }
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private (string ssid, string signal, string radio, string auth) GetWifiDetails()
        {
            string ssid = "N/A";
            string signal = "0%";
            string radio = "N/A";
            string auth = "N/A";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    using var reader = new StringReader(output);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.StartsWith("SSID") && !line.StartsWith("BSSID"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1) ssid = parts[1].Trim();
                        }
                        else if (line.StartsWith("Signal"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1) signal = parts[1].Trim();
                        }
                        else if (line.StartsWith("Radio type"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1) radio = parts[1].Trim();
                        }
                        else if (line.StartsWith("Authentication"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1) auth = parts[1].Trim();
                        }
                    }
                }
            }
            catch { }

            return (ssid, signal, radio, auth);
        }
    }
}
