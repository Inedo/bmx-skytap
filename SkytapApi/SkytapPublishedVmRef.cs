namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal sealed class SkytapPublishedVmRef
    {
        public SkytapPublishedVmRef(string id, string access, string desktopUrl = null)
        {
            this.Id = id;
            this.Access = access;
            this.DesktopUrl = desktopUrl;
        }

        public string Id { get; private set; }
        public string Access { get; private set; }
        public string DesktopUrl { get; private set; }
    }
}
