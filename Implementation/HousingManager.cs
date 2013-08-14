using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

using Terraria.Plugins.Common;

using TShockAPI;
using TShockAPI.DB;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  public class HousingManager {
    #region [Constants]
    private const string HouseRegionNameAppendix = "*H_";
    private const char HouseRegionNameNumberSeparator = ':';
    #endregion

    #region [Property: Trace]
    private readonly PluginTrace trace;

    public PluginTrace Trace {
      get { return this.trace; }
    }
    #endregion

    #region [Property: Config]
    private Configuration config;

    public Configuration Config {
      get { return this.config; }
      set {
        Contract.Requires<ArgumentNullException>(value != null);
        this.config = value;
      }
    }
    #endregion


    #region [Method: Constructor]
    public HousingManager(PluginTrace trace, Configuration config) {
      Contract.Requires<ArgumentNullException>(trace != null);
      Contract.Requires<ArgumentNullException>(config != null);

      this.trace = trace;
      this.config = config;
    }
    #endregion

    #region [Methods: CreateHouseRegion, ToHouseRegionName]
    public void CreateHouseRegion(TSPlayer player, Rectangle area, bool checkOverlaps = true, bool checkPermissions = false) {
      Contract.Requires<ArgumentNullException>(player != null);
      Contract.Requires<PlayerNotLoggedInException>(player.IsLoggedIn);
      Contract.Requires<ArgumentException>(area.Width > 0 && area.Height > 0);

      int maxHouses = int.MaxValue;
      if (checkPermissions) {
        if (!player.Group.HasPermission(HouseRegionsPlugin.Define_Permission))
          throw new MissingPermissionException(HouseRegionsPlugin.Define_Permission);
        
        if (!player.Group.HasPermission(HouseRegionsPlugin.NoLimits_Permission)) {
          if (this.Config.MaxHousesPerUser > 0)
            maxHouses = this.Config.MaxHousesPerUser;

          Configuration.HouseSizeConfig restrictingSizeConfig;
          if (!this.CheckHouseRegionValidSize(area, out restrictingSizeConfig))
            throw new InvalidHouseSizeException(restrictingSizeConfig);
        }
      }

      if (checkOverlaps && this.CheckHouseRegionOverlap(player.UserAccountName, area))
        throw new HouseOverlapException();

      // Find a free house index.
      int houseIndex;
      string houseName = null;
      for (houseIndex = 1; houseIndex <= maxHouses; houseIndex++) {
        houseName = this.ToHouseRegionName(player.UserAccountName, houseIndex);
        if (TShock.Regions.GetRegionByName(houseName) == null)
          break;
      }
      if (houseIndex == maxHouses)
        throw new LimitEnforcementException("Max amount of houses reached.");

      if (!TShock.Regions.AddRegion(
        area.X, area.Y, area.Width, area.Height, houseName, player.UserAccountName, Main.worldID.ToString(), 
        this.Config.DefaultZIndex
      ))
        throw new InvalidOperationException();
    }

    public string ToHouseRegionName(string owner, int houseIndex) {
      Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(owner));
      Contract.Requires<ArgumentOutOfRangeException>(houseIndex > 0);

      return string.Concat(
        HousingManager.HouseRegionNameAppendix, owner, HousingManager.HouseRegionNameNumberSeparator, houseIndex
      );
    }
    #endregion

    #region [Methods: TryGetHouseRegionAtPlayer, TryGetHouseRegionData, IsHouseRegion, CheckHouseRegionOverlap, CheckHouseRegionValidSize]
    public bool TryGetHouseRegionAtPlayer(TSPlayer player, out string owner, out int houseIndex, out Region region) {
      Contract.Requires<ArgumentNullException>(player != null);

      for (int i = 0; i < TShock.Regions.Regions.Count; i++) {
        region = TShock.Regions.Regions[i];
        if (region.InArea(player.TileX, player.TileY) && this.TryGetHouseRegionData(region.Name, out owner, out houseIndex))
          return true;
      }

      owner = null;
      region = null;
      houseIndex = -1;
      return false;
    }

    public bool TryGetHouseRegionData(string regionName, out string owner, out int houseIndex) {
      Contract.Requires<ArgumentNullException>(regionName != null);

      owner = null;
      houseIndex = -1;

      if (!regionName.StartsWith(HousingManager.HouseRegionNameAppendix))
        return false;

      int separatorIndex = regionName.LastIndexOf(HousingManager.HouseRegionNameNumberSeparator);
      if (
        separatorIndex == -1 || separatorIndex == regionName.Length - 1 || 
        separatorIndex <= HousingManager.HouseRegionNameAppendix.Length
      )
        return false;

      string houseIndexRaw = regionName.Substring(separatorIndex + 1);
      if (!int.TryParse(houseIndexRaw, out houseIndex))
        return false;

      owner = regionName.Substring(HousingManager.HouseRegionNameAppendix.Length, separatorIndex - HousingManager.HouseRegionNameAppendix.Length);
      return true;
    }

    public bool IsHouseRegion(string regionName) {
      string dummy;
      int dummy2;
      return this.TryGetHouseRegionData(regionName, out dummy, out dummy2);
    }

    public bool CheckHouseRegionOverlap(string owner, Rectangle regionArea) {
      for (int i = 0; i < TShock.Regions.Regions.Count; i++) {
        Region tsRegion = TShock.Regions.Regions[i];
        if (
          regionArea.Right < tsRegion.Area.Left || regionArea.X > tsRegion.Area.Right ||
          regionArea.Bottom < tsRegion.Area.Top || regionArea.Y > tsRegion.Area.Bottom
        )
          continue;

        string houseOwner;
        int houseIndex;
        if (!this.TryGetHouseRegionData(tsRegion.Name, out houseOwner, out houseIndex)) {
          if (this.Config.AllowTShockRegionOverlapping || tsRegion.Name.StartsWith("*"))
            continue;

          return true;
        }
        if (houseOwner == owner)
          continue;

        return true;
      }

      return false;
    }

    public bool CheckHouseRegionValidSize(Rectangle regionArea, out Configuration.HouseSizeConfig problematicConfig) {
      int areaTotalTiles = regionArea.Width * regionArea.Height;

      problematicConfig = this.Config.MinSize;
      if (
        regionArea.Width < this.Config.MinSize.Width || regionArea.Height < this.Config.MinSize.Height ||
        areaTotalTiles < this.Config.MinSize.TotalTiles
      )
        return false;

      if (
        regionArea.Width > this.Config.MaxSize.Width || regionArea.Height > this.Config.MaxSize.Height ||
        areaTotalTiles > this.Config.MaxSize.TotalTiles
      )
        return false;

      problematicConfig = default(Configuration.HouseSizeConfig);
      return true;
    }

    public bool CheckHouseRegionValidSize(Rectangle regionArea) {
      Configuration.HouseSizeConfig dummy;
      return this.CheckHouseRegionValidSize(regionArea, out dummy);
    }
    #endregion
  }
}