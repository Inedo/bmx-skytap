namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal class SkytapResource
    {
        public SkytapResource(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
    }
}
