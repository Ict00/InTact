using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Threading;
using InTact.Client.Models.Avaloniaed;
using InTact.Client.Net;
using InTact.Client.ViewModels;
using InTactCommon.Objects;

namespace InTact.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Global.running = false;
        Global.client._client.Stop();
        Global.authWindow.Close();
    }

    private void OtherChatSelected(object? sender, SelectionChangedEventArgs e)
    {
        var a = e.AddedItems[0] as ServerChatInfo;
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ChangeChat(a.Id);
        }
    }

    private void UserSelected(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            var a = e.AddedItems[0] as AvaUser;
            ObservableCollection<AvaMessage> msgs = [];

            if (Global.AllDms.TryGetValue(a.Id, out var messages))
            {
                Dispatcher.UIThread.Invoke(() => { msgs = messages!; });
            }
            else
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    Global.client.DmMessages.TryAdd(a.Id, []);

                    foreach (var i in Global.client.DmMessages[a.Id])
                    {
                        msgs.Add(AvaMessage.FromMessage(i));
                    }

                    Global.AllDms[a.Id] = msgs;
                });
            }

            var dmWindow = new DirectMessageWindow()
            {
                DataContext = new DirectMessageWindowViewModel(a, msgs)
            };

            dmWindow.Show();
        }
        catch (Exception ex) { }
    }
}