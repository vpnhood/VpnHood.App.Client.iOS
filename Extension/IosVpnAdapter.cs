using Microsoft.Extensions.Logging;
using NetworkExtension;
using System.Net;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;
using VpnHood.Core.VpnAdapters.Abstractions;

namespace VpnHood.Core.VpnAdapters.IosTun;

public class IosVpnAdapter(NEPacketTunnelProvider tunnelProvider, IosVpnAdapterSettings settings)
    : TunVpnAdapter(settings)
{
    private NEPacketTunnelFlow? _packetFlow;
    private readonly List<NEIPv4Route> _ipv4Routes = [];
    private readonly List<NEIPv6Route> _ipv6Routes = [];
    private readonly List<IPAddress> _dnsServers = [];
    private readonly List<IpNetwork> _ipv4Networks = [];
    private readonly List<IpNetwork> _ipv6Networks = [];
    private int? _mtu;

    private readonly byte[] _writeBuffer = new byte[0xFFFF];

    public override bool IsNatSupported => false;
    public override bool IsAppFilterSupported => false;
    protected override bool IsSocketProtectedByBind => false;
    protected override Task AdapterAdd(CancellationToken cancellationToken) => Task.CompletedTask;
    protected override void AdapterRemove() { }

    protected override async Task AdapterOpen(CancellationToken cancellationToken)
    {
        VhLogger.Instance.LogDebug("Establishing iOS tun adapter...");
        var settings = new NEPacketTunnelNetworkSettings("vpnhood");

        // Set the default gateway for IPv4 and IPv6
        if (_ipv4Networks.Count > 0)
        {
            settings.IPv4Settings = new NEIPv4Settings(
                _ipv4Networks.Select(x => x.Prefix.ToString()).ToArray(),
                _ipv4Networks.Select(x => x.SubnetMask.ToString()).ToArray());

            settings.IPv4Settings.IncludedRoutes = _ipv4Routes.ToArray();
        }

        // Set the default gateway for IPv6
        if (_ipv6Networks.Count > 0)
        {
            settings.IPv6Settings = new NEIPv6Settings(
                _ipv6Networks.Select(x => x.Prefix.ToString()).ToArray(),
                _ipv6Networks.Select(x => NSNumber.FromInt32(x.PrefixLength)).ToArray());
            
            settings.IPv6Settings.IncludedRoutes = _ipv6Routes.ToArray();
        }

        // Set DNS servers if any are provided
        if (_dnsServers.Count > 0)
            settings.DnsSettings = new NEDnsSettings(_dnsServers.Select(x=>x.ToString()) .ToArray());

        if (_mtu.HasValue)
        {
            settings.Mtu = NSNumber.FromInt32(_mtu.Value);
        }

        await tunnelProvider.SetTunnelNetworkSettingsAsync(settings);

        _packetFlow = tunnelProvider.PacketFlow;
        VhLogger.Instance.LogDebug("iOS tun adapter has been established.");
    }

    protected override void AdapterClose()
    {
        _ipv4Networks.Clear();
        _ipv6Networks.Clear();
        _ipv4Routes.Clear();
        _ipv6Routes.Clear();
        _dnsServers.Clear();
        _mtu = null;
        _packetFlow = null;
    }

    protected override Task AddAddress(IpNetwork ipNetwork, CancellationToken cancellationToken)
    {
        if (ipNetwork.IsV4)
            _ipv4Networks.Add(ipNetwork);
        else
            _ipv6Networks.Add(ipNetwork);

        return Task.CompletedTask;
    }

    protected override Task AddRoute(IpNetwork ipNetwork, CancellationToken cancellationToken)
    {
        if (ipNetwork.IsV4)
            _ipv4Routes.Add(new NEIPv4Route(ipNetwork.Prefix.ToString(), ipNetwork.SubnetMask.ToString()));
        else
            _ipv6Routes.Add(new NEIPv6Route(ipNetwork.Prefix.ToString(), ipNetwork.PrefixLength));

        return Task.CompletedTask;
    }

    protected override Task AddNat(IpNetwork ipNetwork, CancellationToken cancellationToken) =>
        throw new NotSupportedException("iOS does not support NAT.");

    protected override Task SetSessionName(string sessionName, CancellationToken cancellationToken) 
        => Task.CompletedTask;

    protected override Task SetMetric(int metric, bool ipV4, bool ipV6, CancellationToken cancellationToken) 
        => Task.CompletedTask;

    protected override Task SetMtu(int mtu, bool ipV4, bool ipV6, CancellationToken cancellationToken)
    {
        _mtu = mtu;
        return Task.CompletedTask;
    }

    protected override Task SetDnsServers(IPAddress[] dnsServers, CancellationToken cancellationToken)
    {
        _dnsServers.AddRange(dnsServers);
        return Task.CompletedTask;
    }

    protected override Task SetAllowedApps(string[] packageIds, CancellationToken cancellationToken) =>
        Task.CompletedTask; // MDM-only on iOS

    protected override Task SetDisallowedApps(string[] packageIds, CancellationToken cancellationToken) =>
        Task.CompletedTask; // MDM-only on iOS

    protected override string AppPackageId =>
        NSBundle.MainBundle.BundleIdentifier ?? throw new Exception("Could not get the app BundleIdentifier!");

    public override bool ProtectSocket(System.Net.Sockets.Socket socket) => true;

    public override bool ProtectSocket(System.Net.Sockets.Socket socket, IPAddress ipAddress) => true;

    protected override void WaitForTunRead() => Thread.Sleep(10);

    protected override void WaitForTunWrite() => Thread.Sleep(10);

    protected override bool WritePacket(IpPacket ipPacket)
    {
        if (_packetFlow == null)
            throw new InvalidOperationException("Packet flow is not initialized.");

        var buffer = ipPacket.GetUnderlyingBufferUnsafe(_writeBuffer, out var offset, out var length);
        var data = NSData.FromArray(buffer[offset..(offset + length)]);
        
        // todo: optimize by using batch after we get first IOs release
        _packetFlow.WritePackets([data], [NSNumber.FromInt32(0)]);

        return true;
    }

    protected override void StartReadingPackets()
    {
        if (_packetFlow == null)
            throw new InvalidOperationException("Packet flow is not initialized.");

        _packetFlow.ReadPackets(OnPacketsReceived);
    }

    private void OnPacketsReceived(NSData[] packets, NSNumber[] protocols)
    {
        foreach (var packetBuffer in packets)
        {
            // todo: for better performance try to use bytes and convert by Marshal, lets do it later
            var buffer = packetBuffer.ToArray();
            var ipPacket = PacketBuilder.Parse(buffer);
            OnPacketReceived(ipPacket);
        }
    }

    protected override bool ReadPacket(byte[] buffer)
    {
        throw new NotSupportedException("Use StartReadingPackets as iOS already handle it.");
    }
}
