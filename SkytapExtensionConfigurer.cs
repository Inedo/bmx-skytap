using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [CustomEditor(typeof(SkytapExtensionConfigurerEditor))]
    public sealed class SkytapExtensionConfigurer : ExtensionConfigurerBase
    {
        [Persistent]
        public string UserName { get; set; }
        [Persistent(Encrypted = true)]
        public string Password { get; set; }

        public override string ToString()
        {
            return string.Empty;
        }
    }
}
