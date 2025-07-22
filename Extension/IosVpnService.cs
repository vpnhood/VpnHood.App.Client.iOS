using Microsoft.Extensions.Logging;
using NetworkExtension;
using VpnHood.Core.Client.VpnServices.Abstractions;
using VpnHood.Core.Client.VpnServices.Host;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Tunneling.Sockets;
using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.VpnAdapters.IosTun;

public class IosVpnService : NEPacketTunnelProvider, IVpnServiceHandler
{
    private readonly VpnServiceHost _vpnServiceHost;

    // TODO: VpnServiceConfigFolder should point to a file shared with the app group,
    // so that it can be accessed by other components of the app.
    public static string VpnServiceConfigFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vpn-service");

    public IosVpnService()
    {
        // Ensure the VPN service config folder exists
        VhLogger.Instance.LogDebug("IosVpnService has been initialized. Config folder: {ConfigFolder}", VpnServiceConfigFolder);
        _vpnServiceHost = new VpnServiceHost(VpnServiceConfigFolder, this, new SocketFactory());
    }

    // this method is called when the VPN service is started
    public override void StartTunnel(
        NSDictionary<NSString, NSObject>? options, Action<NSError> startTunnelCompletionHandler)
    {
        // Start your VPN tunnel here
        Console.WriteLine("Starting tunnel...");
        _ = _vpnServiceHost.TryConnect(true);

        // For now, just call completionHandler with no error to signal success
        startTunnelCompletionHandler(null!);
    }

    public override void StopTunnel(NEProviderStopReason reason, Action completionHandler)
    {
        Console.WriteLine("Stopping tunnel...");
        _ = _vpnServiceHost.TryDisconnect();
        
        // Clean up resources and stop tunnel
        completionHandler();
    }

    public IVpnAdapter CreateAdapter(VpnAdapterSettings adapterSettings, string? debugData)
    {
        return new IosVpnAdapter(this, new IosVpnAdapterSettings {
            AdapterName = adapterSettings.AdapterName, 
            Blocking = adapterSettings.Blocking,
            AutoDisposePackets = adapterSettings.AutoDisposePackets,
            AutoRestart = adapterSettings.AutoRestart,
            MaxPacketSendDelay = adapterSettings.MaxPacketSendDelay,
            QueueCapacity = adapterSettings.QueueCapacity,
            AutoMetric = adapterSettings.AutoMetric
        });
    }

    public void ShowNotification(ConnectionInfo connectionInfo)
    {
        // iOS does not support foreground notifications and not need to show a notification.
    }

    public void StopNotification()
    {
        // iOS does not support foreground notifications and not need to show a notification.
    }

    public void StopSelf()
    {
        CancelTunnel(null);
    }
}
