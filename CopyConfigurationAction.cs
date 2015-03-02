using System.Net;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Copy Skytap Environment",
        "Copies an existing Skytap environment to a new environment.")]
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
            var shortDesc = new ShortActionDescription("Copy ", new Hilite(this.ConfigurationName), " Skytap Environment");

            var longDesc = new LongActionDescription(
                "to ",
                new Hilite(Util.CoalesceStr(this.NewConfigurationName, "new environment"))
            );

            if (this.ExportVariables)
            {
                longDesc.AppendContent(
                    ", and set ${Skytap-EnvironmentId} to the environment ID"
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
                this.LogInformation("Creating environment from {1} environment...", configuration.Name);
            else
                this.LogInformation("Creating {0} environment from {1} template...", this.NewConfigurationName, configuration.Name);

            string configurationId;
            try
            {
                configurationId = client.CopyConfiguration(configuration.Id);
            }
            catch (WebException ex)
            {
                this.LogError("The environment could not be copied: " + ex.Message);
                return;
            }

            this.LogDebug("Environment copied (ID={0})", configurationId);

            if (!string.IsNullOrWhiteSpace(this.NewConfigurationName))
            {
                this.LogDebug("Setting environment name to {0}...", this.NewConfigurationName);
                client.RenameConfiguration(configurationId, this.NewConfigurationName);
                this.LogDebug("Environment renamed.");
            }

            if (this.ExportVariables)
                this.SetSkytapVariableValue("EnvironmentId", configurationId);

            this.LogInformation("Environment copied.");
        }
    }
}
