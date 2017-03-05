using SSQLib;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace GoonPug
{
    class ServerUpdater
    {
        public const string Hostname = "csgo.sub.io";
        private readonly IEnumerable<IPEndPoint> _endpoints;
        private readonly SSQL _query;

        public ServerUpdater()
        {
            var address = Dns.GetHostEntry(Hostname).AddressList.First();

            _endpoints = new List<IPEndPoint> { new IPEndPoint(address, 27015), new IPEndPoint(address, 27017) };
            _query = new SSQL();
        }
        
        public ICollection<ServerInfo> PollServers()
        {
            try
            {
                return _endpoints.Select(_query.Server).ToArray();
            }
            catch
            {
                return new ServerInfo[] {
                    new ServerInfo() {
                            Name = "Error fetching server info.",
                            PlayerCount = "0",
                            MaxPlayers = "0"
                    }
                };
            }
        }
    }
}
