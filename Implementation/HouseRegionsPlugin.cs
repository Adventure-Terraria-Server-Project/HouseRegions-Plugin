using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;

using Terraria.Plugins.Common;
using Terraria.Plugins.Common.Hooks;

using Hooks;
using TShockAPI;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  [APIVersion(1, 12)]
  public class HouseRegionsPlugin: TerrariaPlugin {
    #region [Constants]
    private const string TracePrefix = @"[Housing] ";

    public const string Define_Permission          = "houseregions_define";
    public const string Delete_Permission          = "houseregions_delete";
    public const string Share_Permission           = "houseregions_share";
    public const string ShareWithGroups_Permission = "houseregions_sharewithgroups";
    public const string NoLimits_Permission        = "houseregions_nolimits";
    public const string HousingMaster_Permission   = "houseregions_housingmaster";
    public const string Cfg_Permission             = "houseregions_cfg";
    #endregion

    #region [Properties: Static DataDirectory, Static ConfigFilePath]
    public static string DataDirectory {
      get {
        return Path.Combine(TShock.SavePath, "House Regions");
      }
    }

    public static string ConfigFilePath {
      get {
        return Path.Combine(HouseRegionsPlugin.DataDirectory, "Config.xml");
      }
    }
    #endregion

    #region [Property: Static LatestInstance]
    private static HouseRegionsPlugin latestInstance;

    public static HouseRegionsPlugin LatestInstance {
      get { return HouseRegionsPlugin.latestInstance; }
    }
    #endregion

    #region [Property: Trace]
    private PluginTrace trace;

    public PluginTrace Trace {
      get { return this.trace; }
    }
    #endregion

    #region [Property: PluginInfo]
    private readonly PluginInfo pluginInfo;

    protected PluginInfo PluginInfo {
      get { return this.pluginInfo; }
    }
    #endregion

    #region [Property: Config]
    private Configuration config;

    protected Configuration Config {
      get { return this.config; }
    }
    #endregion

    #region [Property: GetDataHookHandler]
    private GetDataHookHandler getDataHookHandler;

    protected GetDataHookHandler GetDataHookHandler {
      get { return this.getDataHookHandler; }
    }
    #endregion

    #region [Property: HousingManager]
    private HousingManager housingManager;

    public HousingManager HousingManager {
      get { return this.housingManager; }
    }
    #endregion

    #region [Property: UserInteractionHandler]
    private UserInteractionHandler userInteractionHandler;

    protected UserInteractionHandler UserInteractionHandler {
      get { return this.userInteractionHandler; }
    }
    #endregion

    private bool hooksEnabled;


    #region [Method: Constructor]
    public HouseRegionsPlugin(Main game): base(game) {
      this.pluginInfo = new PluginInfo(
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

      this.trace = new PluginTrace(HouseRegionsPlugin.TracePrefix);
      HouseRegionsPlugin.latestInstance = this;
    }
    #endregion

    #region [Methods: Initialize, Game_PostInitialize]
    public override void Initialize() {
      GameHooks.PostInitialize += this.Game_PostInitialize;

      this.AddHooks();
    }

    private void Game_PostInitialize() {
      GameHooks.PostInitialize -= this.Game_PostInitialize;

      if (!Directory.Exists(HouseRegionsPlugin.DataDirectory))
        Directory.CreateDirectory(HouseRegionsPlugin.DataDirectory);
      
      if (!this.InitConfig())
        return;

      this.housingManager = new HousingManager(this.Trace, this.Config);
      this.InitUserInteractionHandler();

      this.hooksEnabled = true;
    }

    private bool InitConfig() {
      if (File.Exists(HouseRegionsPlugin.ConfigFilePath)) {
        try {
          this.config = Configuration.Read(HouseRegionsPlugin.ConfigFilePath);
        } catch (Exception ex) {
          this.Trace.WriteLineError(
            "Reading the configuration file failed. This plugin will be disabled. Exception details:\n{0}", ex
          );

          this.Dispose();
          return false;
        }
      } else {
        this.config = new Configuration();
      }

      return true;
    }

    private void InitUserInteractionHandler() {
      Func<Configuration> reloadConfiguration = () => {
        if (this.isDisposed)
          return null;

        this.config = Configuration.Read(HouseRegionsPlugin.ConfigFilePath);
        this.housingManager.Config = this.Config;

        return this.config;
      };
      this.userInteractionHandler = new UserInteractionHandler(
        this.Trace, this.PluginInfo, this.Config, this.HousingManager, reloadConfiguration
      );
    }
    #endregion

    #region [Methods: Server Hook Handling]
    private void AddHooks() {
      if (this.getDataHookHandler != null)
        throw new InvalidOperationException("Hooks already registered.");
      
      this.getDataHookHandler = new GetDataHookHandler(this.Trace, true);
      this.GetDataHookHandler.TileEdit += this.Net_TileEdit;
    }

    private void RemoveHooks() {
      if (this.getDataHookHandler != null) 
        this.getDataHookHandler.Dispose();

      GameHooks.PostInitialize -= this.Game_PostInitialize;
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
        if (this.getDataHookHandler != null)
          this.getDataHookHandler.Dispose();
        if (this.userInteractionHandler != null)
          this.userInteractionHandler.Dispose();
      }

      base.Dispose(isDisposing);
      this.isDisposed = true;
    }
    #endregion
  }
}