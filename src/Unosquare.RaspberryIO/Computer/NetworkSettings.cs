﻿namespace Unosquare.RaspberryIO.Computer
{
    using Swan;
    using Swan.Abstractions;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Represents the network information
    /// </summary>
    public class NetworkSettings : SingletonBase<NetworkSettings>
    {

        private ReadOnlyCollection<NetworkAdapter> m_Adapters;

        private NetworkSettings()
        {

        }

        /// <summary>
        /// Gets the local machine Host Name.
        /// </summary>
        public string HostName => Dns.GetHostName();

        /// <summary>
        /// Retrieves the wireless networks.
        /// </summary>
        /// <returns></returns>
        public List<WirelessNetworkInfo> RetrieveWirelessNetworks()
        {
            var result = new List<WirelessNetworkInfo>();

            foreach (var networkAdapter in RetrieveAdapters().Where(x => x.IsWireless))
            {
                var wirelessOutput = ProcessHelper.GetProcessOutputAsync("iwlist", $"{networkAdapter.Name} scanning").Result;
                var outputLines = wirelessOutput.Split('\n').Select(x => x.Trim()).Where(x => string.IsNullOrWhiteSpace(x) == false).ToArray();

                for (var i = 0; i < outputLines.Length; i++)
                {
                    var line = outputLines[i];

                    if (line.StartsWith("ESSID:") == false) continue;

                    var network = new WirelessNetworkInfo()
                    {
                        Name = line.Replace("ESSID:", "").Replace("\"", string.Empty)
                    };

                    while (true)
                    {
                        if (i + 1 >= outputLines.Length) break;

                        // move next line
                        line = outputLines[++i];

                        if (line.StartsWith("Quality="))
                        {
                            network.Quality = line.Replace("Quality=", "");
                            break;
                        }
                    }

                    while (true)
                    {
                        if (i + 1 >= outputLines.Length) break;

                        // move next line
                        line = outputLines[++i];

                        if (line.StartsWith("Encryption key:"))
                        {
                            network.IsEncrypted = line.Replace("Encryption key:", string.Empty).Trim() == "on";
                            break;
                        }
                    }

                    result.Add(network);
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves the network adapters.
        /// </summary>
        /// <returns></returns>
        public List<NetworkAdapter> RetrieveAdapters()
        {
            var result = new List<NetworkAdapter>();
            var interfacesOutput = ProcessHelper.GetProcessOutputAsync("ifconfig").Result;
            var wlanOutput = ProcessHelper.GetProcessOutputAsync("iwconfig").Result.Split('\n').Where(x => x.Contains("no wireless extensions.") == false).ToArray();
            var outputLines = interfacesOutput.Split('\n').Where(x => string.IsNullOrWhiteSpace(x) == false).ToArray();

            for (var i = 0; i < outputLines.Length; i++)
            {
                var line = outputLines[i];

                if (line[0] >= 'a' && line[0] <= 'z')
                {
                    var adapter = new NetworkAdapter
                    {
                        Name = line.Substring(0, line.IndexOf(' '))
                    };

                    if (line.IndexOf("HWaddr") > 0)
                    {
                        var startIndexHwd = line.IndexOf("HWaddr") + "HWaddr".Length;
                        adapter.MacAddress = line.Substring(startIndexHwd).Trim();
                    }

                    if (i + 1 >= outputLines.Length) break;

                    // move next line
                    line = outputLines[++i].Trim();

                    if (line.StartsWith("inet addr:"))
                    {
                        var tempIP = line.Replace("inet addr:", string.Empty).Trim();
                        tempIP = tempIP.Substring(0, tempIP.IndexOf(' '));

                        IPAddress outValue;
                        if (IPAddress.TryParse(tempIP, out outValue))
                            adapter.IPv4 = outValue;

                        if (i + 1 >= outputLines.Length) break;
                        line = outputLines[++i].Trim();
                    }

                    if (line.StartsWith("inet6 addr:"))
                    {
                        var tempIP = line.Replace("inet6 addr:", string.Empty).Trim();
                        tempIP = tempIP.Substring(0, tempIP.IndexOf('/'));

                        IPAddress outValue;
                        if (IPAddress.TryParse(tempIP, out outValue))
                            adapter.IPv6 = outValue;
                    }

                    var wlanInfo = wlanOutput.FirstOrDefault(x => x.StartsWith(adapter.Name));

                    if (wlanInfo != null)
                    {
                        adapter.IsWireless = true;

                        var startIndex = wlanInfo.IndexOf("ESSID:") + "ESSID:".Length;
                        adapter.AccessPointName = wlanInfo.Substring(startIndex).Replace("\"", string.Empty);
                    }

                    result.Add(adapter);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a Network Adapter
    /// </summary>
    public class NetworkAdapter
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the i PV4.
        /// </summary>
        public IPAddress IPv4 { get; set; }

        /// <summary>
        /// Gets or sets the i PV6.
        /// </summary>
        public IPAddress IPv6 { get; set; }

        /// <summary>
        /// Gets or sets the name of the access point.
        /// </summary>
        public string AccessPointName { get; set; }

        /// <summary>
        /// Gets or sets the mac address.
        /// </summary>
        public string MacAddress { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is wireless.
        /// </summary>
        public bool IsWireless { get; internal set; }
    }

    /// <summary>
    /// Represents a wireless network information
    /// </summary>
    public class WirelessNetworkInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the network quality.
        public string Quality { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance is encrypted.
        /// </summary>
        public bool IsEncrypted { get; internal set; }
    }
}