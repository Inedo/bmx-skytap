using Inedo.BuildMaster;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.Skytap
{
    public abstract class SkytapConfigurationActionBase : SkytapActionBase
    {
        private bool allowConfigNotFound;

        protected SkytapConfigurationActionBase()
            : this(false)
        {
        }
        protected SkytapConfigurationActionBase(bool allowConfigNotFound)
        {
            this.allowConfigNotFound = allowConfigNotFound;
        }

        [Persistent]
        public string ConfigurationId { get; set; }
        [Persistent]
        public string ConfigurationName { get; set; }

        internal sealed override void Execute(SkytapClient client)
        {
            if (string.IsNullOrWhiteSpace(this.ConfigurationId) && string.IsNullOrWhiteSpace(this.ConfigurationName))
            {
                this.LogError("Configuration ID or configuration name must be specified.");
                return;
            }

            var notFoundLogLevel = this.allowConfigNotFound ? MessageLevel.Debug : MessageLevel.Error;

            this.LogDebug("Looking up configuration on Skytap...");

            SkytapConfiguration configuration = null;
            if (!string.IsNullOrWhiteSpace(this.ConfigurationId))
            {
                configuration = client.GetConfiguration(this.ConfigurationId);
                if (configuration == null)
                {
                    if (string.IsNullOrWhiteSpace(this.ConfigurationName))
                    {
                        var message = "Could not find configuration with ID=" + this.ConfigurationId;
                        this.Log(notFoundLogLevel, message);
                        if (this.allowConfigNotFound)
                            this.Execute(client, null);
                        return;
                    }
                    else
                    {
                        this.LogDebug("Could not find configuration with ID=" + this.ConfigurationId + "; looking up configuration with name=" + this.ConfigurationName);
                    }
                }
            }

            if(!string.IsNullOrWhiteSpace(this.ConfigurationName))
            {
                configuration = client.GetConfigurationFromName(this.ConfigurationName);
                if (configuration == null)
                {
                    var message = "Could not find configuration with name=" + this.ConfigurationName;
                    this.Log(notFoundLogLevel, message);
                    if (this.allowConfigNotFound)
                        this.Execute(client, null);
                    return;
                }
            }

            this.LogDebug("Found configuration ID={0}, name={1}", configuration.Id, configuration.Name);
            this.Execute(client, configuration);
        }
        internal abstract void Execute(SkytapClient client, SkytapConfiguration configuration);
    }
}
