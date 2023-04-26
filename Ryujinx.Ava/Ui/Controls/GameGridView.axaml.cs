using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ui.App.Common;
using System;

namespace Ryujinx.Ava.UI.Controls
{
    public partial class GameGridView : UserControl
    {
        public static readonly RoutedEvent<ApplicationOpenedEventArgs> ApplicationOpenedEvent =
            RoutedEvent.Register<GameGridView, ApplicationOpenedEventArgs>(nameof(ApplicationOpened), RoutingStrategies.Bubble);

        public event EventHandler<ApplicationOpenedEventArgs> ApplicationOpened
        {
            add    { AddHandler(ApplicationOpenedEvent,    value); }
            remove { RemoveHandler(ApplicationOpenedEvent, value); }
        }

        public GameGridView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void GameList_DoubleTapped(object sender, RoutedEventArgs args)
        {
            if (sender is ListBox listBox)
            {
                if (listBox.SelectedItem is ApplicationData selected)
                {
                    RaiseEvent(new ApplicationOpenedEventArgs(selected, ApplicationOpenedEvent));
                }
            }
        }

        public void GameList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (sender is ListBox listBox)
            {
                (DataContext as MainWindowViewModel).GridSelectedApplication = listBox.SelectedItem as ApplicationData;
            }
        }

        private void SearchBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            (DataContext as MainWindowViewModel).SearchText = (sender as TextBox).Text;
        }
    }
}
