using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Controls;
using InTact.Client.Models.Avaloniaed;

namespace InTact.Client.Net;

public static class Global
{
    public static ClientNode client;
    public static Thread pollThread;
    public static bool running;
    public static Window authWindow;
    public static Dictionary<ulong, ObservableCollection<AvaMessage>> AllDms = [];
}