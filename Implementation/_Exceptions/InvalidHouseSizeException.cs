using System;
using System.Runtime.Serialization;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  [Serializable]
  public class InvalidHouseSizeException: Exception {
    #region [Property: RestrictingConfig]
    private readonly Configuration.HouseSizeConfig restrictingConfig;

    public Configuration.HouseSizeConfig RestrictingConfig {
      get { return this.restrictingConfig; }
    }
    #endregion

    public InvalidHouseSizeException(string message, Exception inner = null): base(message, inner) {}

    public InvalidHouseSizeException(Configuration.HouseSizeConfig restrictingConfig): base("The size of the house does not match with the configured min / max settings.") {
      this.restrictingConfig = restrictingConfig;
    }

    public InvalidHouseSizeException(): base("The size of the house does not match with the configured min / max settings.") {}

    protected InvalidHouseSizeException(SerializationInfo info, StreamingContext context): base(info, context) {}
  }
}