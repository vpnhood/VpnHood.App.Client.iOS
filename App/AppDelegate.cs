using NetworkExtension;

namespace App;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    private readonly NETunnelProviderManager _vpnManager = new ();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var window = new UIWindow(UIScreen.MainScreen.Bounds);
        var rootView = new UIView
        {
            BackgroundColor = UIColor.SystemBackground
        };

        // Init Button
        var initButton = new UIButton(UIButtonType.System);
        initButton.SetTitle("Init VPN Config", UIControlState.Normal);
        initButton.Frame = new CGRect(50, 200, UIScreen.MainScreen.Bounds.Width - 100, 50);
        initButton.TouchUpInside += (_, _) => { InitVpnTunnelProviderManager(); };

        // Connect Button
        var connectButton = new UIButton(UIButtonType.System);
        connectButton.SetTitle("Connect VPN", UIControlState.Normal);
        connectButton.Frame = new CGRect(50, 300, UIScreen.MainScreen.Bounds.Width - 100, 50);
        connectButton.TouchUpInside += (_, _) =>
        {
            _vpnManager.LoadFromPreferences(
                err =>
                {
                    if (err == null)
                    {
                        Console.WriteLine("Loaded Preferences");
                        _vpnManager.Connection.StartVpnTunnel(out var nsError);
                        Console.WriteLine($"RESULT: {nsError}");
                    }
                    else
                    {
                        Console.WriteLine("Error loading preferences");
                        Console.WriteLine(err);
                    }
                }
            );
        };

        rootView.AddSubviews(initButton, connectButton);

        window.RootViewController = new UIViewController { View = rootView };
        window.MakeKeyAndVisible();

        return true;
    }

    private void InitVpnTunnelProviderManager2()
    {
        var vpnManager = new NETunnelProviderManager();

        var providerProtocol = new NETunnelProviderProtocol
        {
            ProviderBundleIdentifier = "com.vpnhood.simpleNetwork.SampleNE", 
            ServerAddress = "54.157.244.193",
            EnforceRoutes = true,
            IncludeAllNetworks = true
            //ProviderConfiguration = new NSDictionary<NSString, NSObject>();
            // ProviderConfiguration = new NSDictionary<NSString, NSObject>(
            //     new NSString("port"), new NSString("5000"),
            //     new NSString("server"), new NSString("54.157.244.193"),
            //     new NSString("ip"), new NSString("10.8.0.2"),
            //     new NSString("subnet"), new NSString("255.255.255.0"),
            //     new NSString("mtu"), new NSString("1400"),
            //     new NSString("dns"), new NSString("8.8.8.8,8.4.4.4")
            // )
        };

        vpnManager.ProtocolConfiguration = providerProtocol;
        vpnManager.LocalizedDescription = "FooApp";
        vpnManager.Enabled = true;

        vpnManager.SaveToPreferences((error) =>
        {
            if (error != null)
            {
                Console.WriteLine($"Failed to save VPN config: {error.LocalizedDescription}");
            }
            else
            {
                Console.WriteLine("VPN config saved successfully.");
            }
        });
    }
    
    private void InitVpnTunnelProviderManager()
    {
        var providerProtocol = new NETunnelProviderProtocol();
        providerProtocol.ProviderBundleIdentifier = "com.vpnhood.simpleNetwork.SampleNE";

        var keys = new[]
        {
            new NSString("port"),
            new NSString("server"),
            new NSString("ip"),
            new NSString("subnet"),
            new NSString("mtu"),
            new NSString("dns")
        };

        var objects = new NSObject[]
        {
            new NSString("5000"),
            new NSString("54.157.244.193"),
            new NSString("10.8.0.2"),
            new NSString("255.255.255.0"),
            new NSString("1400"),
            new NSString("8.8.8.8,8.4.4.4")
        };

        // var dictionary = new NSDictionary<NSString, NSObject>(keys, objects);
        //providerProtocol.ProviderConfiguration = dictionary;
        providerProtocol.ProviderConfiguration = new NSDictionary<NSString, NSObject>();
        providerProtocol.ServerAddress = "54.157.244.193";
        providerProtocol.EnforceRoutes = true;
        providerProtocol.IncludeAllNetworks = true;
        _vpnManager.ProtocolConfiguration = providerProtocol;
        _vpnManager.LocalizedDescription = "FooApp";
        _vpnManager.Enabled = true;
        _vpnManager.SaveToPreferences(completionHandler: SaveToPreferences);
    }

    private static void SaveToPreferences(NSError? err)
    {
        if (err == null)
        {
            Console.WriteLine("Saved Preferences");
        }
        else
        {
            Console.WriteLine("Error saving preferences");
            Console.WriteLine(err);
        }
    }
}