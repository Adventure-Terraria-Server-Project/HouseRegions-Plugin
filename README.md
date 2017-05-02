House Regions Plugin
===================

### A TShock Region Wrapper for Housing Purposes

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
name format, this plugin will treat the region just like a house.

Releases of this plugin use [Semantic Versioning](http://semver.org/).

### How to Install

Note: This plugin requires [TerrariaAPI-Server](https://github.com/NyxStudios/TerrariaAPI-Server) and [TShock](https://github.com/NyxStudios/TShock) in order to work. You can't use this with a vanilla Terraria server.

Grab the latest release and put the _.dll_ files into your server's _ServerPlugins_ directory. Also put the contents of the _tshock/_ folder into your server's _tshock_ folder. You may change the configuration options to your needs by editing the _tshock/House Regions/Config.xml_ file.

### Commands

* `/house`
* `/house commands`
* `/house summary`
* `/house info`
* `/house define`
* `/house resize <up|down|left|right> <amount>`
* `/house share <user>`
* `/house unshare <user>`
* `/house shareGroup <group>`
* `/house unshareGroup <group>`
* `/house delete`
* `/house scan`
* `/house reloadconfig`

To get more information about a command type `/<command> help` ingame.

### Permissions

* **houseregions.define**
  Can define new or resize existing houses.
* **houseregions.delete**
  Can delete existing houses.
* **houseregions.share**
  Can share houses.
* **houseregions.sharewithgroups**
  Can share houses with TShock groups.
* **houseregions.nolimits**
  Can define houses without a maximum limit or size restrictions.
* **houseregions.housingmaster**
  Can display a list of all house owners. Can change settings of any house, either 
  owned or not owned.
* **houseregions.cfg**
  Can reload the configuration file.

### Credits

Icon made by [freepik](http://www.freepik.com/)