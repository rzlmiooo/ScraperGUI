using System.Net.Http;
using System.Windows;

namespace ScraperGUI;
public partial class MainWindow : Window
{
    private readonly HttpClient _client = new HttpClient();
    private CancellationTokenSource? _cts; // Remote control untuk Stop
    private bool _isRunning = false; // Status saklar

    // Class kecil untuk menyimpan data tiap baris
    private class ScrapeResult
    {
        public string Time { get; set; } = "";
        public string Status { get; set; } = "";
        public string RawData { get; set; } = "";
        
        // Format teks yang akan muncul di UI (1 baris saja)
        public override string ToString()
        {
            var preview = RawData.Length > 40 ? RawData.Substring(0, 40) + "..." : RawData;
            return $"[{Time}] {Status} - {preview}";
        }
    }

    public MainWindow()
    {
        InitializeComponent();
    }

    // Aksi ketika saklar START/STOP diklik
    private async void BtnToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            // PROSES STOP
            _cts?.Cancel(); // Tarik pelatuk pembatalan
            _isRunning = false;
            
            BtnToggle.Content = "START";
            BtnToggle.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Hijau
            InputUrl.IsEnabled = true;
            InputHeader.IsEnabled = true;
        }
        else
        {
            // PROSES START
            _isRunning = true;
            _cts = new CancellationTokenSource(); // Buat remote control baru
            
            BtnToggle.Content = "STOP";
            BtnToggle.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Merah
            InputUrl.IsEnabled = false; // Kunci input saat jalan
            InputHeader.IsEnabled = false;

            // Jalankan looping di background tanpa bikin UI macet
            await RunScraperAsync(_cts.Token);
        }
    }

    // Mesin Scraper
    private async Task RunScraperAsync(CancellationToken token)
    {
        string url = InputUrl.Text;
        string headerInput = InputHeader.Text;

        // Reset dan set Header custom (mirip Postman)
        _client.DefaultRequestHeaders.Clear();
        if (headerInput.Contains(":"))
        {
            var parts = headerInput.Split(':', 2);
            _client.DefaultRequestHeaders.Add(parts[0].Trim(), parts[1].Trim());
        }

        while (!token.IsCancellationRequested)
        {
            var resultData = new ScrapeResult { Time = DateTime.Now.ToString("HH:mm:ss") };

            try
            {
                var response = await _client.GetAsync(url, token);
                resultData.Status = response.StatusCode.ToString();
                resultData.RawData = await response.Content.ReadAsStringAsync(token);
            }
            catch (TaskCanceledException)
            {
                // Terjadi saat user menekan STOP, abaikan saja
                break;
            }
            catch (Exception ex)
            {
                resultData.Status = "ERROR";
                resultData.RawData = ex.Message;
            }

            // Tambahkan hasil ke UI (List Box)
            ListResults.Items.Add(resultData);
            
            // Auto-scroll ke bawah
            ListResults.ScrollIntoView(resultData);

            try
            {
                // Jeda 5 detik sebelum request lagi
                await Task.Delay(5000, token);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    // Aksi double click untuk lihat detail
    private void ListResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ListResults.SelectedItem is ScrapeResult selected)
        {
            // Tampilkan pop-up berisi JSON penuh
            MessageBox.Show(selected.RawData, "Detail Data", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // Aksi tombol Exit
    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    // Aksi tombol Minimize
    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        // Fitur sakti WPF untuk melipat jendela ke Taskbar Windows
        this.WindowState = WindowState.Minimized;
    }

    // Fungsi ajaib untuk membuat jendela bisa digeser
    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Pastikan yang ditekan adalah klik kiri
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            this.DragMove(); // Bawaan WPF, otomatis mengurus kalkulasi koordinat mouse vs layar!
        }
    }
}