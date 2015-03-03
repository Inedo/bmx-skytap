using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.Skytap.SkytapApi;
using Inedo.Data;

namespace Inedo.BuildMasterExtensions.Skytap
{
    [ActionProperties(
        "Create BuildMaster Servers",
        "Creates servers in BuildMaster for virtual machines in a Skytap environment that has published services.")]
    [Tag("skytap")]
    [CustomEditor(typeof(CreateBuildMasterServersActionEditor))]
    public sealed class CreateBuildMasterServersAction : SkytapConfigurationActionBase
    {
        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "Create Servers in BuildMaster for ",
                    new Hilite(this.ConfigurationName),
                    " Environment"
                )
            );
        }

        internal override void Execute(SkytapClient client, SkytapConfiguration configuration)
        {
            var serverIds = this.CreateServers(configuration);

            if (serverIds.Count == 0)
            {
                this.LogWarning("There were no BuildMaster servers created for this environment.");
                return;
            }

            this.RunUpdater();

            this.LogInformation("Waiting for servers to enter ready state...");

            do
            {
                var statuses = StoredProcs
                    .Environments_GetServers(Domains.YN.No)
                    .Execute()
                    .Where(s => serverIds.Contains(s.Server_Id));

                foreach (var server in statuses)
                {
                    if (server.ServerStatus_Code == Domains.ServerStatus.Ready)
                    {
                        this.LogDebug("Server {0} is ready.", server.Server_Name);
                        serverIds.Remove(server.Server_Id);
                    }
                }

                this.Context.CancellationToken.WaitHandle.WaitOne(5000);
                this.ThrowIfCanceledOrTimeoutExpired();
            }
            while (serverIds.Count > 0);

            this.LogInformation("Servers are ready.");
        }

        private HashSet<int> CreateServers(SkytapConfiguration configuration)
        {
            var buildMasterServers = StoredProcs
                .Environments_GetServers(Domains.YN.Yes)
                .Execute()
                .ToDictionary(s => s.Server_Name, StringComparer.OrdinalIgnoreCase);

            var serverIds = new HashSet<int>();

            foreach (var vm in configuration.VirtualMachines)
            {
                var publishedService = vm
                    .NetworkInterfaces
                    .SelectMany(n => n.Services)
                    .Where(s => s.InternalPort == 6468 || s.InternalPort == 6864 || s.InternalPort == 22)
                    .FirstOrDefault();

                if (publishedService == null)
                {
                    this.LogInformation("Virtual machine {0} does not have any published services with known BuildMaster agent ports or SSH ports.", vm.Name);
                    continue;
                }

                this.LogDebug("Waiting for {0}:{1} to start accepting connections...", publishedService.ExternalIPAddress, publishedService.ExternalPort);
                using (var tcpClient = new TcpClient())
                {
                    bool connected = false;
                    do
                    {
                        using (var connectTask = Task.Factory.FromAsync(tcpClient.BeginConnect, tcpClient.EndConnect, publishedService.ExternalIPAddress, publishedService.ExternalPort, null))
                        {
                            while (!connectTask.IsCompleted)
                            {
                                connectTask.Wait(100);
                                this.ThrowIfCanceledOrTimeoutExpired();
                            }

                            if (connectTask.Exception == null)
                                connected = true;
                            else
                                Thread.Sleep(1000);
                        }
                    }
                    while (!connected);
                }

                var bmServer = buildMasterServers.GetValueOrDefault("Skytap-" + vm.Id);
                var agent = this.CreateAgent(publishedService, vm.Credentials);
                if (agent != null)
                {
                    int serverId;
                    if (bmServer == null)
                    {
                        this.LogInformation("Creating server Skytap-{0} for virtual machine {1}...", vm.Id, vm.Name);

                        serverId = (int)StoredProcs.Environments_CreateOrUpdateServer(
                            Server_Id: null,
                            Server_Name: "Skytap-" + vm.Id,
                            ServerType_Code: Domains.ServerTypeCodes.Server,
                            Agent_Configuration: Util.Persistence.SerializeToPersistedObjectXml(agent),
                            Active_Indicator: Domains.YN.Yes
                        ).Execute();
                    }
                    else
                    {
                        serverId = bmServer.Server_Id;
                        var agentConfigString = Util.Persistence.SerializeToPersistedObjectXml(agent);

                        if (agentConfigString != bmServer.Agent_Configuration || !(YNIndicator)bmServer.Active_Indicator)
                        {
                            this.LogInformation("Updating server configuration for server Skytap-{0} (virtual machine {1})...", vm.Id, vm.Name);

                            StoredProcs.Environments_CreateOrUpdateServer(
                                Server_Id: bmServer.Server_Id,
                                Server_Name: bmServer.Server_Name,
                                ServerType_Code: bmServer.ServerType_Code,
                                Agent_Configuration: agentConfigString,
                                Active_Indicator: Domains.YN.Yes
                            ).Execute();
                        }
                        else
                        {
                            this.LogInformation("Server Skytap-{0} already created for virtual machine {1}.", vm.Id, vm.Name);
                        }
                    }

                    serverIds.Add(serverId);

                    this.SetSkytapVariableValue("VM-" + vm.Name, "Skytap-" + vm.Id);
                }
            }

            return serverIds;
        }
        private AgentBase CreateAgent(SkytapPublishedService service, ReadOnlyCollection<SkytapCredential> credentials)
        {
            if (service.InternalPort == 22)
            {
                if (credentials.Count == 0)
                {
                    this.LogWarning("There are no credentials available; cannot create an SSH agent.");
                    return null;
                }

                var agent = Util.Persistence.CreateDynamicInstance(
                    "Inedo.BuildMaster.Extensibility.Agents.Ssh.SshAgent",
                    "BuildMasterExtensions"
                );

                agent.AgentHostName = service.ExternalIPAddress;
                agent.Port = service.ExternalPort;

                foreach (var credential in credentials)
                {
                    agent.UserName = credential.UserName;
                    agent.Password = credential.Password;

                    using (AgentBase testAgent = agent)
                    {
                        try
                        {
                            if (testAgent.GetAgentStatus(null) == Domains.ServerStatus.Ready)
                                return agent;
                        }
                        catch
                        {
                            this.LogWarning("Could not connect to {0}:{1}.", service.ExternalIPAddress, service.ExternalPort);
                            return null;
                        }
                    }
                }

                return null;
            }
            else if (service.InternalPort == 6468 || service.InternalPort == 6864)
            {
                var agent = Util.Persistence.CreateDynamicInstance(
                    service.InternalPort == 6468 ? "Inedo.BuildMaster.Extensibility.Agents.Tcp.TcpAgent" : "Inedo.BuildMaster.Extensibility.Agents.Soap.SoapAgent",
                    "BuildMasterExtensions"
                );

                agent.HostName = service.ExternalIPAddress;
                agent.PortNumber = service.ExternalPort;

                var hostedAgentContext = (IHostedAgentContext)Type
                    .GetType("Inedo.BuildMaster.Agents.HostedAgentContext,BuildMaster")
                    .GetProperty("Instance")
                    .GetValue(null, null);

                using (AgentBase testAgent = agent)
                {
                    try
                    {
                        testAgent.GetAgentStatus(hostedAgentContext);
                    }
                    catch
                    {
                        this.LogWarning("Could not connect to {0}:{1}.", service.ExternalIPAddress, service.ExternalPort);
                        return null;
                    }
                }

                return agent;
            }
            else
            {
                throw new ArgumentException();
            }
        }
        private void RunUpdater()
        {
            this.LogDebug("Triggering an agent update scan...");
            
            var message = Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Messaging.RunExecuterMessage,BuildMaster"), "AgentUpdater");
            Type.GetType("Inedo.BuildMaster.Windows.ServiceApplication.WebUtil,bmservice")
                .GetMethod("HandleRunExecuter", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new[] { message });

            this.LogDebug("Agent update scan initiated.");
        }
    }
}
