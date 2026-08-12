using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Wingman.Models;

namespace Wingman.Services
{
    public class PingService
    {
        private static readonly int[] TargetPorts = new int[] { 53, 80, 443, 445 };

        public void SyncTargets(SystemState state, List<TargetItem> targetConfigs)
        {
            lock (state.Lock)
            {
                var configNames = targetConfigs.ToDictionary(t => t.Name, t => t.Host);
                var currentNames = state.Pings.Keys.ToList();

                foreach (var kvp in configNames)
                {
                    if (!state.Pings.ContainsKey(kvp.Key))
                    {
                        state.Pings[kvp.Key] = new PingTargetStatus
                        {
                            Host = kvp.Value,
                            History = new List<double>(Enumerable.Repeat(0.0, 20)),
                            Status = "init",
                            LastMs = 0
                        };
                    }
                    else
                    {
                        state.Pings[kvp.Key].Host = kvp.Value;
                    }
                }

                foreach (var name in currentNames)
                {
                    if (!configNames.ContainsKey(name))
                    {
                        state.Pings.Remove(name);
                    }
                }
            }
        }

        public async Task PingSingleHostAsync(SystemState state, string name, string host)
        {
            double durationMs = 0;
            string status = "crit";
            bool found = false;

            // 1. Try TCP connect check across ports 53, 80, 443, 445
            foreach (int port in TargetPorts)
            {
                try
                {
                    using var client = new TcpClient();
                    var stopwatch = Stopwatch.StartNew();
                    var connectTask = client.ConnectAsync(host, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(400)) == connectTask && client.Connected)
                    {
                        stopwatch.Stop();
                        durationMs = stopwatch.Elapsed.TotalMilliseconds;
                        status = durationMs < 200 ? "ok" : "warn";
                        found = true;
                        break;
                    }
                }
                catch { }
            }

            // 2. ICMP Ping fallback
            if (!found)
            {
                try
                {
                    using var pinger = new Ping();
                    var reply = await pinger.SendPingAsync(host, 500);
                    if (reply.Status == IPStatus.Success)
                    {
                        durationMs = reply.RoundtripTime;
                        status = durationMs < 200 ? "ok" : "warn";
                        found = true;
                    }
                }
                catch { }
            }

            if (!found)
            {
                durationMs = 0;
                status = "crit";
            }

            lock (state.Lock)
            {
                if (state.Pings.TryGetValue(name, out var targetStatus))
                {
                    targetStatus.History.Add(durationMs);
                    if (targetStatus.History.Count > 20)
                    {
                        targetStatus.History.RemoveAt(0);
                    }
                    targetStatus.LastMs = (int)durationMs;
                    targetStatus.Status = status;
                }
            }
        }
    }
}
