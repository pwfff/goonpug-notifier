using SSQLib;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace goonpug
{
    class GoonPugApplicationContext : ApplicationContext
    {
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayIconContextMenu;
        private ToolStripMenuItem CloseMenuItem;

        private readonly SynchronizationContext synchronizationContext;
        private readonly ServerUpdater serverUpdater;

        public GoonPugApplicationContext()
        {
            serverUpdater = new ServerUpdater();
            serverUpdater.ServerUpdate += OnServerUpdate;

            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            synchronizationContext = SynchronizationContext.Current;

            InitializeComponent();

            TrayIcon.Visible = true;
        }

        private void InitializeComponent()
        {
            TrayIcon = new NotifyIcon();

            TrayIcon.BalloonTipIcon = ToolTipIcon.Info;
            TrayIcon.Text = "sub.io Server Status";
            
            TrayIcon.Icon = Properties.Resources.TrayIcon;
            
            //Optional - Add a context menu to the TrayIcon:
            TrayIconContextMenu = new ContextMenuStrip();
            CloseMenuItem = new ToolStripMenuItem();
            TrayIconContextMenu.SuspendLayout();

            // 
            // CloseMenuItem
            // 
            CloseMenuItem.Name = "Close";
            CloseMenuItem.Size = new Size(152, 22);
            CloseMenuItem.Text = "Exit";
            CloseMenuItem.Click += new EventHandler(CloseMenuItem_Click);
            // 
            // TrayIconContextMenu
            // 
            TrayIconContextMenu.Items.AddRange(new ToolStripItem[] { CloseMenuItem });
            TrayIconContextMenu.Name = "Server Statuses";
            TrayIconContextMenu.Size = new Size(153, 70);

            TrayIconContextMenu.ResumeLayout(false);
            TrayIcon.ContextMenuStrip = TrayIconContextMenu;

            Task.Run(() => serverUpdater.PollServers());
        }

        private void OnServerUpdate(object sender, ServerUpdateEventArgs e)
        {
            if (TrayIconContextMenu.InvokeRequired)
            {
                TrayIconContextMenu.Invoke(new Action<object, ServerUpdateEventArgs>(OnServerUpdate), sender, e);
                return;
            }

            if (e.SendAlert)
            {
                var alertText = "goonman players needed!\n" + string.Join("\n", e.ServerInfos.Select(s => s.Info()));
                TrayIcon.ShowBalloonTip(10000, "sub.io", alertText, ToolTipIcon.Warning);
            }

            TrayIconContextMenu.Items.Clear();

            var menuItems = e.ServerInfos.Select(
                server => ServerToolStripMenuItem.GetToolStripMenuItem(server, new EventHandler(OnServerClick)
            ));
            TrayIconContextMenu.Items.AddRange(menuItems.ToArray());

            TrayIconContextMenu.Items.Add(CloseMenuItem);
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            TrayIcon.Visible = false;
        }
        
        private void OnServerClick(object sender, EventArgs e)
        {
            var menuItem = (ServerToolStripMenuItem)sender;
            System.Diagnostics.Process.Start(
                string.Format("steam://run/730//+connect {0}:{1}", ServerUpdater.Hostname, menuItem.server.Port)
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
    }
}
