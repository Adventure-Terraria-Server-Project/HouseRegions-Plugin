HouseRegions-Plugin
===================

####A TShock Region Wrapper for Housing Purposes

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

Note: This plugin requires Terraria Server API 1.18 and a TShock 4.3 build in 
order to work.

More information to this plugin can be found [here](tshock/House Regions/ReadMe.txt).

Suggestions? Bugs? File issues here:
https://github.com/CoderCow/HouseRegions-Plugin/issues
