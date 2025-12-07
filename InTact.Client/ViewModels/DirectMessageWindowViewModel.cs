using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using InTact.Client.Models.Avaloniaed;
using InTact.Client.Net;
using InTactCommon.Networking.Packets;
using InTactCommon.Objects;

namespace InTact.Client.ViewModels;

public partial class DirectMessageWindowViewModel : ViewModelBase
{
    public ObservableCollection<AvaMessage> Messages { get; } = [];

    public AvaUser To { get; set; } = new AvaUser()
    {
        StatusColor = Brushes.Green,
        Name = "Test",
        Avatar = Brushes.Aquamarine,
    };
    
    private string _input;

    public string Input
    {
        get =>  _input;
        set => SetProperty(ref _input, value);
    }

    public DirectMessageWindowViewModel()
    {
        
    }

    public DirectMessageWindowViewModel(AvaUser to, ObservableCollection<AvaMessage> messages)
    {
        To = to;
        Messages = messages;
    }

    [RelayCommand]
    public async void Send()
    {
        if (_input.Replace(" ", "").Replace("\t", "").Replace("\n", "") == string.Empty) return;
        
        new MessageSentDm()
        {
            Message = new Message()
            {
                AuthorId = Global.client.SelfId,
                Timestamp = DateTime.Now.ToBinary(),
                Content = _input,
                Id = 0,
                Mentions = []
            },
            FromUser = Global.client.SelfId,
            ToUser = To.Id,
        }.SendPacketTo(Global.client.Server!, Global.client._writer);
        Input = string.Empty;
        Console.WriteLine("Sent!");
    }
}