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
        Global.client._handler.Attach<MessageSentPacket>((x, y) =>
        {
            AddMessage(x.MessageSent, x.ChatId);
        });

        Global.client._handler.Attach<MessageSentDm>((x, y) =>
        {
            if (x.FromUser == Global.client.SelfId)
            {
                if (Global.AllDms.TryGetValue(x.ToUser, out var _dms))
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        _dms.Add(AvaMessage.FromMessage(x.Message));
                    });
                    return;
                }
            }
            
            if (Global.AllDms.TryGetValue(x.FromUser, out var dms))
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    dms.Add(AvaMessage.FromMessage(x.Message));
                });
            }
        });
        
        Global.client._handler.Attach<UserAdded>((x, y) =>
        {
            AddUser(x.User);
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

    private void AddUser(User user)
    {
        Dispatcher.UIThread.Invoke(() => { Users.Add(AvaUser.FromUser(user)); });
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
        if (inp.Replace(" ", "").Replace("\t", "").Replace("\n", "") == string.Empty) return;
        
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