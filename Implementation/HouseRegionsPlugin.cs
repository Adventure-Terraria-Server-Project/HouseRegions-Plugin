using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;

using Terraria.Plugins.Common;
using Terraria.Plugins.Common.Hooks;

using TerrariaApi.Server;
using TShockAPI;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  [ApiVersion(1, 21)]
  public class HouseRegionsPlugin: TerrariaPlugin {
    private const string TracePrefix = @"[Housing] ";
    public const string Define_Permission          = "houseregions.define";
    public const string Delete_Permission          = "houseregions.delete";
    public const string Share_Permission           = "houseregions.share";
    public const string ShareWithGroups_Permission = "houseregions.sharewithgroups";
    public const string NoLimits_Permission        = "houseregions.nolimits";
    public const string HousingMaster_Permission   = "houseregions.housingmaster";
    public const string Cfg_Permission             = "houseregions.cfg";

    public static HouseRegionsPlugin LatestInstance { get; private set; }

    public static string DataDirectory {
      get { return Path.Combine(TShock.SavePath, "House Regions"); }
    }

    public static string ConfigFilePath {
      get { return Path.Combine(HouseRegionsPlugin.DataDirectory, "Config.xml"); }
    }

    private bool hooksEnabled;
    internal PluginTrace Trace { get; private set; }
    protected PluginInfo PluginInfo { get; private set; }
    protected Configuration Config { get; private set; }
    protected GetDataHookHandler GetDataHookHandler { get; private set; }
    protected UserInteractionHandler UserInteractionHandler { get; private set; }
    public HousingManager HousingManager { get; private set; }


    public HouseRegionsPlugin(Main game): base(game) {
      this.PluginInfo = new PluginInfo(
        "House Regions",
        Assembly.GetAssembly(typeof(HouseRegionsPlugin)).GetName().Version,
        "",
        "CoderCow",
        "A simple TShock regions wrapper for player housing purposes."
      );

      this.Order = 1;
      #if DEBUG
      if (Debug.Listeners.Count == 0)
        Debug.Listeners.Add(new ConsoleTraceListener());
      #endif

      this.Trace = new PluginTrace(HouseRegionsPlugin.TracePrefix);
      HouseRegionsPlugin.LatestInstance = this;
    }

    #region [Initialization]
    public override void Initialize() {
      ServerApi.Hooks.GamePostInitialize.Register(this, this.Game_PostInitialize);

      this.AddHooks();
    }

    private void Game_PostInitialize(EventArgs e) {
      ServerApi.Hooks.GamePostInitialize.Deregister(this, this.Game_PostInitialize);

      if (!Directory.Exists(HouseRegionsPlugin.DataDirectory))
        Directory.CreateDirectory(HouseRegionsPlugin.DataDirectory);
      
      if (!this.InitConfig())
        return;

      this.HousingManager = new HousingManager(this.Trace, this.Config);
      this.InitUserInteractionHandler();

      this.hooksEnabled = true;
    }

    private bool InitConfig() {
      if (File.Exists(HouseRegionsPlugin.ConfigFilePath)) {
        try {
          this.Config = Configuration.Read(HouseRegionsPlugin.ConfigFilePath);
        } catch (Exception ex) {
          this.Trace.WriteLineError(
            "Reading the configuration file failed. This plugin will be disabled. Exception details:\n{0}", ex
          );

          this.Dispose();
          return false;
        }
      } else {
        this.Config = new Configuration();
      }

      return true;
    }

    private void InitUserInteractionHandler() {
      Func<Configuration> reloadConfiguration = () => {
        if (this.isDisposed)
          return null;

        this.Config = Configuration.Read(HouseRegionsPlugin.ConfigFilePath);
        this.HousingManager.Config = this.Config;

        return this.Config;
      };
      this.UserInteractionHandler = new UserInteractionHandler(
        this.Trace, this.PluginInfo, this.Config, this.HousingManager, reloadConfiguration
      );
    }
    #endregion

    #region [Hook Handling]
    private void AddHooks() {
      if (this.GetDataHookHandler != null)
        throw new InvalidOperationException("Hooks already registered.");
      
      this.GetDataHookHandler = new GetDataHookHandler(this, true);
      this.GetDataHookHandler.TileEdit += this.Net_TileEdit;
    }

    private void RemoveHooks() {
      if (this.GetDataHookHandler != null) 
        this.GetDataHookHandler.Dispose();

      ServerApi.Hooks.GamePostInitialize.Deregister(this, this.Game_PostInitialize);
    }

    private void Net_TileEdit(object sender, TileEditEventArgs e) {
      if (this.isDisposed || !this.hooksEnabled || e.Handled)
        return;

      e.Handled = this.UserInteractionHandler.HandleTileEdit(e.Player, e.EditType, e.BlockType, e.Location, e.ObjectStyle);
    }
    #endregion

    #region [TerrariaPlugin Overrides]
    public override string Name {
      get { return this.PluginInfo.PluginName; }
    }

    public override Version Version {
      get { return this.PluginInfo.VersionNumber; }
    }

    public override string Author {
      get { return this.PluginInfo.Author; }
    }

    public override string Description {
      get { return this.PluginInfo.Description; }
    }
    #endregion

    #region [IDisposable Implementation]
    private bool isDisposed;

    public bool IsDisposed {
      get { return this.isDisposed; } 
    }

    protected override void Dispose(bool isDisposing) {
      if (this.IsDisposed)
        return;
    
      if (isDisposing) {
        if (this.GetDataHookHandler != null)
          this.GetDataHookHandler.Dispose();
        if (this.UserInteractionHandler != null)
          this.UserInteractionHandler.Dispose();

        this.RemoveHooks();
      }

      base.Dispose(isDisposing);
      this.isDisposed = true;
    }
    #endregion
  }
}