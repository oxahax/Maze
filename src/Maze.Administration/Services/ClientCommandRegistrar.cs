﻿using System;
using Autofac;
using Orcus.Administration.Library.Clients;
using Orcus.Administration.Library.Extensions;
using Orcus.Administration.Library.Menu.MenuBase;
using Orcus.Administration.Library.Menus;
using Orcus.Administration.Library.Models;
using Orcus.Administration.Library.Services;
using Orcus.Administration.Library.Views;
using Prism.Commands;
using Unclassified.TxLib;

namespace Orcus.Administration.Services
{
    public class ClientCommandRegistrar : IClientCommandRegistrar
    {
        private readonly ClientsContextMenu _clientsContextMenu;
        private readonly IOrcusRestClient _orcusRestClient;
        private readonly IWindowService _windowService;

        public ClientCommandRegistrar(ClientsContextMenu clientsContextMenu, IWindowService windowService,
            IOrcusRestClient orcusRestClient)
        {
            _clientsContextMenu = clientsContextMenu;
            _windowService = windowService;
            _orcusRestClient = orcusRestClient;
        }

        public void Register<TViewModel>(string txLibResource, IIconFactory iconFactory, CommandCategory category)
        {
            GetNavigationEntry(category).Add(new ItemCommand<ClientViewModel>
            {
                Header = Tx.T(txLibResource),
                Icon = iconFactory.Create(),
                Command = new DelegateCommand<ClientViewModel>(model =>
                {
                    _windowService.Show(typeof(TViewModel), builder =>
                    {
                        builder.RegisterInstance(model);
                        builder.Register(_ => _orcusRestClient.CreateTargeted(model.ClientId))
                            .SingleInstance();
                    }, window => window.TitleBarIcon = iconFactory.Create(), null, out _);
                })
            });
        }

        private NavigationalEntry<ItemCommand<ClientViewModel>> GetNavigationEntry(CommandCategory category)
        {
            switch (category)
            {
                case CommandCategory.Interaction:
                    return _clientsContextMenu.InteractionCommands;
                case CommandCategory.System:
                    return _clientsContextMenu.SystemCommands;
                case CommandCategory.Surveillance:
                    return _clientsContextMenu.SurveillanceCommands;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}