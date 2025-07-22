using Microsoft.Extensions.Logging;
using NetworkExtension;
using VpnHood.Core.Client.Device;
using VpnHood.Core.Client.Device.UiContexts;
using VpnHood.Core.Toolkit.Logging;

namespace VpnHood.App.Client.Ios.Test;

public class IosDevice : IDevice
{
    private readonly NETunnelProviderManager _vpnManager = new();

    // TODO: VpnServiceConfigFolder should point to a file shared with the app group,
    // so that it can be accessed by other components of the app.
    public string VpnServiceConfigFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vpn-service");

    public bool IsBindProcessToVpnSupported => false;
    public bool IsExcludeAppsSupported => false;
    public bool IsIncludeAppsSupported => false;
    public bool IsAlwaysOnSupported => false;
    public string OsInfo { get; } = $"{UIDevice.CurrentDevice.SystemName}: {UIDevice.CurrentDevice.Model}, iOS: {UIDevice.CurrentDevice.SystemVersion}";
    public bool IsTv => false;
    public DeviceMemInfo? MemInfo => null;
    public static bool IsVpnServiceProcess => true; // iOS always runs in the VPN service process.

    public void BindProcessToVpn(bool value)
    {
        if (value)
            throw new NotSupportedException("Binding process to VPN is not supported on iOS.");
    }

    public static IosDevice Create()
    {
        return new IosDevice();
    }

    public DeviceAppInfo[] InstalledApps =>
        throw new NotSupportedException("VPN filtering (App Filter) is not supported for regular iOS VPN apps");


    public Task RequestVpnService(IUiContext? uiContext, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return Task.CompletedTask; // not needed for iOS, as VPN service is managed by the system.
    }

    public async Task StartVpnService(CancellationToken cancellationToken)
    {
        var providerProtocol = new NETunnelProviderProtocol();
        providerProtocol.ProviderBundleIdentifier = "com.vpnhood.test.ios.networkextension";

        // we may use these settings to pass shared folder or other configuration
        // var dictionary = new NSDictionary<NSString, NSObject>(keys, objects);
        //providerProtocol.ProviderConfiguration = dictionary;
        providerProtocol.ProviderConfiguration = new NSDictionary<NSString, NSObject>();
        providerProtocol.ServerAddress = "1.1.1.1"; // may not be required. we will read it from file
        providerProtocol.EnforceRoutes = true;
        providerProtocol.IncludeAllNetworks = true; // temporary true, we will set it to false later and use routes
        _vpnManager.ProtocolConfiguration = providerProtocol;
        _vpnManager.LocalizedDescription = "VpnHood! Test App";
        _vpnManager.Enabled = true;

        // save the VPN configuration
        var saveTaskSource = new TaskCompletionSource();
        _vpnManager.SaveToPreferences(nsError =>
        {
            if (nsError != null!)
                saveTaskSource.TrySetException(new Exception(nsError.Description));
            else
                saveTaskSource.TrySetResult();
        });
        await saveTaskSource.Task;

        // now we can load the preferences as required by the VPN service
        var loadTaskSource = new TaskCompletionSource();
        _vpnManager.LoadFromPreferences(nsError =>
        {
            if (nsError != null!)
                loadTaskSource.TrySetException(new Exception(nsError.Description));
            else
                loadTaskSource.TrySetResult();
        });
        await loadTaskSource.Task;

        // At this point, the VPN configuration is loaded and ready to be used.
        VhLogger.Instance.LogDebug("Preferences loaded successfully. Starting the Vpn.");
        _vpnManager.Connection.StartVpnTunnel(out var nsStartError);
        if (nsStartError != null)
            throw new Exception($"Failed to start VPN tunnel: {nsStartError.Description}");
    }

    public void Dispose()
    {
    }
}
