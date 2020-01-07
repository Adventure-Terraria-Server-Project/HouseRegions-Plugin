using System;
using System.Runtime.Serialization;

namespace Terraria.Plugins.CoderCow.HouseRegions {
  [Serializable]
  public class HouseOverlapException : Exception {
    public HouseOverlapException(string message, Exception inner = null): base(message, inner) {}

    public HouseOverlapException(): base("The house overlaps with another.") {}

    protected HouseOverlapException(SerializationInfo info, StreamingContext context): base(info, context) {}
  }
}