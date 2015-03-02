using System.Net;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Create Skytap Environment",
        "Creates a new Skytap environment from an existing template.")]
    [Tag("skytap")]
    [CustomEditor(typeof(CreateConfigurationActionEditor))]
    public sealed class CreateConfigurationAction : SkytapActionBase
    {
        [Persistent]
        public string TemplateId { get; set; }
        [Persistent]
        public string TemplateName { get; set; }
        [Persistent]
        public string ConfigurationName { get; set; }
        [Persistent]
        public bool ExportVariables { get; set; }

        public override ActionDescription GetActionDescription()
        {
            var shortDesc = new ShortActionDescription();
            if (string.IsNullOrWhiteSpace(this.ConfigurationName))
            {
                shortDesc.AppendContent("Create Skytap Configuration");
            }
            else
            {
                shortDesc.AppendContent(
                    "Create ",
                    new Hilite(this.ConfigurationName),
                    " Skytap Environment"
                );
            }

            var longDesc = new LongActionDescription(
                "from ",
                new Hilite(this.TemplateName)
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

        internal override void Execute(SkytapClient client)
        {
            if (string.IsNullOrWhiteSpace(this.TemplateId) && string.IsNullOrWhiteSpace(this.TemplateName))
            {
                this.LogError("Template ID or template name must be specified.");
                return;
            }

            SkytapResource template = null;
            if (!string.IsNullOrWhiteSpace(this.TemplateId))
            {
                template = client.GetTemplate(this.TemplateId);
                if (template == null)
                {
                    if (string.IsNullOrWhiteSpace(this.TemplateName))
                    {
                        this.LogError("Could not find template with ID=" + this.TemplateId);
                        return;
                    }
                    else
                    {
                        this.LogDebug("Could not find template with ID=" + this.TemplateId + "; looking up template with name=" + this.TemplateName);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(this.TemplateName))
            {
                template = client.GetTemplateFromName(this.TemplateName);
                if (template == null)
                {
                    this.LogError("Could not find template with name=" + this.TemplateName);
                    return;
                }
            }

            this.LogDebug("Found template ID={0}, name={1}", template.Id, template.Name);
            this.Execute(client, template);
        }

        private void Execute(SkytapClient client, SkytapResource template)
        {
            if (string.IsNullOrWhiteSpace(this.ConfigurationName))
                this.LogInformation("Creating environment from {1} template...", template.Name);
            else
                this.LogInformation("Creating {0} environment from {1} template...", this.ConfigurationName, template.Name);

            string configurationId;
            try
            {
                configurationId = client.CreateConfiguration(template.Id);
            }
            catch (WebException ex)
            {
                this.LogError("The environment could not be created: " + ex.Message);
                return;
            }

            this.LogDebug("Environment created (ID={0})", configurationId);

            if (!string.IsNullOrWhiteSpace(this.ConfigurationName))
            {
                this.LogDebug("Setting environment name to {0}...", this.ConfigurationName);
                client.RenameConfiguration(configurationId, this.ConfigurationName);
                this.LogDebug("Environment renamed.");
            }

            if (this.ExportVariables)
                this.SetSkytapVariableValue("EnvironmentId", configurationId);

            this.LogInformation("Environment created.");
        }
    }
}
