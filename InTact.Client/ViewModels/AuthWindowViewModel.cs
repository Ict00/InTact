using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using InTact.Client.Models.Avaloniaed;
using InTact.Client.Net;
using InTactCommon.Networking.Packets;
using InTactCommon.Objects;

namespace InTact.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";
    public ObservableCollection<AvaMessage> Messages { get; set; } = [];
    public ObservableCollection<AvaUser> Users { get; set; } = [];
    public ObservableCollection<ServerChatInfo> Chats { get; set; } = [];
    public ulong CurrentChatId { get; set; } = 0;
    private string inp = string.Empty;

    public string InputText
    {
        get => inp;
        set => SetProperty(ref inp, value);
    }

    public MainWindowViewModel()
    {
        Global.client = new ClientNode(12700, "127.0.0.1", "Secret", "XXX");
        Global.client.Start();
        
        Global.client._handler.Attach<MessageSentPacket>((x, y) =>
        {
            AddMessage(x.MessageSent, x.ChatId);
        });
        
        Global.client._handler.Attach<UserChangedStatus>((x, y) =>
        {
            int index = 0;
            foreach (var i in Users)
            {
                if (i.Id == x.UserId)
                {
                    index = Users.IndexOf(i);
                    break;
                }
            }

            Users[index] = AvaUser.WithStatus(Users[index], x.NewStatus);
        });
        
        while (!Global.client.Joined || Global.client.Chats.Count == 0 || Global.client.Users.Count == 0)
        {
            Global.client.Poll();
        }
        
        CurrentChatId = Global.client.Chats[0].Id;

        foreach (var chat in Global.client.Chats)
        {
            Chats.Add(new(chat.Id, chat.Name));
        }

        foreach (var usr in Global.client.Users)
        {
            Users.Add(AvaUser.FromUser(usr));
        }
        
        AddAllFromChatId(CurrentChatId);
        
        Global.running = true;
        Global.pollThread = new Thread(() =>
        {
            while (Global.running)
            {
                Global.client.Poll();
                Thread.Sleep(30);
            }
        });

        Global.pollThread.Start();
    }

    private void AddAllFromChatId(ulong id)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var chat = Global.client.Chats.Find(x => x.Id == id)!;
            Messages.Clear();
            chat.Messages.ForEach(m => Messages.Add(AvaMessage.FromMessage(m)));
        });
    }

    public void ChangeChat(ulong id)
    {
        CurrentChatId = id;
        AddAllFromChatId(CurrentChatId);
    }

    private void AddMessage(Message message, ulong id)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (id == CurrentChatId)
            {
                Messages.Add(AvaMessage.FromMessage(message));
            }
        });
    }
    
    [RelayCommand]
    public async Task Send()
    {
        List<ulong> mentions = [];
        
        Users.ToList().ForEach(x =>
        {
            if (InputText.Contains($"@{x.Name}"))
            {
                mentions.Add(x.Id);
            }
        });
        
        new MessageSentPacket()
        {
            MessageSent = new Message()
            {
                AuthorId = Global.client.SelfId,
                Timestamp = DateTime.Now.ToBinary(),
                Content = inp,
                Id = 0,
                Mentions = mentions
            },
            ChatId = CurrentChatId
        }.SendPacketTo(Global.client.Server!, Global.client._writer);
        InputText = string.Empty;
    }
}