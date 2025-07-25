using VpnHood.AppLib;
using VpnHood.AppLib.Abstractions;
using VpnHood.AppLib.Services.Ads;
using VpnHood.AppLib.Utils;
using VpnHood.Core.Client.Abstractions;

namespace VpnHood.App.Client.Ios.Test;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // initialize VpnHoodApp
        if (!VpnHoodApp.IsInit)
        {
            VpnHoodApp.Init(new IosDevice(), BuildAppOptions());
            VpnHoodApp.Instance.Settings.UserSettings.DebugData1 = DebugCommands.UseTcpOverTun;
        }

        // create the temporary minimal window
        var window = new UIWindow(UIScreen.MainScreen.Bounds);
        var rootView = new UIView
        {
            BackgroundColor = UIColor.SystemBackground
        };

        // Add Connect Button
        var connectButton = new UIButton(UIButtonType.System);
        connectButton.SetTitle("Connect", UIControlState.Normal);
        connectButton.Frame = new CGRect(50, 300, UIScreen.MainScreen.Bounds.Width - 100, 50);
        connectButton.TouchUpInside += (_, _) =>
        {
            VpnHoodApp.Instance.Connect();
        };

        rootView.AddSubviews(connectButton);
        window.RootViewController = new UIViewController { View = rootView };
        window.MakeKeyAndVisible();
        return true;
    }

    private static AppOptions BuildAppOptions()
    {
        // load app configs
        // TODO: set a local location for app, it does not need to be shared with extension
        var storageFolderPath = AppOptions.BuildStorageFolderPath("VpnHoodTest");

        // load app settings and resources
        // ReSharper disable once UseObjectOrCollectionInitializer
        var resources = new AppResources();
        resources.Strings.AppName = "VpnHoodTest";

        return new AppOptions(appId: "com.vpnhood.test.ios", "VpnHoodConnect", isDebugMode: true)
        {
            StorageFolderPath = storageFolderPath,
            AccessKeys = [ClientOptions.SampleAccessKey],
            Resources = resources,
            UiName = "VpnHoodTest",
            IsAddAccessKeySupported = false,
            UseInternalLocationService = false,
            AdOptions = new AppAdOptions
            {
                PreloadAd = false
            }
        };
    }

}