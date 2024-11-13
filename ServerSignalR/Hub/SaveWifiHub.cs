using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetworkLocator.MVVM.Model;
using ServerSignalR.Models;

namespace ServerSignalR.Hub;

public class SaveWifiHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly ApplicationContextDb _db;

    public SaveWifiHub(ApplicationContextDb db)
    {
        _db = db;
    }

    public async Task SaveWifi(List<WiFiModel> networks)
    {
        if (networks.Count == 0 || !networks.Any())
        {
            await Clients.Caller.SendAsync("SaveWifiError", "Список сетей пуст.");
            return;
        }

        try
        {
            await _db.WifiModels.AddRangeAsync(networks);
            await _db.SaveChangesAsync();

            await Clients.Caller.SendAsync("SaveWifiSuccess");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine("Ошибка при сохранении данных в БД: " + ex.Message);
            await Clients.Caller.SendAsync("SaveWifiError", "Ошибка при сохранении данных");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("Ошибка при работе с БД: " + ex.Message);
            await Clients.Caller.SendAsync("SaveWifiError", "Ошибка при работе с БД");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Неизвестная ошибка: " + ex.Message);
            await Clients.Caller.SendAsync("SaveWifiError", "Неизвестная ошибка");
        }
    }
}