﻿using System;
using System.Windows;
using System.Windows.Threading;
using Orcus.Administration.Library.Views;

// ReSharper disable once CheckNamespace
namespace FileExplorer.Administration.Views
{
    /// <summary>
    ///     Interaction logic for InputTextView.xaml
    /// </summary>
    public partial class InputTextView
    {
        public InputTextView(IWindowViewManager viewManager) : base(viewManager)
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                NameTextBox.Focus();
                NameTextBox.SelectAll();
            }));
        }
    }
}