using System.Net;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Copy Skytap Configuration",
        "Copies an existing Skytap configuration to a new configuration.")]
    [Tag("skytap")]
    [CustomEditor(typeof(CopyConfigurationActionEditor))]
    public sealed class CopyConfigurationAction : SkytapConfigurationActionBase
    {
        [Persistent]
        public string NewConfigurationName { get; set; }
        [Persistent]
        public bool ExportVariables { get; set; }

        public override ActionDescription GetActionDescription()
        {
            var shortDesc = new ShortActionDescription("Copy ", new Hilite(this.ConfigurationName), " Skytap Configuration");

            var longDesc = new LongActionDescription(
                "to ",
                new Hilite(Util.CoalesceStr(this.NewConfigurationName, "new configuration"))
            );

            if (this.ExportVariables)
            {
                longDesc.AppendContent(
                    ", and set ${Skytap-ConfigurationId} to the configuration ID"
                );
            }

            return new ActionDescription(
                shortDesc,
                longDesc
            );
        }

        internal override void Execute(SkytapClient client, SkytapConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(this.NewConfigurationName))
                this.LogInformation("Creating configuration from {1} configuration...", configuration.Name);
            else
                this.LogInformation("Creating {0} configuration from {1} template...", this.NewConfigurationName, configuration.Name);

            string configurationId;
            try
            {
                configurationId = client.CopyConfiguration(configuration.Id);
            }
            catch (WebException ex)
            {
                this.LogError("The configuration could not be copied: " + ex.Message);
                return;
            }

            this.LogDebug("Configuration copied (ID={0})", configurationId);

            if (!string.IsNullOrWhiteSpace(this.NewConfigurationName))
            {
                this.LogDebug("Setting configuration name to {0}...", this.NewConfigurationName);
                client.RenameConfiguration(configurationId, this.NewConfigurationName);
                this.LogDebug("Configuration renamed.");
            }

            if (this.ExportVariables)
                this.SetSkytapVariableValue("ConfigurationId", configurationId);

            this.LogInformation("Configuration copied.");
        }
    }
}
