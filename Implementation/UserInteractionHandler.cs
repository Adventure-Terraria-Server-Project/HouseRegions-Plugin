using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using DPoint = System.Drawing.Point;

using Terraria.Plugins.Common;

using TShockAPI;
using TShockAPI.DB;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  public class UserInteractionHandler: UserInteractionHandlerBase, IDisposable {
    #region [Nested: CommandExecDummyPlayer]
    private class CommandExecDummyPlayer: TSPlayer {
      private readonly Action<string,Color> sendMessageHandler;

      public CommandExecDummyPlayer(
        TSPlayer originalPlayer, Action<string,Color> sendMessageHandler
      ): base(originalPlayer.Name) {
        this.User.ID = originalPlayer.User.ID;
        this.User.Name = originalPlayer.User.Name;
        this.IsLoggedIn = originalPlayer.IsLoggedIn;
        this.Group = originalPlayer.Group;

        this.sendMessageHandler = sendMessageHandler;
      }

      public override void SendMessage(string msg, Color color) {
        this.sendMessageHandler?.Invoke(msg, color);
      }
    }
    #endregion

    protected PluginInfo PluginInfo { get; private set; }
    protected Configuration Config { get; private set; }
    protected HousingManager HousingManager { get; private set; }
    protected Func<Configuration> ReloadConfigurationCallback { get; private set; }


    public UserInteractionHandler(
      PluginTrace trace, PluginInfo pluginInfo, Configuration config, HousingManager housingManager,
      Func<Configuration> reloadConfigurationCallback
    ): base(trace) {
      Contract.Requires<ArgumentNullException>(trace != null);
      Contract.Requires<ArgumentException>(!pluginInfo.Equals(PluginInfo.Empty));
      Contract.Requires<ArgumentNullException>(config != null);
      Contract.Requires<ArgumentNullException>(housingManager != null);
      Contract.Requires<ArgumentNullException>(reloadConfigurationCallback != null);

      this.PluginInfo = pluginInfo;
      this.Config = config;
      this.HousingManager = housingManager;
      this.ReloadConfigurationCallback = reloadConfigurationCallback;

      #region Command Setup
      base.RegisterCommand(
        new[] { "house", "housing" }, this.RootCommand_Exec, this.RootCommand_HelpCallback
      );
      #endregion
    }

    #region [Command Handling /house]
    private void RootCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;
      
      base.StopInteraction(args.Player);

      if (args.Parameters.Count >= 1) {
        string subCommand = args.Parameters[0].ToLowerInvariant();

        if (this.TryExecuteSubCommand(subCommand, args))
          return;
      }

      args.Player.SendMessage(this.PluginInfo.ToString(), Color.White);
      args.Player.SendMessage(this.PluginInfo.Description, Color.White);
      args.Player.SendMessage(string.Empty, Color.Yellow);

      int playerHouseCount = 0;
      for (int i = 0; i < TShock.Regions.Regions.Count; i++) {
        string houseOwner;
        int houseIndex;
        if (
          this.HousingManager.TryGetHouseRegionData(TShock.Regions.Regions[i].Name, out houseOwner, out houseIndex) &&
          houseOwner == args.Player.User.Name
        )
          playerHouseCount++;
      }

      string statsMessage = string.Format(
        "You've defined {0} of {1} possible houses so far.", playerHouseCount, this.Config.MaxHousesPerUser
      );
      args.Player.SendMessage(statsMessage, Color.Yellow);
      args.Player.SendMessage("Type \"/house commands\" to get a list of available commands.", Color.Yellow);
      args.Player.SendMessage("To get more general information about this plugin type \"/house help\".", Color.Yellow);
    }

    private bool TryExecuteSubCommand(string commandNameLC, CommandArgs args) {
      switch (commandNameLC) {
        case "commands":
        case "cmds": {
          int pageNumber;
          if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
            return true;

          List<string> terms = new List<string>();
          terms.Add("/house info");
          terms.Add("/house scan");
          if (args.Player.Group.HasPermission(HouseRegionsPlugin.HousingMaster_Permission))
            terms.Add("/house summary");
          if (args.Player.Group.HasPermission(HouseRegionsPlugin.Define_Permission)) {
            terms.Add("/house define");
            terms.Add("/house resize");
          }
          if (args.Player.Group.HasPermission(HouseRegionsPlugin.Delete_Permission))
            terms.Add("/house delete");
          if (args.Player.Group.HasPermission(HouseRegionsPlugin.Share_Permission)) {
            terms.Add("/house setowner");
            terms.Add("/house share");
            terms.Add("/house unshare");
          }
          if (args.Player.Group.HasPermission(HouseRegionsPlugin.ShareWithGroups_Permission)) {
            terms.Add("/house sharegroup");
            terms.Add("/house unsharegroup");
          }
          if (args.Player.Group.HasPermission(HouseRegionsPlugin.Cfg_Permission))
            terms.Add("/house reloadconfig");
          
          List<string> lines = PaginationUtil.BuildLinesFromTerms(terms);
          PaginationUtil.SendPage(args.Player, pageNumber, lines, new PaginationUtil.Settings {
            HeaderFormat = "House Commands (Page {0} of {1})",
            LineTextColor = Color.LightGray,
          });

          return true;
        }
        case "summary":
          this.HouseSummaryCommand_Exec(args);
          return true;
        case "info":
          this.HouseInfoCommand_Exec(args);
          return true;
        case "scan":
          this.HouseScanCommand_Exec(args);
          return true;
        case "define":
        case "def":
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.Define_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          this.HouseDefineCommand_Exec(args);
          return true;
        case "resize":
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.Define_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          this.HouseResizeCommand_Exec(args);
          return true;
        case "delete":
        case "del":
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.Delete_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          this.HouseDeleteCommand_Exec(args);
          return true;
        case "setowner":
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.Share_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          this.HouseSetOwnerCommand_Exec(args);
          return true;
        case "shareuser":
        case "share":
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.Share_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          this.HouseShareCommand_Exec(args);
          return true;
        case "unshareuser":
        case "unshare":
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.Share_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          this.HouseUnshareCommand_Exec(args);
          return true;
        case "sharegroup":
        case "shareg":
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.ShareWithGroups_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          this.HouseShareGroupCommand_Exec(args);
          return true;
        case "unsharegroup":
        case "unshareg":
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.ShareWithGroups_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          this.HouseUnshareGroupCommand_Exec(args);
          return true;
        case "reloadconfiguration":
        case "reloadconfig":
        case "reloadcfg": {
          if (!args.Player.Group.HasPermission(HouseRegionsPlugin.Cfg_Permission)) {
            args.Player.SendErrorMessage("You do not have the necessary permission to do that.");
            return true;
          }

          if (args.Parameters.Count == 2 && args.Parameters[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
            args.Player.SendMessage("Command reference for /house reloadconfiguration (Page 1 of 1)", Color.Lime);
            args.Player.SendMessage("/house reloadconfiguration|reloadconfig|reloadcfg", Color.White);
            args.Player.SendMessage("Reloads House Region's configuration file and applies all new settings.", Color.LightGray);
            return true;
          }

          this.PluginTrace.WriteLineInfo("Reloading configuration file.");
          try {
            this.Config = this.ReloadConfigurationCallback();
            this.PluginTrace.WriteLineInfo("Configuration file successfully reloaded.");

            if (args.Player != TSPlayer.Server)
              args.Player.SendSuccessMessage("Configuration file successfully reloaded.");
          } catch (Exception ex) {
            this.PluginTrace.WriteLineError(
              "Reloading the configuration file failed. Keeping old configuration. Exception details:\n{0}", ex
            );
          }

          return true;
        }
      }

      return false;
    }

    private bool RootCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return true;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 1, null, out pageNumber))
        return false;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("House Regions Overview (Page 1 of 2)", Color.Lime);
          args.Player.SendMessage("This plugin provides players on TShock driven Terraria servers the possibility", Color.LightGray);
          args.Player.SendMessage("of defining houses in which other players can not alter any tiles.", Color.LightGray);
          args.Player.SendMessage("For more information about defining new houses write /house define help", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.LightGray);
          args.Player.SendMessage("You may also want to allow other players to change the tiles in your house,", Color.LightGray);
          break;
        case 2:
          args.Player.SendMessage("to do that, you can either add specific users or whole groups of users to your", Color.LightGray);
          args.Player.SendMessage("house. To get more information on how to sharing a house type /house share help or", Color.LightGray);
          args.Player.SendMessage("/house sharegroup help.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.LightGray);
          args.Player.SendMessage("To check for existing houses or to get general information about existing houses", Color.LightGray);
          args.Player.SendMessage("use the /house info command.", Color.LightGray);
          break;
      }

      return true;
    }
    #endregion

    #region [Command Handling /house summary]
    private void HouseSummaryCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber = 1;
      if (args.Parameters.Count > 2) {
        if (args.Parameters[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
          this.HouseSummaryCommand_HelpCallback(args);
          return;
        }
        
        args.Player.SendErrorMessage("Proper syntax: /house summary [page]");
        args.Player.SendInfoMessage("Type /house summary help to get more information about this command.");
        return;
      }

      var ownerHouses = new Dictionary<string,int>(TShock.Regions.Regions.Count);
      for (int i = 0; i < TShock.Regions.Regions.Count; i++) {
        Region tsRegion = TShock.Regions.Regions[i];
        string owner;
        int dummy;
        if (!this.HousingManager.TryGetHouseRegionData(tsRegion.Name, out owner, out dummy))
          continue;

        int houseCount;
        if (!ownerHouses.TryGetValue(owner, out houseCount))
          ownerHouses.Add(owner, 1);
        else
          ownerHouses[owner] = houseCount + 1;
      }

      IEnumerable<string> ownerHousesTermSelector = ownerHouses.Select(
        pair => string.Concat(pair.Key, " (", pair.Value, ")")
      );

      PaginationTools.SendPage(
        args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(ownerHousesTermSelector), new PaginationTools.Settings {
          HeaderFormat = string.Format("House Owners ({{0}}/{{1}}):"),
          FooterFormat = string.Format("Type /house summary {{0}} for more."),
          NothingToDisplayString = "There are no house regions in this world."
        }
      );
    }

    private void HouseSummaryCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house summary (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house summary [page]", Color.White);
          args.Player.SendMessage("Displays all house owners and the amount of house regions they own.", Color.LightGray);
          return;
      }
    }
    #endregion

    #region [Command Handling /house info]
    private void HouseInfoCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber = 1;
      if (args.Parameters.Count > 2) {
        if (args.Parameters[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
          this.HouseInfoCommand_HelpCallback(args);
          return;
        }
        
        args.Player.SendErrorMessage("Proper syntax: /house info [page]");
        args.Player.SendInfoMessage("Type /house info help to get more information about this command.");
        return;
      }

      string owner;
      Region region;
      if (!this.TryGetHouseRegionAtPlayer(args.Player, out owner, out region))
        return;

      List<string> lines = new List<string> {
        "Owned by: " + owner
      };

      if (region.AllowedIDs.Count > 0) {
        IEnumerable<string> sharedUsersSelector = region.AllowedIDs.Select(userId => {
          User user = TShock.Users.GetUserByID(userId);
          if (user != null)
            return user.Name;
          else
            return string.Concat("{ID: ", userId, "}");
        });

        List<string> extraLines = PaginationTools.BuildLinesFromTerms(sharedUsersSelector.Distinct());
        extraLines[0] = "Shared with: " + extraLines[0];
        lines.AddRange(extraLines);
      } else {
        lines.Add("House is not shared with any users.");
      }

      if (region.AllowedGroups.Count > 0) {
        List<string> extraLines = PaginationTools.BuildLinesFromTerms(region.AllowedGroups.Distinct());
        extraLines[0] = "Shared with groups: " + extraLines[0];
        lines.AddRange(extraLines);
      } else {
        lines.Add("House is not shared with any groups.");
      }

      PaginationTools.SendPage(
        args.Player, pageNumber, lines, new PaginationTools.Settings {
          HeaderFormat = string.Format("Information About This House ({{0}}/{{1}}):"),
          FooterFormat = string.Format("Type /house info {{0}} for more information.")
        }
      );

      this.SendAreaDottedFakeWiresTimed(args.Player, region.Area, 5000);
    }

    private void HouseInfoCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house info (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house info [page]", Color.White);
          args.Player.SendMessage("Displays several information about the house at your current positon.", Color.LightGray);
          args.Player.SendMessage("Will also display the boundaries of the house by wires.", Color.LightGray);
          return;
      }
    }
    #endregion

    #region [Command Handling /house scan]
    private void HouseScanCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      if (args.Parameters.Count > 1) {
        if (args.Parameters[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
          this.HouseScanCommand_HelpCallback(args);
          return;
        }
        
        args.Player.SendErrorMessage("Proper syntax: /house scan");
        args.Player.SendInfoMessage("Type /house scan help to get more information about this command.");
        return;
      }

      Point playerLocation = new Point(args.Player.TileX, args.Player.TileY);
      List<Rectangle> houseAreasToDisplay = new List<Rectangle>(
        from r in TShock.Regions.Regions
        where Math.Sqrt(Math.Pow(playerLocation.X - r.Area.Center.X, 2) + Math.Pow(playerLocation.Y - r.Area.Center.Y, 2)) <= 200
        select r.Area
      );
      if (houseAreasToDisplay.Count == 0) {
        args.Player.SendSuccessMessage("There are no nearby house regions.");
        return;
      }

      foreach (Rectangle regionArea in houseAreasToDisplay)
        this.SendAreaDottedFakeWires(args.Player, regionArea);
      args.Player.SendInfoMessage("Hold a wire or wire tool to see all nearby house regions.");

      System.Threading.Timer hideTimer = null;
      hideTimer = new System.Threading.Timer(state => {
          foreach (Rectangle regionArea in houseAreasToDisplay)
            this.SendAreaDottedFakeWires(args.Player, regionArea, false);

          // ReSharper disable AccessToModifiedClosure
          Debug.Assert(hideTimer != null);
          hideTimer.Dispose();
          // ReSharper restore AccessToModifiedClosure
        },
        null, 10000, Timeout.Infinite
      );
    }

    private void HouseScanCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house scan (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house scan", Color.White);
          args.Player.SendMessage("Displays all house region boundaries close to your character's position", Color.LightGray);
          args.Player.SendMessage("as wires.", Color.LightGray);
          return;
      }
    }
    #endregion

    #region [Command Handling /house define]
    private void HouseDefineCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      if (args.Parameters.Count > 1) {
        if (args.Parameters[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
          this.HouseDefineCommand_HelpCallback(args);
          return;
        }

        args.Player.SendErrorMessage("Proper syntax: /house define");
        args.Player.SendInfoMessage("Type /house define help to get more help to this command.");
        return;
      }

      if (!args.Player.IsLoggedIn) {
        args.Player.SendErrorMessage("You have to be logged in in order to define houses.");
        return;
      }

      DPoint point1 = DPoint.Empty;
      DPoint point2 = DPoint.Empty;
      Rectangle houseArea = Rectangle.Empty;
      args.Player.SendMessage("First Mark", Color.IndianRed);
      args.Player.SendMessage("Mark the top left tile of your house by placing a wire using wrench", Color.MediumSpringGreen);
      args.Player.SendMessage("or by altering the tile otherwise.", Color.MediumSpringGreen);

      CommandInteraction interaction = this.StartOrResetCommandInteraction(args.Player, 60000);
      interaction.TileEditCallback += (playerLocal, editType, tileId, tileLocation, objectStyle) => {
        // Revoke Mark 1 or 2
        if (editType == TileEditType.DestroyWire || editType == TileEditType.DestroyWireBlue || editType == TileEditType.DestroyWireGreen || editType == TileEditType.DestroyWireYellow) {
          if (tileLocation == point1) {
            point1 = DPoint.Empty;

            if (houseArea != Rectangle.Empty)
              this.SendAreaDottedFakeWires(playerLocal, houseArea, false);

            playerLocal.SendTileSquare(tileLocation);

            if (point2 != DPoint.Empty)
              this.SendFakeWireCross(playerLocal, point2);

            args.Player.SendMessage("First Mark", Color.IndianRed);
            args.Player.SendMessage("Mark the top left tile of your house by placing a wire using wrench", Color.MediumSpringGreen);
            args.Player.SendMessage("or by altering the tile otherwise.", Color.MediumSpringGreen);
            args.Player.SendMessage(string.Empty, Color.MediumSpringGreen);
            interaction.ResetTimer();
          } else if (tileLocation == point2) {
            point2 = DPoint.Empty;

            if (houseArea != Rectangle.Empty)
              this.SendAreaDottedFakeWires(playerLocal, houseArea, false);

            playerLocal.SendTileSquare(tileLocation);

            if (point1 != DPoint.Empty)
              this.SendFakeWireCross(playerLocal, point1);

            args.Player.SendMessage("Second Mark", Color.IndianRed);
            args.Player.SendMessage("Mark the bottom right tile of your house by placing a wire using wrench", Color.MediumSpringGreen);
            args.Player.SendMessage("or by altering the tile otherwise.", Color.MediumSpringGreen);
            args.Player.SendMessage(string.Empty, Color.MediumSpringGreen);
            interaction.ResetTimer();
          }
          return new CommandInteractionResult { IsHandled = true, IsInteractionCompleted = false };
        }

        // Mark 1 / 2
        if (point1 == DPoint.Empty || point2 == DPoint.Empty) {
          if (point1 == DPoint.Empty)
            point1 = tileLocation;
          else
            point2 = tileLocation;

          playerLocal.SendTileSquare(tileLocation);
          this.SendFakeWireCross(playerLocal, tileLocation);

          if (point1 != DPoint.Empty && point2 != DPoint.Empty) {
            houseArea = new Rectangle(
              Math.Min(point1.X, point2.X), Math.Min(point1.Y, point2.Y),
              Math.Abs(point1.X - point2.X), Math.Abs(point1.Y - point2.Y)
            );
            this.SendAreaDottedFakeWires(playerLocal, houseArea);

            args.Player.SendMessage("Final Mark", Color.IndianRed);
            args.Player.SendMessage("Mark any point inside your house to accept, or any point outside the house to cancel.", Color.MediumSpringGreen);
            args.Player.SendMessage(string.Empty, Color.MediumSpringGreen);
          } else {
            if (point2 == DPoint.Empty) {
              args.Player.SendMessage("Second Mark", Color.IndianRed);
              args.Player.SendMessage("Mark the bottom right tile of your house by placing a wire using wrench", Color.MediumSpringGreen);
              args.Player.SendMessage("or by altering the tile otherwise.", Color.MediumSpringGreen);
              args.Player.SendMessage(string.Empty, Color.MediumSpringGreen);
            } else {
              args.Player.SendMessage("First Mark", Color.IndianRed);
              args.Player.SendMessage("Mark the bottom right tile of your house by placing a wire using wrench", Color.MediumSpringGreen);
              args.Player.SendMessage("or by altering the tile otherwise.", Color.MediumSpringGreen);
              args.Player.SendMessage(string.Empty, Color.MediumSpringGreen);
            }
          }

          if (editType == TileEditType.PlaceWire || editType == TileEditType.PlaceWireBlue || editType == TileEditType.PlaceWireGreen || editType == TileEditType.PlaceWireYellow)
            TerrariaUtils.Items.CreateNew(playerLocal, playerLocal.ToLocation(), new ItemData(ItemType.Wire));
          interaction.ResetTimer();

          return new CommandInteractionResult { IsHandled = true, IsInteractionCompleted = false };
        } else {
          // Final Mark
          playerLocal.SendTileSquare(point1);
          playerLocal.SendTileSquare(point2);
          this.SendAreaDottedFakeWires(playerLocal, houseArea, false);
          playerLocal.SendTileSquare(tileLocation);

          if (editType == TileEditType.PlaceWire || editType == TileEditType.PlaceWireBlue || editType == TileEditType.PlaceWireGreen || editType == TileEditType.PlaceWireYellow)
            TerrariaUtils.Items.CreateNew(playerLocal, playerLocal.ToLocation(), new ItemData(ItemType.Wire));

          if (
            tileLocation.X >= houseArea.Left && tileLocation.X <= houseArea.Right && 
            tileLocation.Y >= houseArea.Top && tileLocation.Y <= houseArea.Bottom
          ) {
            try {
              if (houseArea.Width <= 0 || houseArea.Height <= 0) {
                playerLocal.SendErrorMessage("The house has to be at least one block high and wide.");
              } else {
                this.HousingManager.CreateHouseRegion(playerLocal, houseArea, true, true);
                playerLocal.SendMessage("House was successfully created. Other players can no longer change blocks", Color.MediumSpringGreen);
                playerLocal.SendMessage("inside the defined house region.", Color.MediumSpringGreen);
              }
            } catch (InvalidHouseSizeException ex) {
              this.ExplainInvalidRegionSize(playerLocal, houseArea, ex.RestrictingConfig);
            } catch (HouseOverlapException) {
              if (this.Config.AllowTShockRegionOverlapping) {
                playerLocal.SendErrorMessage("The house would overlap with another house where you're not the owner of.");
              } else {
                playerLocal.SendErrorMessage("The house would overlap with another house where you're not the owner of or");
                playerLocal.SendErrorMessage("it overlaps with a TShock region.");
              }
            } catch (LimitEnforcementException) {
              playerLocal.SendErrorMessage(
                "You have reached the maximum of {0} houses. Delete at least one of your other houses first.", 
                this.Config.MaxHousesPerUser
              );
            }
          } else {
            playerLocal.SendWarningMessage("Defining of house was aborted.");
          }

          return new CommandInteractionResult { IsHandled = true, IsInteractionCompleted = true };
        }
      };
      interaction.TimeExpiredCallback += (playerLocal) => {
        playerLocal.SendErrorMessage("Waited too long. No house will be defined.");
      };
      interaction.AbortedCallback += (playerLocal) => {
        if (point1 != DPoint.Empty)
          playerLocal.SendTileSquare(point1);
        if (point2 != DPoint.Empty)
          playerLocal.SendTileSquare(point2);
        if (houseArea != Rectangle.Empty)
          this.SendAreaDottedFakeWires(playerLocal, houseArea, false);
      };
    }

    private void HouseDefineCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house define (Page 1 of 2)", Color.Lime);
          args.Player.SendMessage("/house define|def", Color.White);
          args.Player.SendMessage("Switches to house definition mode. You have to set two points in order to", Color.LightGray);
          args.Player.SendMessage("create a new house. Write /house define, then hit the top left corner of your", Color.LightGray);
          args.Player.SendMessage("house, after this hit the bottom right corner, then new house region will be defined.", Color.LightGray);
          args.Player.SendMessage("NOTE: Using wrench to mark the house region corners is recommended, you can also", Color.IndianRed);
          return;
        case 2:
          args.Player.SendMessage("revoke already defined points by using wire cutter while in definition mode, try it!", Color.IndianRed);
          args.Player.SendMessage("Already existing houses can always be resized by using /house resize later.", Color.LightGray);
          return;
      }
    }
    #endregion

    #region [Command Handling /house resize]
    private void HouseResizeCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      Action invalidSyntax = () => {
        args.Player.SendErrorMessage("Proper syntax: /house resize <up|down|left|right>[...] <amount>");
        args.Player.SendInfoMessage("Type /house resize help to get more information about this command.");
      };

      if (args.Parameters.Count >= 2 && args.Parameters[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
        this.HouseResizeCommand_HelpCallback(args);
        return;
      }

      Region region;
      string owner;
      if (!this.TryGetAccessibleHouseRegionAtPlayer(args.Player, out owner, out region))
        return;

      int amount;
      if (args.Parameters.Count < 3 || !int.TryParse(args.Parameters[args.Parameters.Count - 1], out amount)) {
        invalidSyntax();
        return;
      }

      Rectangle newArea = region.Area;
      List<int> directions = new List<int>();
      //0 = up
      //1 = right
      //2 = down
      //3 = left
      for (int i = 1; i < args.Parameters.Count - 1; i++) {
        switch (args.Parameters[i].ToLowerInvariant()) {
          case "up":
          case "u":
            newArea.Y -= amount;
            newArea.Height += amount;
            directions.Add(0);
            break;
          case "down":
          case "d":
            newArea.Height += amount;
            directions.Add(2);
            break;
          case "left":
          case "l":
            newArea.X -= amount;
            newArea.Width += amount;
            directions.Add(3);
            break;
          case "right":
          case "r":
            newArea.Width += amount;
            directions.Add(1);
            break;
        }
      }

      if (newArea.Width < 0)
        newArea.Width = 1;
      if (newArea.Height < 0)
        newArea.Height = 1;
      
      Configuration.HouseSizeConfig restrictingSizeConfig;
      if (!this.HousingManager.CheckHouseRegionValidSize(newArea, out restrictingSizeConfig)) {
        this.ExplainInvalidRegionSize(args.Player, newArea, restrictingSizeConfig);
        return;
      }

      if (this.HousingManager.CheckHouseRegionOverlap(owner, newArea)) {
        if (this.Config.AllowTShockRegionOverlapping) {
          args.Player.SendErrorMessage("The house region would overlap either with another house not owned by you or");
          args.Player.SendErrorMessage("with a TShock region.");
        } else {
          args.Player.SendErrorMessage("The house region would overlap with another house not owned by you.");
        }

        return;
      }

      Rectangle oldArea = region.Area;
      region.Area = newArea;
      foreach (int direction in directions)
      {
          if (!TShock.Regions.ResizeRegion(region.Name, amount, direction))
          {
              args.Player.SendErrorMessage("Internal error has occured.");
              region.Area = oldArea;
              return;
          }
      }

      args.Player.SendSuccessMessage("House was successfully resized.");
      this.SendAreaDottedFakeWires(args.Player, oldArea, false);
      this.SendAreaDottedFakeWiresTimed(args.Player, newArea, 2000);
    }

    private void HouseResizeCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house resize (Page 1 of 3)", Color.Lime);
          args.Player.SendMessage("/house resize <up|down|left|right>[...] <amount>", Color.White);
          args.Player.SendMessage("Resizes the current house to one direction by the given amount.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.IndianRed);
          args.Player.SendMessage("u|d|l|r = The directions to resize to (up, left, down, right).", Color.LightGray);
          break;
        case 2:
          args.Player.SendMessage("amount = The amount of tiles to expand, can also be negative to shrink", Color.LightGray);
          args.Player.SendMessage("         the house region.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.IndianRed);
          args.Player.SendMessage("NOTE: If you hold a wire or wire tool, then you can see the new boundaries", Color.IndianRed);
          args.Player.SendMessage("of the house region after the resize.", Color.IndianRed);
          break;
        case 3:
          args.Player.SendMessage("NOTE: You have to own a house in order to resize it, just having", Color.IndianRed);
          args.Player.SendMessage("build access is not sufficient.", Color.IndianRed);
          return;
      }
    }
    #endregion

    #region [Command Handling /house delete]
    private void HouseDeleteCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      if (args.Parameters.Count > 1) {
        if (args.Parameters[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
          this.HouseDeleteCommand_HelpCallback(args);
          return;
        }

        args.Player.SendErrorMessage("Proper syntax: /house delete");
        args.Player.SendInfoMessage("Type /house delete help to get more information about this command.");
        return;
      }

      Region region;
      if (!this.TryGetAccessibleHouseRegionAtPlayer(args.Player, out region))
        return;

      if (!TShock.Regions.DeleteRegion(region.Name)) {
        args.Player.SendErrorMessage("Internal error has occured.");
        return;
      }

      args.Player.SendSuccessMessage("The house was successfully deleted.");
    }

    private void HouseDeleteCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house delete (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house delete|del", Color.White);
          args.Player.SendMessage("Deletes the house region where your character currently stands in.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.IndianRed);
          args.Player.SendMessage("NOTE: You have to own a house in order to remove it, just having", Color.IndianRed);
          args.Player.SendMessage("build access is not sufficient.", Color.IndianRed);
          return;
      }
    }
    #endregion

    #region [Command Handling /house setowner]
    private void HouseSetOwnerCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      if (args.Parameters.Count < 2) {
        args.Player.SendErrorMessage("Proper syntax: /house setowner <user name>");
        args.Player.SendInfoMessage("Type /house setowner help to get more information about this command.");
        return;
      }

      string newOwnerRaw = args.ParamsToSingleString(1);
      if (newOwnerRaw.Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
        this.HouseSetOwnerCommand_HelpCallback(args);
        return;
      }

      User tsUser;
      if (!TShockEx.MatchUserByPlayerName(newOwnerRaw, out tsUser, args.Player))
        return;

      Region region;
      if (!this.TryGetAccessibleHouseRegionAtPlayer(args.Player, out region))
        return;

      if (tsUser.Name == region.Owner) {
        args.Player.SendErrorMessage($"{tsUser.Name} is already the owner of this region.");
        return;
      }
        
      Group tsGroup = TShock.Groups.GetGroupByName(tsUser.Group);
      if (tsGroup == null) {
        args.Player.SendErrorMessage("The new owner's TShock group could not be determined.");
        return;
      }

      try {
        this.HousingManager.CreateHouseRegion(tsUser, tsGroup, region.Area, false, true, false);
      } catch (LimitEnforcementException) {
        args.Player.SendErrorMessage("The new owner of the house would exceed their house limit.");
        return;
      } catch (Exception ex) {
        args.Player.SendErrorMessage("Internal error has occured: " + ex.Message);
        return;
      }
      
      if (!TShock.Regions.DeleteRegion(region.Name)) {
        args.Player.SendErrorMessage("Internal error has occured when deleting the old house region.");
        return;
      }

      args.Player.SendSuccessMessage($"The owner of this house has been set to \"{tsUser.Name}\" and all shared users and groups were deleted from it.");
    }

    private void HouseSetOwnerCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house setowner (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house setowner <user name>", Color.White);
          args.Player.SendMessage("Changes the owning user of the house at you character.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.IndianRed);
          args.Player.SendMessage("NOTE: You have to own a house in order to change its owner, just having", Color.IndianRed);
          args.Player.SendMessage("build access is not sufficient.", Color.IndianRed);
          return;
      }
    }
    #endregion

    #region [Command Handling /house share]
    private void HouseShareCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      if (args.Parameters.Count < 2) {
        args.Player.SendErrorMessage("Proper syntax: /house share <user name>");
        args.Player.SendInfoMessage("Type /house share help to get more information about this command.");
        return;
      }

      string shareTargetRaw = args.ParamsToSingleString(1);
      if (shareTargetRaw.Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
        this.HouseShareCommand_HelpCallback(args);
        return;
      }

      User tsUser;
      if (!TShockEx.MatchUserByPlayerName(shareTargetRaw, out tsUser, args.Player))
        return;

      Region region;
      if (!this.TryGetAccessibleHouseRegionAtPlayer(args.Player, out region))
        return;

      if (!TShock.Regions.AddNewUser(region.Name, tsUser.Name)) {
        args.Player.SendErrorMessage("Internal error has occured.");
        return;
      }

      args.Player.SendSuccessMessage("User \"{0}\" has build access to this house now.", tsUser.Name);
    }

    private void HouseShareCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house share (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house shareuser|share <user name>", Color.White);
          args.Player.SendMessage("Grants build access to another user for the house at you character.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.IndianRed);
          args.Player.SendMessage("NOTE: You have to own a house in order to share it, just having", Color.IndianRed);
          args.Player.SendMessage("build access is not sufficient.", Color.IndianRed);
          return;
      }
    }
    #endregion

    #region [Command Handling /house unshare]
    private void HouseUnshareCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      if (args.Parameters.Count < 2) {
        args.Player.SendErrorMessage("Proper syntax: /house unshare <user name>");
        args.Player.SendInfoMessage("Type /house unshare help to get more information about this command.");
        return;
      }

      string shareTargetRaw = args.ParamsToSingleString(1);
      if (shareTargetRaw.Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
        this.HouseUnshareCommand_HelpCallback(args);
        return;
      }

      User tsUser;
      if (!TShockEx.MatchUserByPlayerName(shareTargetRaw, out tsUser, args.Player))
        return;

      Region region;
      if (!this.TryGetAccessibleHouseRegionAtPlayer(args.Player, out region))
        return;

      if (!TShock.Regions.RemoveUser(region.Name, tsUser.Name)) {
        args.Player.SendErrorMessage("Internal error has occured.");
        return;
      }

      args.Player.SendSuccessMessage("User \"{0}\" has no more build access to this house anymore.", tsUser.Name);
    }

    private void HouseUnshareCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house share (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house unshareuser|unshare <user name>", Color.White);
          args.Player.SendMessage("Removes build access of another user for the house at you character.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.IndianRed);
          args.Player.SendMessage("NOTE: You have to own a house in order to alter shares of it,", Color.IndianRed);
          args.Player.SendMessage("just having build access is not sufficient.", Color.IndianRed);
          return;
      }
    }
    #endregion

    #region [Command Handling /house sharegroup]
    private void HouseShareGroupCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      if (args.Parameters.Count < 2) {
        args.Player.SendErrorMessage("Proper syntax: /house sharegroup <group name>");
        args.Player.SendInfoMessage("Type /house sharegroup help to get more information about this command.");
        return;
      }

      string shareTargetRaw = args.ParamsToSingleString(1);
      if (shareTargetRaw.Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
        this.HouseShareGroupCommand_HelpCallback(args);
        return;
      }

      Group tsGroup = TShock.Groups.GetGroupByName(shareTargetRaw);
      if (tsGroup == null) {
        args.Player.SendErrorMessage("A group with the name \"{0}\" does not exist.", shareTargetRaw);
        return;
      }

      Region region;
      if (!this.TryGetAccessibleHouseRegionAtPlayer(args.Player, out region))
        return;

      if (!TShock.Regions.AllowGroup(region.Name, tsGroup.Name)) {
        args.Player.SendErrorMessage("Internal error has occured.");
        return;
      }

      args.Player.SendSuccessMessage("All users of group \"{0}\" have build access to this house now.", tsGroup.Name);
    }

    private void HouseShareGroupCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house sharegroup (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house sharegroup|shareg <group name>", Color.White);
          args.Player.SendMessage("Grants build access to all users in a TShock group for the house at you character.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.IndianRed);
          args.Player.SendMessage("NOTE: You have to own a house in order to share it, just having", Color.IndianRed);
          args.Player.SendMessage("build access is not sufficient.", Color.IndianRed);
          return;
      }
    }
    #endregion

    #region [Command Handling /house unsharegroup]
    private void HouseUnshareGroupCommand_Exec(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      if (args.Parameters.Count < 2) {
        args.Player.SendErrorMessage("Proper syntax: /house unsharegroup <group name>");
        args.Player.SendInfoMessage("Type /house unsharegroup help to get more information about this command.");
        return;
      }

      string shareTargetRaw = args.ParamsToSingleString(1);
      if (shareTargetRaw.Equals("help", StringComparison.InvariantCultureIgnoreCase)) {
        this.HouseUnshareGroupCommand_HelpCallback(args);
        return;
      }

      Group tsGroup = TShock.Groups.GetGroupByName(shareTargetRaw);
      if (tsGroup == null) {
        args.Player.SendErrorMessage("A group with the name \"{0}\" does not exist.", shareTargetRaw);
        return;
      }

      Region region;
      if (!this.TryGetAccessibleHouseRegionAtPlayer(args.Player, out region))
        return;

      if (!TShock.Regions.RemoveGroup(region.Name, tsGroup.Name)) {
        args.Player.SendErrorMessage("Internal error has occured.");
        return;
      }

      args.Player.SendSuccessMessage("Users of group \"{0}\" have no more build access to this house anymore.", tsGroup.Name);
    }

    private void HouseUnshareGroupCommand_HelpCallback(CommandArgs args) {
      if (args == null || this.IsDisposed)
        return;

      int pageNumber;
      if (!PaginationUtil.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
        return;

      switch (pageNumber) {
        default:
          args.Player.SendMessage("Command reference for /house unsharegroup (Page 1 of 1)", Color.Lime);
          args.Player.SendMessage("/house unsharegroup|unshareg <group name>", Color.White);
          args.Player.SendMessage("Removes build access of all users in a TShock group for the house at you character.", Color.LightGray);
          args.Player.SendMessage(string.Empty, Color.IndianRed);
          args.Player.SendMessage("NOTE: You have to own a house in order to alter shares of it,", Color.IndianRed);
          args.Player.SendMessage("just having build access is not sufficient.", Color.IndianRed);
          return;
      }
    }
    #endregion

    private bool TryGetHouseRegionAtPlayer(TSPlayer player, out string owner, out Region region) {
      Contract.Requires<ArgumentNullException>(player != null);

      int dummy;
      if (!this.HousingManager.TryGetHouseRegionAtPlayer(player, out owner, out dummy, out region)) {
        player.SendErrorMessage("There's no house on your current position.");
        return false;
      }

      return true;
    }

    private bool TryGetAccessibleHouseRegionAtPlayer(TSPlayer player, out string owner, out Region region) {
      Contract.Requires<ArgumentNullException>(player != null);

      if (!this.TryGetHouseRegionAtPlayer(player, out owner, out region))
        return false;

      if (player.User.Name!= owner && !player.Group.HasPermission(HouseRegionsPlugin.HousingMaster_Permission)) {
        player.SendErrorMessage("You're not the owner of this house.");
        return false;
      }

      return true;
    }

    private bool TryGetAccessibleHouseRegionAtPlayer(TSPlayer player, out Region region) {
      string dummy;
      return this.TryGetAccessibleHouseRegionAtPlayer(player, out dummy, out region);
    }

    private void SendFakeTileWire(TSPlayer player, DPoint tileLocation) {
      Tile tile = TerrariaUtils.Tiles[tileLocation];
      if (tile.wire2())
        return;

      try {
        tile.wire2(true);
        player.SendTileSquare(tileLocation, 1);
      } finally {
        tile.wire2(false);
      }
    }

    private void SendAreaDottedFakeWiresTimed(TSPlayer player, Rectangle area, int timeMs) {
      this.SendAreaDottedFakeWires(player, area);
                                
      System.Threading.Timer hideTimer = null;
      hideTimer = new System.Threading.Timer(state => {
          this.SendAreaDottedFakeWires(player, area, false);

          // ReSharper disable AccessToModifiedClosure
          Debug.Assert(hideTimer != null);
          hideTimer.Dispose();
          // ReSharper restore AccessToModifiedClosure
        },
        null, timeMs, Timeout.Infinite
      );
    }

    private void SendAreaDottedFakeWires(TSPlayer player, Rectangle area, bool setOrUnset = true) {
      foreach (Point boundaryPoint in TShock.Utils.EnumerateRegionBoundaries(area))
        if ((boundaryPoint.X + boundaryPoint.Y & 1) == 0)
          if (setOrUnset)
            this.SendFakeTileWire(player, new DPoint(boundaryPoint.X, boundaryPoint.Y));
          else
            player.SendTileSquare(boundaryPoint.X, boundaryPoint.Y, 1);
    }

    private void SendFakeWireCross(TSPlayer player, DPoint crossLocation) {
      this.SendFakeTileWire(player, crossLocation);
      this.SendFakeTileWire(player, crossLocation.OffsetEx(-1, 0));
      this.SendFakeTileWire(player, crossLocation.OffsetEx(1, 0));
      this.SendFakeTileWire(player, crossLocation.OffsetEx(0, -1));
      this.SendFakeTileWire(player, crossLocation.OffsetEx(0, 1));
    }

    private void ExplainInvalidRegionSize(TSPlayer toPlayer, Rectangle area, Configuration.HouseSizeConfig restrictingConfig) {
      if (restrictingConfig.Equals(this.Config.MinSize)) {
        toPlayer.SendErrorMessage("This region has no valid house size, it's too small:");
        toPlayer.SendErrorMessage("Min width: {0} (you've tried to set {1}).", restrictingConfig.Width, area.Width);
        toPlayer.SendErrorMessage("Min height: {0} (you've tried to set {1}).", restrictingConfig.Height, area.Height);
        toPlayer.SendErrorMessage("Min total blocks: {0} (you've tried to set {1}).", restrictingConfig.TotalTiles, area.Width * area.Height);
      } else {
        toPlayer.SendErrorMessage("This region has no valid house size, it's too large:");
        toPlayer.SendErrorMessage("Max width: {0} (you've tried to set {1}).", restrictingConfig.Width, area.Width);
        toPlayer.SendErrorMessage("Max height: {0} (you've tried to set {1}).", restrictingConfig.Height, area.Height);
        toPlayer.SendErrorMessage("Max total blocks: {0} (you've tried to set {1}).", restrictingConfig.TotalTiles, area.Width * area.Height);
      }
    }

    #region [IDisposable Implementation]
    protected override void Dispose(bool isDisposing) {
      if (this.IsDisposed)
        return;
      
      if (isDisposing)
        this.ReloadConfigurationCallback = null;

      base.Dispose(isDisposing);
    }
    #endregion
  }
}
