//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AppContainer.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2024 Starion Group S.A.
//
//     This file is part of CDP4-COMET Batch Editor.
//     The CDP4-COMET Batch Editor is a commandline application to perform batch operations on a
//     ECSS-E-TM-10-25 Annex A and Annex C data source
//
//     The CDP4-COMET Batch Editor is free software; you can redistribute it and/or
//     modify it under the terms of the GNU Lesser General Public
//     License as published by the Free Software Foundation; either
//     version 3 of the License, or any later version.
//
//     The CDP4-COMET Batch Editor is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//     GNU Lesser General License version 3 for more details.
//
//     You should have received a copy of the GNU Lesser General License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace CDPBatchEditor
{
    using Autofac;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands;
    using CDPBatchEditor.Commands.Command;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Commands.Interface;
    using CDPBatchEditor.Resources;
    using CDPBatchEditor.Services;
    using CDPBatchEditor.Services.Interfaces;

    using CDP4Dal;

    /// <summary>
    /// Provides a <see cref="IContainer" />
    /// </summary>
    public static class AppContainer
    {
        /// <summary>
        /// The IoC container
        /// </summary>
        public static IContainer Container { get; set; }

        /// <summary>
        /// Builds the container and register
        /// </summary>
        /// <param name="commandArguments">Command arguments.</param>
        public static void BuildContainer(ICommandArguments commandArguments)
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterType<App>().As<IApp>().SingleInstance();
            containerBuilder.RegisterType<ResourceLoader>().As<IResourceLoader>();
            containerBuilder.RegisterType<SessionService>().As<ISessionService>().SingleInstance();
            containerBuilder.RegisterType<FilterService>().As<IFilterService>().SingleInstance();
            containerBuilder.RegisterInstance(commandArguments).As<ICommandArguments>().SingleInstance();
            containerBuilder.RegisterType<CommandDispatcher>().As<ICommandDispatcher>();
            containerBuilder.RegisterType<DomainCommand>().As<IDomainCommand>();
            containerBuilder.RegisterType<ParameterCommand>().As<IParameterCommand>();
            containerBuilder.RegisterType<RequirementSimpleParameterValueCommand>().As<IRequirementSimpleParameterValueCommand>();
            containerBuilder.RegisterType<StateCommand>().As<IStateCommand>();
            containerBuilder.RegisterType<SubscriptionCommand>().As<ISubscriptionCommand>();
            containerBuilder.RegisterType<OverrideCommand>().As<IOverrideCommand>();
            containerBuilder.RegisterType<ValueSetCommand>().As<IValueSetCommand>();
            containerBuilder.RegisterType<ScaleCommand>().As<IScaleCommand>();
            containerBuilder.RegisterType<OptionCommand>().As<IOptionCommand>();
            containerBuilder.RegisterType<SyncCommand>().As<ISyncCommand>();
            containerBuilder.RegisterType<ElementUsageCommand>().As<IElementUsageCommand>();
            containerBuilder.RegisterType<ReportGenerator>().As<IReportGenerator>();
            containerBuilder.RegisterType<CDPMessageBus>().As<ICDPMessageBus>().SingleInstance();

            Container = containerBuilder.Build();
        }
    }
}
