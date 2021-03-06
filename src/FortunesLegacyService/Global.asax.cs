﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.Wcf;
using Autofac.Integration.Web;
using FortuneCommon;
using FortuneCookieDatabase;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MySql.Data.MySqlClient;
using Pivotal.Discovery.Client;
using Steeltoe.CloudFoundry.Connector.MySql;
using Steeltoe.CloudFoundry.Connector.MySql.EF6;
using Steeltoe.Extensions.Configuration;


namespace FortunesLegacyService
{
    public class Global : HttpApplication, IContainerProviderAccessor
    {
        private static IContainerProvider _containerProvider;

        public IContainerProvider ContainerProvider
        {
            get { return _containerProvider; }
        }

        public static MySqlConnection DatabaseFactory() => _containerProvider.RequestLifetime.Resolve<MySqlConnection>();
        void Application_Start(object sender, EventArgs e)
        {

            ServerConfig.RegisterConfig("development", (env, configBuilder) => 
                configBuilder.SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddCloudFoundry()
                .AddEnvironmentVariables());


            var services = new ServiceCollection();
            services.AddDiscoveryClient(ServerConfig.Configuration);
            services.AddDbContext<FortuneCookieDbContext>(ServerConfig.Configuration);
            services.AddMySqlConnection(ServerConfig.Configuration);

            var builder = new ContainerBuilder();
            builder.Populate(services);

//            builder.AddDiscoveryService();
            builder.RegisterType<FortuneServiceWCF>();
            var container = builder.Build();

            // ensure that discovery client component starts up
            container.Resolve<IDiscoveryClient>();
            // force db opeartion so db gets created on startup
            container.Resolve<FortuneCookieDbContext>().FortuneCookies.Load();
            _containerProvider = new ContainerProvider(container);
            AutofacHostFactory.Container = container;

        }

        void Application_Error(object sender, EventArgs e)
        {
            Exception exc = Server.GetLastError();
            Console.Error.WriteLine(exc);
        }
    }
}