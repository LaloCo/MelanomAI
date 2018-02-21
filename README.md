## 1. Create a new Custom Vision Project
The main part of this application is, of course, the Custom Vision AI, which is the first thing you should set up. You have to go to [customvision.ai](https://www.customvision.ai/) to sign in with a Microsoft account, after which you will be able to create a new project.

This will be very easy to setup, you don't even need to understand what Artificial Intelligence is, let alone how it works or what techniques the tool will use to be trained and improved, all you need to do is add at least two tags (for this example lets say benign and malignant) and add photos to each tag. It really is as simple as uploading images and tagging them as one or another.

## 2. Get the data set ready
In my case, after some research, I discovered the International Skin Imaging Collaboration archive, which contains a ton of images of benign, malignant and unclassified skin marks. This makes for a perfect dataset. Like I mentioned, all I had to do was upload images. I did start with the images marked as malignant, and since the tool is limited to 1000 images, I only uploaded about 350 images. **Do not upload all the images at once**, it will make it harder (in a case like this impossible) to classify later. Instead, like I mentioned, upload first one type of images, once uploaded, classify them, and then upload the other kind of images. There is a helpful "untagged" filter to the left that lets you filter by those elements that haven't been tagged, so you can select them and tag them.

## 3. Train the AI
Once you have at least two tags, with at least five images each, it is time to train the AI, and while this sounds scary to anyone not familiar with AI, in this case, all you have to do is press that big green Train button at the top of the website, and that is it. After a few minutes, the AI will be ready. You will be able to navigate to the performance tab and check the Precision and Recall of your traineBy the way, in here you could have multiple iterations, as you get more data, and it becomes more reliable, you should keep retraining the AI so it gets more and more accurate. But really, once the AI has been trained for the first time, it is ready to be used on your Xamarin apps. In this performance tab you will see the prediction URL button, which will give you two URLs, once in case you have a file (which will be the case in our Xamarin app), and one in case you have the URL of an image that is in the cloud. I will be demonstrating how to use the first scenario with a Xamarin Forms app that allows the user to take pictures and send that picture to this API.

## 4. The Xamarin Forms App
All the code for the Xamarin Forms app can be found in this GitHub repo.

The Xamarin code is fairly straightforward, I have a Page with an Image and a ListView, the image will display the picture that is taken, and the list the results received from the endpoint. I have a couple of classes, defining the objects to which the JSON with the responses will be deserialized to:

    public class Prediction
    {
        public string TagId { get; set; }
        public string Tag { get; set; }
        public double Probability { get; set; }
    }

    public class Response
    {
        public string Id { get; set; }
        public string Project { get; set; }
        public string Iteration { get; set; }
        public DateTime Created { get; set; }
        public IList<Prediction> Predictions { get; set; }
    }
  
Then the method that is the event handler for the click of a button, the cue to take a picture. Here I simply use James [Montemagno's Media Plugin](https://github.com/jamesmontemagno/MediaPlugin). Notice here the stream of the file that holds the picture is passed to a MakePredictionsAsync method:

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
            photoImage.Source = ImageSource.FromStream(() => stream);

            await MakePredictionAsync(stream);
        }
    }   

It is in that MakePredictionsAsync method where the interesting stuff happens. I use the URL provided by the service, set an HTTP client the way the service requests (with a Prediction-Key header, a certain media type header), and pass the stream that has previously been transformed into a byte array and wait for the response.

That response is then deserialized into instances of the classes mentioned above and used as the ItemsSource for the ListView.

BTW, I also use a helper method to get the image as byte data using the BinaryReader class:

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
