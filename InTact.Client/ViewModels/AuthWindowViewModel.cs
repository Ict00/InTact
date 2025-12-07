using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using InTact.Client.Models.Avaloniaed;
using InTact.Client.Net;
using InTact.Client.Views;
using InTactCommon.Networking.Packets;
using InTactCommon.Objects;

namespace InTact.Client.ViewModels;



public partial class AuthWindowViewModel : ViewModelBase
{
    private JoinState _authing = JoinState.NotAuthing;

    public JoinState AuthingNow
    {
        get => _authing;
        set
        {
            SetProperty(ref _authing, value);
            OnPropertyChanged(nameof(AuthProcess));
            OnPropertyChanged(nameof(AuthWindow));
            OnPropertyChanged(nameof(Introducing));
        }
    }
    
    public bool AuthWindow => AuthingNow == JoinState.NotAuthing;
    public bool AuthProcess => AuthingNow == JoinState.Authing;
    public bool Introducing => AuthingNow == JoinState.Introducing;
    
    public string Token { get; set; } = "";
    public string Address { get; set; } = "";

    private string inp = "";

    public string IntroEnteredColor
    {
        get => inp;
        set
        {
            SetProperty(ref inp, value);
            OnPropertyChanged(nameof(ColorBrush));
        }
    }
    public string IntroEnteredUsername { get; set; } = "";
    private IBrush brush;

    public IBrush ColorBrush
    {
        get
        {
            try
            {
                var a = new SolidColorBrush(Color.Parse(IntroEnteredColor));

                return a;
            }
            catch (Exception ex)
            {
                return Brushes.Black;
            }
        }
    }

    [RelayCommand]
    public async Task Login()
    {
        AuthingNow = JoinState.Authing;
        if (!Address.Contains(':'))
        {
            AuthingNow = JoinState.NotAuthing;
            return;
        }
        string ip = Address.Split(':')[0];
        if (int.TryParse(Address.Split(':')[1], out int port))
        {
            await ActuallyLogin(ip, port, Token);
        }
        else
        {
            AuthingNow = JoinState.NotAuthing;
        }
    }

    [RelayCommand]
    public async Task IntroduceAndLogin()
    {
        SColor sColor = new()
        {
            R = 0,
            G = 0,
            B = 0
        };
        
        if (Color.TryParse(IntroEnteredColor, out Color colorForBrush))
        {
            sColor.R = colorForBrush.R;
            sColor.G = colorForBrush.G;
            sColor.B = colorForBrush.B;
        }

        new IntroductionUser()
        {
            Username = IntroEnteredUsername,
            PfpColor = sColor,
            Token = Token
        }.SendPacketTo(Global.client.Server!, Global.client._writer);

        bool stillTry = true;
        
        Global.client._handler.Attach<JoinServerResponse>((x, y) =>
        {
            if (x.Status == JoinStatus.Joined)
            {
                stillTry = false;
                AuthingNow = JoinState.Authing;
            }
            else
            {
                stillTry = false;
                AuthingNow = JoinState.NotAuthing;
            }
        });
        
        Global.client.WaitTillLoaded(() => Global.client._client.IsRunning);

        if (AuthingNow == JoinState.NotAuthing)
        {
            IntroEnteredColor = "";
            IntroEnteredUsername = "";
        }
        else
        {
            Global.authWindow.Hide();

            new MainWindow()
            {
                DataContext = new MainWindowViewModel()
            }.Show();
        }
    }

    private async Task ActuallyLogin(string address, int port, string token)
    {
        Global.client = new ClientNode(port, address, "Secret", token);
        try
        {
            bool stillTry = true;
            
            Global.client._handler.Attach<JoinServerResponse>((x, y) =>
            {
                if (x.Status == JoinStatus.IntroduceYourself)
                {
                    stillTry = false;
                    AuthingNow = JoinState.Introducing;
                }
                else if (x.Status != JoinStatus.Joined)
                {
                    stillTry = false;
                    AuthingNow = JoinState.NotAuthing;
                }
            });
            
            Global.client.Start();
            
            Global.client.WaitTillLoaded(() => stillTry);

            if (stillTry)
            {
                Global.authWindow.Hide();

                new MainWindow()
                {
                    DataContext = new MainWindowViewModel()
                }.Show();
                AuthingNow = JoinState.NotAuthing;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            AuthingNow = JoinState.NotAuthing;
        }
    }
}


public enum JoinState
{
    NotAuthing,
    Authing,
    Introducing
}