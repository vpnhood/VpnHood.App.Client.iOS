using NetworkExtension;

namespace Extension;

public class PacketTunnelProvider : NEPacketTunnelProvider
{
    public override void StartTunnel(NSDictionary<NSString, NSObject>? options,
        Action<NSError> startTunnelCompletionHandler)
    {
        // Start your VPN tunnel here
        Console.WriteLine("Starting tunnel...");

        // For now, just call completionHandler with no error to signal success
        startTunnelCompletionHandler(null);
    }

    public override void StopTunnel(NEProviderStopReason reason, Action completionHandler)
    {
        Console.WriteLine("Stopping tunnel...");

        // Clean up resources and stop tunnel
        completionHandler();
    }
}