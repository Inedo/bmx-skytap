using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    public abstract class SkytapActionBase : ActionBase
    {
        internal SkytapActionBase()
        {
        }

        protected override sealed void Execute()
        {
            var configurer = (SkytapExtensionConfigurer)this.GetExtensionConfigurer();
            if (configurer == null)
            {
                this.LogError("A configuration profile is required for this action.");
                return;
            }

            if (string.IsNullOrWhiteSpace(configurer.UserName))
            {
                this.LogError("A user name must be specified in the Skytap extension configuration profile.");
                return;
            }

            if (string.IsNullOrWhiteSpace(configurer.Password))
            {
                this.LogError("A password must be specified in the Skytap extension configuration profile.");
                return;
            }

            try
            {
                var client = new SkytapClient(configurer.UserName, configurer.Password);
                this.Execute(client);
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response == null)
                    throw;

                try
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        var xdoc = XDocument.Load(responseStream);
                        var error = (string)xdoc.Descendants("error").FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            this.LogError("Error returned from Skytap: ({0}) {1}", (int)response.StatusCode, error);
                            return;
                        }
                    }
                }
                catch
                {
                }

                this.LogError("Error returned from Skytap: ({0}) {1}", (int)response.StatusCode, response.StatusDescription);
            }
        }

        internal abstract void Execute(SkytapClient client);

        protected bool SetSkytapVariableValue(string name, string value)
        {
            int maxLength = 50 - "Skytap-".Length;
            if (name.Length > maxLength)
            {
                this.LogWarning("Cannot create variable for {0}; variable name would be greater than 50 characters.", name);
                return false;
            }

            if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_\-\. ]+$"))
            {
                this.LogWarning("Cannot create variable for {0}; variable name would contain invalid characters.", name);
                return false;
            }

            this.LogDebug("Creating variable ${{Skytap-{0}}}...", name);

            StoredProcs.Variables_CreateOrUpdateVariableDefinition(
                Variable_Name: "Skytap-" + name,
                Environment_Id: this.Context.EnvironmentId,
                Server_Id: null,
                ApplicationGroup_Id: null,
                Application_Id: this.Context.ApplicationId,
                Deployable_Id: null,
                Release_Number: this.Context.ReleaseNumber,
                Build_Number: this.Context.BuildNumber,
                Execution_Id: this.Context.ExecutionId,
                Value_Text: value,
                Sensitive_Indicator: Domains.YN.No
            ).Execute();

            this.LogDebug("Variable ${{Skytap-{0}}} set to {1}.", name, value);

            return true;
        }
    }
}
