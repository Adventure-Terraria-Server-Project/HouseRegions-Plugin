using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.Plugins.Common;

using TShockAPI;
using TShockAPI.DB;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  public class HousingManager {
    private const string HouseRegionNameAppendix = "*H_";
    private const char HouseRegionNameNumberSeparator = ':';

    private Configuration config;

    public PluginTrace Trace { get; private set; }

    public Configuration Config {
      get { return this.config; }
      set {
        Contract.Requires<ArgumentNullException>(value != null);
        this.config = value;
      }
    }


    public HousingManager(PluginTrace trace, Configuration config) {
      Contract.Requires<ArgumentNullException>(trace != null);
      Contract.Requires<ArgumentNullException>(config != null);

      this.Trace = trace;
      this.config = config;
    }

    public void CreateHouseRegion(TSPlayer player, Rectangle area, bool checkOverlaps = true, bool checkPermissions = false, bool checkDefinePermission = false) {
      Contract.Requires<ArgumentNullException>(player != null);
      Contract.Requires<PlayerNotLoggedInException>(player.IsLoggedIn);

      this.CreateHouseRegion(player.User, player.Group, area, checkOverlaps, checkPermissions, checkDefinePermission);
    }

    public void CreateHouseRegion(User user, Group group, Rectangle area, bool checkOverlaps = true, bool checkPermissions = false, bool checkDefinePermission = false) {
      Contract.Requires<ArgumentNullException>(user != null);
      Contract.Requires<ArgumentNullException>(group != null);
      Contract.Requires<ArgumentException>(area.Width > 0 && area.Height > 0);

      int maxHouses = int.MaxValue;
      if (checkPermissions) {
        if (!group.HasPermission(HouseRegionsPlugin.Define_Permission))
          throw new MissingPermissionException(HouseRegionsPlugin.Define_Permission);
        
        if (!group.HasPermission(HouseRegionsPlugin.NoLimits_Permission)) {
          if (this.Config.MaxHousesPerUser > 0)
            maxHouses = this.Config.MaxHousesPerUser;

          Configuration.HouseSizeConfig restrictingSizeConfig;
          if (!this.CheckHouseRegionValidSize(area, out restrictingSizeConfig))
            throw new InvalidHouseSizeException(restrictingSizeConfig);
        }
      }

      if (checkOverlaps && this.CheckHouseRegionOverlap(user.Name, area))
        throw new HouseOverlapException();

      // Find a free house index.
      int houseIndex;
      string houseName = null;
      for (houseIndex = 1; houseIndex <= maxHouses; houseIndex++) {
        houseName = this.ToHouseRegionName(user.Name, houseIndex);
        if (TShock.Regions.GetRegionByName(houseName) == null)
          break;
      }
      if (houseIndex == maxHouses)
        throw new LimitEnforcementException("Max amount of houses reached.");

      if (!TShock.Regions.AddRegion(
        area.X, area.Y, area.Width, area.Height, houseName, user.Name, Main.worldID.ToString(), 
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

    public void SetHouseRegionOwner(Region region, string newOwnerName) {
      Contract.Requires<ArgumentNullException>(region != null);
      Contract.Requires<ArgumentNullException>(newOwnerName != null);

      string currentOwner;
      int index;
      if (!this.TryGetHouseRegionData(region.Name, out currentOwner, out index))
        throw new ArgumentException("The given region is not a house region.");

      if (currentOwner == newOwnerName)
        return;

      string newRegionName = this.ToHouseRegionName(newOwnerName, index);
      TShock.DB.Query("UPDATE Regions SET RegionName=@0,Owner=@1 WHERE RegionName=@2 AND WorldID=@3", newRegionName, newOwnerName, region.Name, Main.worldID.ToString());
      region.Name = newRegionName;
      region.Owner = newOwnerName;
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

      problematicConfig = this.Config.MaxSize;
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
  }
}