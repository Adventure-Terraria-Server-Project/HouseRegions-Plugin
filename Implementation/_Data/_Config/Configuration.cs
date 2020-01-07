using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

using Terraria.Plugins.Common;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  public class Configuration {
    #region [Nested: HouseSizeConfig]
    public struct HouseSizeConfig {
      public int TotalTiles { get; private set; }
      public int Width { get; private set; }
      public int Height { get; private set; }


      public static HouseSizeConfig FromXmlElement(XmlElement rootElement) {
        if (rootElement == null) throw new ArgumentNullException();

        int totalTiles = int.Parse(rootElement["TotalTiles"].InnerText);
        int width = int.Parse(rootElement["Width"].InnerText);
        int height = int.Parse(rootElement["Height"].InnerText);

        return new HouseSizeConfig(totalTiles, width, height);
      }

      public HouseSizeConfig(int totalTiles, int width, int height): this() {
        this.TotalTiles = totalTiles;
        this.Width = width;
        this.Height = height;
      }
    }
    #endregion

    public const string CurrentVersion = "1.0";

    public int MaxHousesPerUser { get; set; }
    public HouseSizeConfig MinSize { get; set; }
    public HouseSizeConfig MaxSize { get; set; }
    public bool AllowTShockRegionOverlapping { get; set; }
    public int DefaultZIndex { get; set; }


    public static Configuration Read(string filePath) {
      XmlReaderSettings configReaderSettings = new XmlReaderSettings {
        ValidationType = ValidationType.Schema,
        ValidationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints | XmlSchemaValidationFlags.ReportValidationWarnings
      };

      string configSchemaPath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".xsd");
      configReaderSettings.Schemas.Add(null, configSchemaPath);
      
      XmlDocument document = new XmlDocument();
      using (XmlReader configReader = XmlReader.Create(filePath, configReaderSettings))
        document.Load(configReader);

      // Before validating using the schema, first check if the configuration file's version matches with the supported version.
      XmlElement rootElement = document.DocumentElement;
      string fileVersionRaw;
      if (rootElement.HasAttribute("Version"))
        fileVersionRaw = rootElement.GetAttribute("Version");
      else
        fileVersionRaw = "1.0";
      
      if (fileVersionRaw != Configuration.CurrentVersion) {
        throw new FormatException(string.Format(
          "The configuration file is either outdated or too new. Expected version was: {0}. File version is: {1}", 
          Configuration.CurrentVersion, fileVersionRaw
        ));
      }

      Configuration resultingConfig = new Configuration();
      resultingConfig.MaxHousesPerUser = int.Parse(rootElement["MaxHousesPerUser"].InnerText);
      resultingConfig.MinSize = HouseSizeConfig.FromXmlElement(rootElement["MinHouseSize"]);
      resultingConfig.MaxSize = HouseSizeConfig.FromXmlElement(rootElement["MaxHouseSize"]);
      resultingConfig.AllowTShockRegionOverlapping = BoolEx.ParseEx(rootElement["AllowTShockRegionOverlapping"].InnerText);
      resultingConfig.DefaultZIndex = int.Parse(rootElement["DefaultZIndex"].InnerText);

      return resultingConfig;
    }
  }
}