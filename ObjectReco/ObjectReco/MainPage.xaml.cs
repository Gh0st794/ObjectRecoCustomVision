using Newtonsoft.Json;
using Plugin.Media.Abstractions;
using System;
using System.Linq;
using System.Net.Http;
using Xamarin.Forms;

namespace ObjectReco
{
    public partial class MainPage : ContentPage
    {
        public const string ServiceApiUrl = "custom vision service url";
        public const string ApiKey = "api key";


        private MediaFile _foto = null;        

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ElegirImage(object sender, EventArgs e)
        {
            await Plugin.Media.CrossMedia.Current.Initialize();

            _foto = await Plugin.Media.CrossMedia.Current.PickPhotoAsync(new PickMediaOptions());
            Img.Source = ImageSource.FromFile(_foto.Path);
        }

        private async void TomarFoto(object sender, EventArgs e)
        {
            await Plugin.Media.CrossMedia.Current.Initialize();

            _foto = await Plugin.Media.CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
            {
                DefaultCamera = Plugin.Media.Abstractions.CameraDevice.Rear,
                Directory = "Vision",
                Name = "Target.jpg"
            });

            if (_foto == null)
            {
                return;
            }

            Img.Source = ImageSource.FromFile(_foto.Path);
            /*
            Img.Source = ImageSource.FromStream(() =>
            {
                var stream = _foto.GetStream();
                return stream;
            });*/
        }

        private async void Clasificar(object sender, EventArgs e)
        {
            using (Acr.UserDialogs.UserDialogs.Instance.Loading("Clasificando..."))
            {
                if (_foto == null) return;

                var stream = _foto.GetStream();

                var httpClient = new HttpClient();
                var url = ServiceApiUrl;
                httpClient.DefaultRequestHeaders.Add("Prediction-Key", ApiKey);

                var content = new StreamContent(stream);

                var response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    Acr.UserDialogs.UserDialogs.Instance.Toast("Hubo un error en la deteccion...");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();

                var c = JsonConvert.DeserializeObject<ClasificationResponse>(json);

                var p = c.Predictions.FirstOrDefault();
                if (p == null)
                {
                    Acr.UserDialogs.UserDialogs.Instance.Toast("Imagen no reconocida.");
                    return;
                }
                //ResponseLabel.Text = $"{p.Tag} - {p.Probability:p0}";
                ResponseLabel.Text = p.Tag + "-" + p.Probability;
                Accuracy.Progress = p.Probability;
            }

            Acr.UserDialogs.UserDialogs.Instance.Toast("Clasificacion terminada...");
        }
    }


    public class ClasificationResponse
    {
        public string Id { get; set; }
        public string Project { get; set; }
        public string Iteration { get; set; }
        public DateTime Created { get; set; }
        public Prediction[] Predictions { get; set; }
    }

    public class Prediction
    {
        public string TagId { get; set; }
        public string Tag { get; set; }
        public float Probability { get; set; }
    }

}