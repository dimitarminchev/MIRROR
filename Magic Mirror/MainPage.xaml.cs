using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Magic_Mirror
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Private Part
        private List<LandSlides> landslides; // LandSlides Collection       
        private const string file = "Grammar\\grammar.xml"; // Grammer File                                                                
        private SpeechRecognizer recognizer;  // Speech Recognizer

        // Constructor
        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;

            // Initialize Recognizer
            InitializeSpeechRecognizer();
        }

        // Load
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Map Token
            Map.MapServiceToken = "daFWvXsEwa449YzR1o4A~4VlbsCKuphWjH4cbtY2S8A~Anqk8FXdiU-KYJyh8OEyO3r9IivuATJbFAWDPFtTX2RXofbbadXBqGgRjdNcWoyV";
            Map.LandmarksVisible = true;

            // Available Voice Commands
            List<String> voice = new List<string>();
            voice.Add("Show Map");
            voice.Add("Show Location");
            voice.Add("Show LandSlides");
            voice.Add("Zoom In");
            voice.Add("Zoom Out");
            voice.Add("Radio Rock");
            voice.Add("Radio Classic");
            voice.Add("Radio Stop");
            Commands.ItemsSource = voice;

            // Welcome Text
            MessageShow("Welcome");
        }

        // Unload
        private async void MainPage_Unloaded(object sender, object args)
        {
            // Stop recognizing
            await recognizer.ContinuousRecognitionSession.StopAsync();
            recognizer.Dispose();
            recognizer = null;
        }

      
        // Initialize Speech Recognizer and start async recognition
        private async void InitializeSpeechRecognizer()
        {
            // Initialize recognizer
            recognizer = new SpeechRecognizer();

            // Set event handlers
            recognizer.StateChanged += RecognizerStateChanged;
            recognizer.ContinuousRecognitionSession.ResultGenerated += RecognizerResultGenerated;

            // Load Grammer file constraint
            string fileName = String.Format(file);
            StorageFile grammarContentFile = await Package.Current.InstalledLocation.GetFileAsync(fileName);

            SpeechRecognitionGrammarFileConstraint grammarConstraint = new SpeechRecognitionGrammarFileConstraint(grammarContentFile);

            // Add to grammer constraint
            recognizer.Constraints.Add(grammarConstraint);

            // Compile grammer
            SpeechRecognitionCompilationResult compilationResult = await recognizer.CompileConstraintsAsync();

            Debug.WriteLine("Status: " + compilationResult.Status.ToString());

            // If successful, display the recognition result.
            if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
            {
                Debug.WriteLine("Result: " + compilationResult.ToString());

                await recognizer.ContinuousRecognitionSession.StartAsync();
            }
            else
            {
                Debug.WriteLine("Status: " + compilationResult.Status);
            }
        }

        // Recognizer state changed
        private void RecognizerStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine("Speech recognizer state: " + args.State.ToString());
        }

        // Recognizer generated results
        private void RecognizerResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // Output debug strings
            Debug.WriteLine(args.Result.Status);
            Debug.WriteLine(args.Result.Text);
            //int count = args.Result.SemanticInterpretation.Properties.Count;
            //Debug.WriteLine("Count: " + count);
            Debug.WriteLine("Tag: " + args.Result.Constraint.Tag);

            // Process Commands
            if (args.Result.Text.ToLower().Contains("zoom")) ZoomCommands(args.Result.Text.ToLower());
            else if (args.Result.Text.ToLower().Contains("radio")) RadioCommands(args.Result.Text.ToLower());
            else MainCommands(args.Result.Text.ToLower());
        }

        // Main 
        private async void MainCommands(string commands)
        {
            // Map
            if (commands.ToLower().Contains("map"))
            {
                MessageShow("Show Map");
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Map.Visibility = Visibility.Visible;
                });
            }
            // Location
            if (commands.ToLower().Contains("location"))
            {
                MessageShow("Show Location");
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        var locationAccessStatus = await Geolocator.RequestAccessAsync();
                        if (locationAccessStatus == GeolocationAccessStatus.Allowed)
                        {
                            Geolocator geolocator = new Geolocator();
                            Geoposition currentPosition = await geolocator.GetGeopositionAsync();
                            Map.Center = new Geopoint(new BasicGeoposition()
                            {
                                Latitude = currentPosition.Coordinate.Latitude,
                                Longitude = currentPosition.Coordinate.Longitude
                            });
                            Map.ZoomLevel = 13;
                        }
                    }
                    catch (Exception message)
                    {
                        Debug.WriteLine(message);
                    }
                });
            }
            // Landslides
            if (commands.ToLower().Contains("land") || (commands.ToLower().Contains("slides")))
            {
                MessageShow("Show Land Slides");

                // Visualize Land Slides
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        // JSON
                        var client = new HttpClient();
                        var response = await client.GetAsync(new Uri("https://data.nasa.gov/resource/tfkf-kniw.json"));
                        var json = await response.Content.ReadAsStringAsync();
                        JsonSerializer serializer = new JsonSerializer();
                        landslides = JsonConvert.DeserializeObject<List<LandSlides>>(json);
                        // Process
                        foreach (var item in landslides)
                        {
                            Geopoint _point = new Geopoint(new BasicGeoposition()
                            {
                                Latitude = double.Parse(item.latitude),
                                Longitude = double.Parse(item.longitude)
                            });
                            MapIcon _icon = new MapIcon
                            {
                                Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/landslide.png")),
                                Location = _point,
                                NormalizedAnchorPoint = new Point(0.5, 1.0),
                                Title = item.adminname1 + ", " + item.countryname + ", " + DateTime.Parse(item.date).ToString("dd.MM.yyyy"),
                                ZIndex = 0
                            };
                            Map.MapElements.Add(_icon);
                        }
                    }
                    catch (Exception message)
                    {
                        Debug.WriteLine(message);
                    }
                });
            }
        }

        // Zoom 
        private async void ZoomCommands(string commands)
        {
            // Rosk
            if (commands.ToLower().Contains("in"))
            {
                MessageShow("Zoom In");
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Map.ZoomLevel++;
                });

            }
            // Classic
            if (commands.ToLower().Contains("out"))
            {
                MessageShow("Zoom Out");
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Map.ZoomLevel--;
                });
            }
        }

        // Radio
        private async void RadioCommands(string commands)
        {
            // Rosk
            if (commands.ToLower().Contains("rock"))
            {
                MessageShow("Radio Rock");
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Radio.Source = new Uri("http://stream.radioreklama.bg:80/radio1rock64");
                    Radio.Play();
                });

            }
            // Classic
            if (commands.ToLower().Contains("classic"))
            {
                MessageShow("Radio Classic");
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Radio.Source = new Uri("http://stream.radioreklama.bg:80/radio1128");
                    Radio.Play();
                });
            }
            // Stop
            if (commands.ToLower().Contains("stop"))
            {
                MessageShow("Radio Stop");
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Radio.Stop();
                });
            }
        }

        // Message Show    
        private async void MessageShow(string text)
        {
           
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Show Message Text
                Messages.Text = text;
                Messages.Visibility = Visibility.Visible;

                // Hide Message Timer
                MessageTimer = new DispatcherTimer();
                MessageTimer.Interval = new TimeSpan(0, 0, 5); // Magic 5 seconds
                MessageTimer.Tick += Timer_Tick;
                MessageTimer.Start();
            });
        }

        // Hide Message Timer
        private DispatcherTimer MessageTimer = null;
        private async void Timer_Tick(object sender, object e)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MessageTimer.Stop();
                Messages.Text = "";
                Messages.Visibility = Visibility.Collapsed;
            });
        }
    }
}
