using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Stop Skytap Configuration",
        "Suspends, shuts down, or powers off a Skytap configuration and optionally waits for its runstate to change.")]
    [Tag("skytap")]
    [CustomEditor(typeof(StopConfigurationActionEditor))]
    public sealed class StopConfigurationAction : SkytapConfigurationActionBase
    {
        [Persistent]
        public bool WaitForStop { get; set; }
        [Persistent]
        public StopConfigurationMode Runstate { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    InedoLib.Util.Switch<StopConfigurationMode, string>(this.Runstate)
                        .Case(StopConfigurationMode.Suspend, "Suspend ")
                        .Case(StopConfigurationMode.ShutDown, "Shut Down ")
                        .Case(StopConfigurationMode.PowerOff, "Power Off ")
                    .End(),
                    new Hilite(this.ConfigurationName),
                    " Skytap Configuration"
                ),
                new LongActionDescription(
                    this.WaitForStop ? "and wait for it to enter stopped state" : string.Empty
                )
            );
        }

        internal override void Execute(SkytapClient client, SkytapConfiguration configuration)
        {
            var runstate = client.GetConfigurationRunstate(configuration.Id);
            if (runstate == "stopped")
            {
                this.LogInformation("Configuration {0} is already stopped.", configuration.Name);
                return;
            }

            if (runstate == "suspended" && this.Runstate == StopConfigurationMode.Suspend)
            {
                this.LogInformation("Configuration {0} is already suspended.", configuration.Name);
                return;
            }

            this.LogInformation(
                "{0} configuration...",
                InedoLib.Util.Switch<StopConfigurationMode, string>(this.Runstate)
                    .Case(StopConfigurationMode.Suspend, "Suspending")
                    .Case(StopConfigurationMode.ShutDown, "Shutting down")
                    .Case(StopConfigurationMode.PowerOff, "Powering off")
                .End()
            );

            var targetRunstate = InedoLib.Util.Switch<StopConfigurationMode, string>(this.Runstate)
                .Case(StopConfigurationMode.Suspend, "suspended")
                .Case(StopConfigurationMode.ShutDown, "stopped")
                .Case(StopConfigurationMode.PowerOff, "halted")
                .End();

            client.SetConfigurationRunstate(configuration.Id, targetRunstate);

            if (this.WaitForStop)
            {
                this.LogInformation("Stop command issued; waiting for configuration to enter {0} state...", this.Runstate == StopConfigurationMode.Suspend ? "suspended" : "stopped");

                do
                {
                    this.Context.CancellationToken.WaitHandle.WaitOne(5000);
                    this.ThrowIfCanceledOrTimeoutExpired();
                    runstate = client.GetConfigurationRunstate(configuration.Id);
                }
                while (runstate == "busy");

                if (runstate == "stopped" || runstate == "suspended")
                    this.LogInformation("Configuration is {0}.", runstate);
                else
                    this.LogError("Configuration is {0}.", runstate);
            }
            else
            {
                this.LogInformation("Stop command issued.");
            }
        }
    }
}
