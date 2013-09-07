=================================================================================
 Housing Regions for TerrariaServer-API and TShock
   (c) CoderCow 2013
=================================================================================
 
A TShock Region Wrapper for Housing Purposes
---------------------------------------------------------------------------------

This plugin provides players on TShock driven Terraria servers the possibility 
of defining houses in which other players can not alter any tiles. It 
accomplishes this by utilizing TShock's region system, i.e. this plugin simply
wraps the region system functionality with an easy to use and more restricted
interface designed for regular users.

For quick usage and for the sake of usabilitiy house regions are kept entirely 
unnamed, when being defined two points to mark the region boundaries are 
sufficient.
To change parameters of a house region later, like adding shared players or 
groups, the player must simply stand in the region they want to change and 
execute the related house commands. The maximum amount of house regions per 
user, several size restrictions, and whether house regions can overlap with
regular TShock regions can be configured.

Warning: TShock regions defined through this plugin are named in the format 
"*H_<User>:<HouseIndex>" thus, if you manually define a TShock region with this 
name format, this plugin will treat the region like a house.

Note: This plugin requires Terraria Server API 1.12 and a TShock 4.1 build in 
order to work.

Suggestions? Bugs? File issues here:
https://github.com/CoderCow/HouseRegions-Plugin/issues


Commands
---------------------------------------------------------------------------------
/House
/House Commands
/House Info
/House Define
/House Resize <up|down|left|right> <amount>
/House Share <user>
/House Unshare <user>
/House ShareGroup <group>
/House UnshareGroup <group>
/House Delete
/House ReloadConfig

To get more information about a command type 
/<command> help
ingame.


Permissions
---------------------------------------------------------------------------------
houseregions_define
  Can define new or resize existing houses.
houseregions_delete
  Can delete existing houses.
houseregions_share
  Can share houses.
houseregions_sharewithgroups
  Can share houses with TShock groups.
houseregions_nolimits
  Can define houses without a maximum limit or size restrictions.

houseregions_housingmaster
  Can change settings of any house, either owned or not owned.
houseregions_cfg
  Can reload the configuration file.

Changelog
---------------------------------------------------------------------------------
Version 1.1.0 [09/07/2013]
  -Fixed some typos.

Version 1.0.0 [08/14/2013]
  -First public release by CoderCow.