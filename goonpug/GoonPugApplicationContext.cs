using SSQLib;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoonPug
{
    class GoonPugApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayIconContextMenu;
        private ToolStripMenuItem _closeMenuItem;
        
        private readonly ServerUpdater serverUpdater;

#if DEBUG
        private const int AlertMinutes = 1;
        private const int MinPlayers = 0;
        private const int MaxPlayers = 999;
#else
        private const int AlertMinutes = 10;
        private const int MinPlayers = 5;
        private const int MaxPlayers = 11;
#endif

        private DateTime _lastAlert;

        public GoonPugApplicationContext()
        {
            serverUpdater = new ServerUpdater();

            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            
            InitializeComponent();

            _trayIcon.Visible = true;

            PollServers();
        }

        private void InitializeComponent()
        {
            _trayIcon = new NotifyIcon();

            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.Text = "sub.io Server Status";
            
            _trayIcon.Icon = Properties.Resources.TrayIcon;
            
            //Optional - Add a context menu to the TrayIcon:
            _trayIconContextMenu = new ContextMenuStrip();
            _closeMenuItem = new ToolStripMenuItem();
            _trayIconContextMenu.SuspendLayout();
            
            _closeMenuItem.Name = "Close";
            _closeMenuItem.Size = new Size(152, 22);
            _closeMenuItem.Text = "Exit";
            _closeMenuItem.Click += new EventHandler(CloseMenuItem_Click);

            _trayIconContextMenu.Items.AddRange(new ToolStripItem[] { _closeMenuItem });
            _trayIconContextMenu.Name = "Server Statuses";
            _trayIconContextMenu.Size = new Size(153, 70);

            _trayIconContextMenu.ResumeLayout(false);
            _trayIcon.ContextMenuStrip = _trayIconContextMenu;
        }

        public async Task PollServers()
        {
            while (true)
            {
                var pollTask = Task.Run(() => serverUpdater.PollServers());
                var serverInfos = await pollTask;

                if (serverInfos.Any(s => s.ServerNeedsPlayers(MinPlayers, MaxPlayers)) && NeedsAlert())
                {
                    _lastAlert = DateTime.UtcNow;
                    var alertText = "goonman players needed!\n" + string.Join("\n", serverInfos.Select(s => s.Info()));
                    _trayIcon.ShowBalloonTip(10000, "sub.io", alertText, ToolTipIcon.Warning);
                }

                _trayIconContextMenu.SuspendLayout();
                _trayIconContextMenu.Items.Clear();

                var menuItems = serverInfos.Select(
                    server => ServerToolStripMenuItem.GetToolStripMenuItem(server, new EventHandler(OnServerClick)
                ));
                _trayIconContextMenu.Items.AddRange(menuItems.ToArray());

                _trayIconContextMenu.Items.Add(_closeMenuItem);
                _trayIconContextMenu.ResumeLayout(true);
                
                await Task.Delay(30000);
            }
        }

        private bool NeedsAlert()
        {
            return DateTime.UtcNow - _lastAlert > TimeSpan.FromMinutes(AlertMinutes);
        }
        
        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            _trayIcon.Visible = false;
        }
        
        private void OnServerClick(object sender, EventArgs e)
        {
            var menuItem = (ServerToolStripMenuItem)sender;
            System.Diagnostics.Process.Start(
                string.Format("steam://connect/{0}:{1}", ServerUpdater.Hostname, menuItem.server.Port)
            );
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
                Application.Exit();
        }
    }

    public class ServerToolStripMenuItem : ToolStripMenuItem
    {
        public readonly ServerInfo server;

        public ServerToolStripMenuItem(string text, Image image, EventHandler onClick, string name, ServerInfo server)
            : base(text, image, onClick, name)
        {
            this.server = server;
        }

        public static ToolStripMenuItem GetToolStripMenuItem(ServerInfo server, EventHandler onClick)
        {
            return new ServerToolStripMenuItem(
                server.Info(),
                null,
                onClick,
                server.Name,
                server
            );
        }
    }

    public static class ServerInfoExtensions
    {
        public static string Info(this ServerInfo server)
        {
            return string.Format("{0}: {1}/{2}", server.Name, server.PlayerCount, server.MaxPlayers);
        }

        public static bool ServerNeedsPlayers(this ServerInfo server, int minPlayers, int maxPlayers)
        {
            var players = int.Parse(server.PlayerCount);
            return players > minPlayers && players < maxPlayers;
        }
    }
}
