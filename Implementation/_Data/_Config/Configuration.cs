using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Xml;
using System.Xml.Schema;

using Terraria.Plugins.Common;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  public class Configuration {
    #region [Nested: HouseSizeConfig]
    public struct HouseSizeConfig {
      #region [Property: TotalTiles]
      private readonly int totalTiles;

      public int TotalTiles {
        get { return this.totalTiles; }
      }
      #endregion

      #region [Property: Width]
      private readonly int width;

      public int Width {
        get { return this.width; }
      }
      #endregion

      #region [Property: Height]
      private readonly int height;

      public int Height {
        get { return this.height; }
      }
      #endregion


      #region [Method: Static FromXmlElement]
      public static HouseSizeConfig FromXmlElement(XmlElement rootElement) {
        Contract.Requires<ArgumentNullException>(rootElement != null);

        int totalTiles = int.Parse(rootElement["TotalTiles"].InnerText);
        int width = int.Parse(rootElement["Width"].InnerText);
        int height = int.Parse(rootElement["Height"].InnerText);

        return new HouseSizeConfig(totalTiles, width, height);
      }
      #endregion

      #region [Method: Constructor]
      public HouseSizeConfig(int totalTiles, int width, int height) {
        this.totalTiles = totalTiles;
        this.width = width;
        this.height = height;
      }
      #endregion
    }
    #endregion

    #region [Constants]
    public const string CurrentVersion = "1.0";
    #endregion

    #region [Property: MaxHousesPerUser]
    private int maxHousesPerUser;

    public int MaxHousesPerUser {
      get { return this.maxHousesPerUser; }
      set { this.maxHousesPerUser = value; }
    }
    #endregion

    #region [Property: MinSize]
    private HouseSizeConfig minSize;

    public HouseSizeConfig MinSize {
      get { return this.minSize; }
      set { this.minSize = value; }
    }
    #endregion

    #region [Property: MaxSize]
    private HouseSizeConfig maxSize;

    public HouseSizeConfig MaxSize {
      get { return this.maxSize; }
      set { this.maxSize = value; }
    }
    #endregion

    #region [Property: AllowTShockRegionOverlapping]
    private bool allowTShockRegionOverlapping;

    public bool AllowTShockRegionOverlapping {
      get { return this.allowTShockRegionOverlapping; }
      set { this.allowTShockRegionOverlapping = value; }
    }
    #endregion

    #region [Property: DefaultZIndex]
    private int defaultZIndex;

    public int DefaultZIndex {
      get { return this.defaultZIndex; }
      set { this.defaultZIndex = value; }
    }
    #endregion


    #region [Methods: Static Read]
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
      resultingConfig.maxHousesPerUser = int.Parse(rootElement["MaxHousesPerUser"].InnerText);
      resultingConfig.minSize = HouseSizeConfig.FromXmlElement(rootElement["MinHouseSize"]);
      resultingConfig.maxSize = HouseSizeConfig.FromXmlElement(rootElement["MaxHouseSize"]);
      resultingConfig.allowTShockRegionOverlapping = BoolEx.ParseEx(rootElement["AllowTShockRegionOverlapping"].InnerText);
      resultingConfig.defaultZIndex = int.Parse(rootElement["DefaultZIndex"].InnerText);

      return resultingConfig;
    }
    #endregion

    #region [Method: Constructor]
    public Configuration() {}
    #endregion
  }
}