using Akka.Actor;
using Akka.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Services
{
    public class AkkaService : IHostedService
    {
        private ActorSystem _actorSystem;
        private readonly IServiceProvider _sp;

        public AkkaService(IServiceProvider sp)
        {
            _sp = sp;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Akka.Configuration.Config akkaConfig = @"akka.loglevel = DEBUG
                    akka.loggers=[""Akka.Logger.NLog.NLogLogger, Akka.Logger.NLog""]";
            var bootstrap = BootstrapSetup.Create().WithConfig(akkaConfig);
            var di = DependencyResolverSetup.Create(_sp);
            var actorSystemSetup = bootstrap.And(di);
            _actorSystem = ActorSystem.Create("FMSystem", actorSystemSetup);

            //services.AddSingleton(sp => ActorSystem.Create("FMSystem", akkaConfig));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _actorSystem.Dispose();
        }
    }
}
