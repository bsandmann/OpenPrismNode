namespace OpenPrismNode.Web.Common;

public static class OpnVersion
{
   public static string GetVersion()
   {
      var version = typeof(Program)
         .Assembly
         .GetName()
         .Version;
      if (version is null)
      {
         return "unknown";
      }
      return $"{version.Major}.{version.Minor}.{version.Build}";
   }
}