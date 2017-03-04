using SSQLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace GoonPug
{
    public delegate void ServerUpdateEventHandler(object sender, ServerUpdateEventArgs e);

    public class ServerUpdateEventArgs : EventArgs
    {
        public readonly IEnumerable<ServerInfo> ServerInfos;
        public readonly bool SendAlert;

        public ServerUpdateEventArgs(IEnumerable<ServerInfo> serverInfos, bool sendAlert)
        {
            ServerInfos = serverInfos;
            SendAlert = sendAlert;
        }
    }

    class ServerUpdater
    {
        public const string Hostname = "csgo.sub.io";
        public event ServerUpdateEventHandler ServerUpdate;

        private readonly IEnumerable<IPEndPoint> Endpoints;

#if DEBUG
        private const int MinPlayers = 0;
        private const int AlertMinutes = 1;
#else
        private const int MinPlayers = 5;
        private const int AlertMinutes = 10;
#endif

        private DateTime lastAlert;

        public ServerUpdater()
        {
            var address = Dns.GetHostEntry(Hostname).AddressList.First();

            Endpoints = new List<IPEndPoint> { new IPEndPoint(address, 27015), new IPEndPoint(address, 27017) };

        }

        public void PollServers()
        {
            SSQL query = new SSQL();

            while (true)
            {
                try
                {
                    var serverInfos = Endpoints.Select(query.Server);

                    bool alert = false;
                    if (serverInfos.Any(ServerNeedsPlayers) && NeedsAlert())
                    {
                        lastAlert = DateTime.UtcNow;
                        alert = true;
                    };

                    ServerUpdate(this, new ServerUpdateEventArgs(serverInfos, alert));
                }
                catch
                {
                    ServerUpdate(this, new ServerUpdateEventArgs(
                        new ServerInfo[] { new ServerInfo() {
                            Name = "Error fetching server info.",
                            PlayerCount = "0",
                            MaxPlayers = "0"
                        } },
                        false
                    ));
                }

                Thread.Sleep(30000);
            }
        }
        
        private static bool ServerNeedsPlayers(ServerInfo server)
        {
            var players = int.Parse(server.PlayerCount);
            return players > MinPlayers && players < 11;
        }

        private bool NeedsAlert()
        {
            return DateTime.UtcNow - lastAlert > TimeSpan.FromMinutes(AlertMinutes);
        }
    }
}
