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

Note: This plugin requires Terraria Server API 1.14 and TShock 4.2.

Suggestions? Bugs? File issues here:
https://github.com/CoderCow/HouseRegions-Plugin/issues


Commands
---------------------------------------------------------------------------------
/House
/House Commands
/House Summary
/House Info
/House Define
/House Resize <up|down|left|right> <amount>
/House Share <user>
/House Unshare <user>
/House ShareGroup <group>
/House UnshareGroup <group>
/House Delete
/House Scan
/House ReloadConfig

To get more information about a command type 
/<command> help
ingame.


Permissions
---------------------------------------------------------------------------------
houseregions.define
  Can define new or resize existing houses.
houseregions.delete
  Can delete existing houses.
houseregions.share
  Can share houses.
houseregions.sharewithgroups
  Can share houses with TShock groups.
houseregions.nolimits
  Can define houses without a maximum limit or size restrictions.

houseregions.housingmaster
  Can display a list of all house owners. Can change settings of any house, either 
  owned or not owned.
houseregions.cfg
  Can reload the configuration file.

Changelog
---------------------------------------------------------------------------------
Version 1.1.1 [02/13/2014]
  -Fixed a bug causing unhandled exceptions when a house with a size of 0, 0 was 
   tried to be defined.
  -Increased range of /house scan by 200% and time by 100%.
  -Updated to Plugin Common Lib 2.6.

Version 1.1.0 [10/03/2013]
  -Updated for Terraria 1.2 and TShock 4.2 pre.
  -Changed the permission model to "houseregions.<perm>" ("_" to ".").
  -Added /house summary to list all current house owners and the amount of owned
   house regions.
  -Added /house scan to display all nearby house regions by wires.
  -Fixed /house resize not working for house masters on unowned houses.
  -Fixed a bug causing command help to not work for sub-commands of /house.
  -Fixed some typos.

Version 1.0.0 [08/14/2013]
  -First public release by CoderCow.