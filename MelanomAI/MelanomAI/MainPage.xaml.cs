using Plugin.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.IO;
using System.Net.Http;
using Plugin.Media.Abstractions;
using Newtonsoft.Json;
using MelanomAI.Model;

namespace MelanomAI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            var assembly = typeof(MainPage);
            photoImage.Source = ImageSource.FromResource("MelanomAI.Assets.Images.logo.png", assembly);
        }

        private async void cameraToolbarItem_Clicked(object sender, EventArgs e)
        {
            if (CrossMedia.Current.IsTakePhotoSupported)
            {
                var mediaOptions = new Plugin.Media.Abstractions.StoreCameraMediaOptions
                {
                    Directory = "SkinMarks",
                    Name = $"{DateTime.UtcNow}.jpg"
                };

                var file = await CrossMedia.Current.TakePhotoAsync(mediaOptions);

                if (file == null)
                {
                    await DisplayAlert("Error", "Ocurrió un error al obtener la imagen", "Ok");
                    return;
                }

                var stream = file.GetStream();
                photoImage.Source = ImageSource.FromStream(() => file.GetStream());

                await MakePredictionAsync(stream);
            }

            //var mediaOptions = new PickMediaOptions()
            //{
            //    PhotoSize = PhotoSize.Small
            //};
            //var file = await CrossMedia.Current.PickPhotoAsync(mediaOptions);

            //if (file == null)
            //{
            //    await DisplayAlert("Error", "Ocurrió un error al obtener la imagen", "Ok");
            //    return;
            //}

            //var stream = file.GetStream();
            //photoImage.Source = ImageSource.FromStream(() => file.GetStream());

            //await MakePredictionAsync(stream);
        }

        private async Task MakePredictionAsync(Stream stream)
        {
            var imageBytes = GetImageAsByteData(stream);
            var url = "https://southcentralus.api.cognitive.microsoft.com/customvision/v1.0/Prediction/5d3cd86b-b95f-497f-8f1d-d9dce96d8991/image";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", "fd63926c323344a0aacaa249ebd73fc6");

                using (var content = new ByteArrayContent(imageBytes))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    var predictions = JsonConvert.DeserializeObject<Response>(responseString);
                    resultsListView.ItemsSource = predictions.Predictions;
                }
            }
        }

        private byte[] GetImageAsByteData(Stream stream)
        {
            BinaryReader binaryReader = new BinaryReader(stream);
            return binaryReader.ReadBytes((int)stream.Length);
        }
    }
}
