using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Windows.Devices.WiFi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using NetworkLocator.MVVM.Model;

namespace NetworkLocator.MVVM.ViewModel;

public class ScanViewModel : ObservableObject
{
    private HubConnection _hubConnection;
    
    private WiFiModel _bestNetwork;
    private bool _isScanning;
    private bool _scanningSuccessful;
    private ObservableCollection<WiFiModel> _networks;

    public bool IsScanning
    {
        get => _isScanning;
        private set => SetProperty(ref _isScanning, value);
    }
    public bool ScanningSuccessful
    {
        get => _scanningSuccessful;
        private set => SetProperty(ref _scanningSuccessful, value);
    }
    public ObservableCollection<WiFiModel> Networks
    {
        get => _networks;
        private set => SetProperty(ref _networks, value);
    }
    public WiFiModel BestNetwork
    {
        get => _bestNetwork;
        private set => SetProperty(ref _bestNetwork, value);
    }
    
    public AsyncRelayCommand ScanCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }

    public ScanViewModel()
    {
        Networks = [];
        ScanCommand = new AsyncRelayCommand(ScanForNetworks);
        SaveCommand = new AsyncRelayCommand(SaveNetwork);
    }

    private async Task ScanForNetworks()
    {
        try
        {
            IsScanning = true;
            ScanningSuccessful = false;

            var access = await WiFiAdapter.RequestAccessAsync();

            if (access != WiFiAccessStatus.Allowed)
            {
                MessageBox.Show("Доступ к WiFi запрещен.", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var adapters = await WiFiAdapter.FindAllAdaptersAsync();
            if (adapters.Count == 0)
            {
                MessageBox.Show("Не удалось найти WiFi адаптер.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var adapter = adapters[0];
            await adapter.ScanAsync();

            var networksList = adapter.NetworkReport.AvailableNetworks.Select(network => new WiFiModel
            {
                Ssid = network.Ssid,
                SignalBars = network.SignalBars
            }).ToList();

            Networks.Clear();
            foreach (var network in networksList)
            {
                Networks.Add(network);
            }

            BestNetwork = Networks.OrderByDescending(x => x.SignalBars).FirstOrDefault();
            ScanningSuccessful = true;
        }
        catch (COMException ex)
        {
            MessageBox.Show($"Ошибка при работе с WiFi адаптером {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsScanning = false;
        }
    }
    private async Task ConnectToSignalR()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder().WithUrl($"https://localhost:7284/saveWifi").WithAutomaticReconnect(new[]
            {
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
            }).Build();
            
            _hubConnection.On("SaveWifiSuccess", () => {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    MessageBox.Show("Успешное сохранения данных", "Сохранение");
                });
            });
            _hubConnection.On<string>("SaveWifiError", result => {
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    MessageBox.Show(result, "Сохранение");
                });
            });
            
            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("SaveWifi", Networks.ToList());
            Console.WriteLine("Connected successfully.");
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show("Failed to connect to the server. Please check your network.", "Server");
            Console.WriteLine($"HttpRequestException: {ex.Message}");
        }
        catch (SocketException ex)
        {
            MessageBox.Show("Network error occurred while connecting to the server.", "Network");
            Console.WriteLine($"SocketException: {ex.Message}");
        }
        catch (HubException ex)
        {
            MessageBox.Show("Error occurred with the SignalR hub connection.", "Server");
            Console.WriteLine($"HubException: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show( "An error occurred in the application. Please try again.", "Error");
            Console.WriteLine($"InvalidOperationException: {ex.Message}");
        }
        catch (TimeoutException ex)
        {
            MessageBox.Show("Connection to the server timed out. Please try again.", "Timeout");
            Console.WriteLine($"TimeoutException: {ex.Message}");
        }
        catch (Exception ex)
        {
            MessageBox.Show("An unexpected error occurred.", "Error");
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
    private async Task SaveNetwork()
    {
        await ConnectToSignalR();
    }
}