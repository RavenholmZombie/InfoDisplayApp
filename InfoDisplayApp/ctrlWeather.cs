using InfoDisplayApp.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InfoDisplayApp
{
    public partial class ctrlWeather : UserControl
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly System.Windows.Forms.Timer _weatherTimer;

        private bool _isUpdating = false;

        // Princeton, Maine
        private const double Latitude = 45.143109;
        private const double Longitude = -67.526589;

        public ctrlWeather()
        {
            InitializeComponent();

            // Refresh weather every 15 minutes
            _weatherTimer = new System.Windows.Forms.Timer
            {
                Interval = 15 * 60 * 1000
            };

            _weatherTimer.Tick += WeatherTimer_Tick;

            Load += ctrlWeather_Load;
            Disposed += ctrlWeather_Disposed;
        }

        private async void ctrlWeather_Load(object? sender, EventArgs e)
        {
            // Load immediately when the widget appears
            await UpdateWeatherAsync();

            _weatherTimer.Start();
        }

        private async void WeatherTimer_Tick(object? sender, EventArgs e)
        {
            await UpdateWeatherAsync();
        }

        private async Task UpdateWeatherAsync()
        {
            // Prevent two requests from running at the same time
            if (_isUpdating)
                return;

            _isUpdating = true;
            pBoxBtnRefresh.Enabled = false;
            pBoxBtnRefresh.Image = Resources.refresh_disabled;

            try
            {
                string url =
                    $"https://api.open-meteo.com/v1/forecast" +
                    $"?latitude={Latitude}" +
                    $"&longitude={Longitude}" +
                    $"&current=temperature_2m,weather_code,is_day" +
                    $"&temperature_unit=fahrenheit" +
                    $"&timezone=America%2FNew_York";

                string json = await _httpClient.GetStringAsync(url);

                using JsonDocument document = JsonDocument.Parse(json);

                JsonElement current =
                    document.RootElement.GetProperty("current");

                double temperature =
                    current.GetProperty("temperature_2m").GetDouble();

                int weatherCode =
                    current.GetProperty("weather_code").GetInt32();

                bool isDay =
                    current.GetProperty("is_day").GetInt32() == 1;

                // Update text
                lblTown.Text = "Princeton, ME";

                lblTemperature.Text =
                    $"{Math.Round(temperature):0}°F";

                lblCurrentCondition.Text =
                    GetWeatherDescription(weatherCode);

                // Update icon
                SetWeatherIcon(weatherCode, isDay);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Weather update failed: {ex}"
                );

                lblCurrentCondition.Text = "Weather unavailable";
            }
            finally
            {
                _isUpdating = false;
                pBoxBtnRefresh.Enabled = true;
                pBoxBtnRefresh.Image = Resources.refresh_norm;
            }
        }

        private static string GetWeatherDescription(int code)
        {
            return code switch
            {
                0 => "Clear",
                1 => "Mostly Clear",
                2 => "Partly Cloudy",
                3 => "Cloudy",

                45 or 48 => "Foggy",

                51 => "Light Drizzle",
                53 => "Drizzle",
                55 => "Heavy Drizzle",

                56 or 57 => "Freezing Drizzle",

                61 => "Light Rain",
                63 => "Rain",
                65 => "Heavy Rain",

                66 or 67 => "Freezing Rain",

                71 => "Light Snow",
                73 => "Snow",
                75 => "Heavy Snow",

                77 => "Snow Grains",

                80 => "Light Showers",
                81 => "Showers",
                82 => "Heavy Showers",

                85 => "Snow Showers",
                86 => "Heavy Snow Showers",

                95 => "Thunderstorms",
                96 or 99 => "Severe Thunderstorms",

                _ => "Unknown"
            };
        }

        private void SetWeatherIcon(int code, bool isDay)
        {
            pBoxWeatherIcon.Image = code switch
            {
                0 => Properties.Resources.weather_sunny,

                1 => Properties.Resources.weather_partlycloudy,
                2 => Properties.Resources.weather_mostlycloudy,
                3 => Properties.Resources.weather_overcast,

                45 or 48 =>
                    Properties.Resources.weather_fog,

                51 or 61 =>
                    Properties.Resources.weather_lightrain,

                53 or 55 or 80 =>
                    Properties.Resources.weather_lightshowers,

                56 or 57 or 66 or 67 =>
                    Properties.Resources.weather_hail,

                63 =>
                    Properties.Resources.weather_rain,

                65 or 81 or 82 =>
                    Properties.Resources.weather_showers,

                71 or 77 or 85 =>
                    Properties.Resources.weather_lightsnow,

                73 or 75 or 86 =>
                    Properties.Resources.weather_snow,

                95 =>
                    Properties.Resources.weather_thunder,

                96 or 99 =>
                    Properties.Resources.weather_thunderstorm,

                _ =>
                    Properties.Resources.weather_cloudy
            };
        }

        private void ctrlWeather_Disposed(object? sender, EventArgs e)
        {
            _weatherTimer.Stop();
            _weatherTimer.Dispose();
        }

        private void pBoxBtnRefresh_MouseEnter(object sender, EventArgs e)
        {
            pBoxBtnRefresh.Image = Resources.refresh_highlight;
        }

        private async void pBoxBtnRefresh_MouseClick(object sender, MouseEventArgs e)
        {
            pBoxBtnRefresh.Image = Resources.refresh_click;
            await UpdateWeatherAsync();
            pBoxBtnRefresh.Image = Resources.refresh_norm;
        }

        private void pBoxBtnRefresh_MouseLeave(object sender, EventArgs e)
        {
            pBoxBtnRefresh.Image = Resources.refresh_norm;
        }
    }
}