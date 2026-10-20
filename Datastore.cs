using System.Text.Json;
using System.Text.Json.Serialization;

namespace JastipITS;

/// <summary>Penyimpanan data ke file JSON di folder %AppData%\JastipITS.</summary>
public class DataStore
{
    public class Wadah
    {
        public List<Pesanan> Pesanans { get; set; } = new();
        public List<MenuMakanan> Menus { get; set; } = new();
    }

    private static readonly JsonSerializerOptions Opsi = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; }
    public List<Pesanan> Pesanans { get; private set; } = new();
    public List<MenuMakanan> Menus { get; private set; } = new();

    public DataStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JastipITS");
        Directory.CreateDirectory(folder);
        FilePath = Path.Combine(folder, "data.json");
        Load();
    }

    public void Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                var w = JsonSerializer.Deserialize<Wadah>(File.ReadAllText(FilePath), Opsi);
                if (w != null)
                {
                    Pesanans = w.Pesanans;
                    Menus = w.Menus;
                    return;
                }
            }
            catch (JsonException)
            {
                File.Copy(FilePath, FilePath + ".rusak", true);
            }
        }
        SeedMenus();
        Save();
    }

    public void Save()
    {
        var tmp = FilePath + ".tmp";
        var json = JsonSerializer.Serialize(new Wadah { Pesanans = Pesanans, Menus = Menus }, Opsi);
        File.WriteAllText(tmp, json);
        File.Move(tmp, FilePath, true);
    }

    public int NextPesananId() => Pesanans.Count == 0 ? 1 : Pesanans.Max(p => p.Id) + 1;
    public int NextMenuId() => Menus.Count == 0 ? 1 : Menus.Max(m => m.Id) + 1;

    private void SeedMenus()
    {
        Menus = new List<MenuMakanan>
        {
            new() { Id = 1, Nama = "Nasi Goreng",  Kantin = "Kantin Pusat",    Harga = 12000 },
            new() { Id = 2, Nama = "Ayam Geprek",  Kantin = "Kantin Pusat",    Harga = 15000 },
            new() { Id = 3, Nama = "Mie Ayam",     Kantin = "Kantin Teknik",   Harga = 13000 },
            new() { Id = 4, Nama = "Soto Ayam",    Kantin = "Kantin Teknik",   Harga = 14000 },
            new() { Id = 5, Nama = "Es Teh",       Kantin = "Kantin Pusat",    Harga = 4000  },
            new() { Id = 6, Nama = "Kopi Susu",    Kantin = "Kantin Perpustakaan", Harga = 10000 },
        };
    }
}