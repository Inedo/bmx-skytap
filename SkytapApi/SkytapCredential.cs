using System.Xml.Linq;

namespace Inedo.BuildMasterExtensions.Skytap.SkytapApi
{
    internal sealed class SkytapCredential
    {
        public SkytapCredential(XElement credential)
        {
            var text = (string)credential.Element("text");
            if (!string.IsNullOrWhiteSpace(text))
            {
                var parts = text.Split(new[] { '/' }, 2);
                if (parts.Length == 1)
                {
                    this.UserName = text.Trim();
                    this.Password = string.Empty;
                }
                else
                {
                    this.UserName = parts[0].Trim();
                    this.Password = parts[1].Trim();
                }
            }
        }

        public string UserName { get; private set; }
        public string Password { get; private set; }
    }
}
