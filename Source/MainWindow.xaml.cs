using CoreAudio;
using Newtonsoft.Json;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;



namespace CollectionLauncher
{
    public partial class MainWindow : Window
    {
        private bool isDragging = false;
        private Point startPoint;
        private Controller controller;
        private DispatcherTimer timer;
        public static string currentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "";
        private string gamePath = currentPath + @"\games\";

        public static int selectedIndex = 0;
        private List<Game> games = new();
        MMDevice currentDevice;
        private DateTime lastInteract = DateTime.Now;
        private int lastSpeed = 200;
        private void initGames()
        {
            lblNoGame.Visibility = Visibility.Hidden;
            games.Clear();
            System.IO.Directory.CreateDirectory(gamePath);
            var directories = Directory.GetDirectories(currentPath + @"\games\");
            foreach (var directory in directories)
            {
                DirectoryInfo directoryInfo = new(directory);
                if (File.Exists(System.IO.Path.Combine(directory, "cover.jpg")))
                {
                    var game = new Game
                    {
                        Name = directoryInfo.Name,
                    };
                    var jsonPath = System.IO.Path.Combine(directory, "info.json");
                    if (!File.Exists(jsonPath))
                        File.WriteAllText(jsonPath, JsonConvert.SerializeObject(game));
                    else
                        game = JsonConvert.DeserializeObject<Game>(File.ReadAllText(jsonPath));
                    games.Add(game);
                }
            }
            refreshGames();
            if (gamesImageContainer.Children.Count > 0)
            {
                BlurEffect blurEffect = new() { Radius = 10 };
                blurredImage.Effect = blurEffect;
                DockPanel p = (DockPanel)gamesImageContainer.Children[selectedIndex];
                Image i = (Image)p.Children[0];
                ImageMouseEnter(i);
            }
            else
            {
                lblNoGame.Visibility = Visibility.Visible;
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            initGames();





            controller = new Controller(UserIndex.One);

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            timer.Tick += Timer_Tick;
            timer.Start();


            MMDeviceEnumerator enumerator = new(new Guid());
            MMDeviceCollection audioDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            var def = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            foreach (var device in audioDevices)
            {
                audioList.Items.Add(device.FriendlyName);
                if (def.ID == device.ID)
                {
                    currentDevice = device;
                    audioList.SelectedItem = device.FriendlyName;
                    volumeSlide.Value = currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
                    volumeMuted.IsChecked = !currentDevice.AudioEndpointVolume.Mute;
                    if (volumeSlide.Value == 0)
                        currentDevice.AudioEndpointVolume.Mute = true;
                }
            }
            audioList.SelectionChanged += AudioList_SelectionChanged;
            volumeSlide.ValueChanged += VolumeSlide_ValueChanged;
            volumeMuted.Click += VolumeMuted_Click;
        }

        private void VolumeMuted_Click(object sender, RoutedEventArgs e)
        {
            currentDevice.AudioEndpointVolume.Mute = !volumeMuted.IsChecked ?? true;

        }

        private void VolumeSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar = (float)volumeSlide.Value / 100;
            if (volumeSlide.Value == 0)
                currentDevice.AudioEndpointVolume.Mute = true;
            SystemSounds.Beep.Play();

        }

        private void AudioList_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetDefaultAudioPlaybackDevice(audioList.SelectedItem.ToString() ?? "");

        public void SetDefaultAudioPlaybackDevice(string deviceName)
        {
            MMDeviceEnumerator enumerator = new(new Guid());
            MMDeviceCollection devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (MMDevice device in devices)
            {
                if (device.DeviceFriendlyName == deviceName)
                {
                    enumerator.SetDefaultAudioEndpoint(device);
                    var a = device.AudioEndpointVolume;
                    SystemSounds.Beep.Play();
                    break;
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                volumeSlide.Value = currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
                volumeMuted.IsChecked = !currentDevice.AudioEndpointVolume.Mute;

                if (!this.IsActive || lastInteract.AddMilliseconds(lastSpeed) > DateTime.Now)
                    return;

                if (!controller.IsConnected)
                {
                    controller = new Controller(UserIndex.One);
                    return;
                }
                DockPanel p = (DockPanel)gamesImageContainer.Children[selectedIndex];
                Image i = (Image)p.Children[0];
                Game game = (Game)i.Tag;


                State state = controller.GetState();

                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp) && audioList.SelectedIndex > 0)
                    audioList.SelectedIndex--;
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown) && audioList.SelectedIndex < audioList.Items.Count - 1)
                    audioList.SelectedIndex++;

                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight) && volumeSlide.Value < 100)
                    volumeSlide.Value++;
                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft) && volumeSlide.Value > 0)
                    volumeSlide.Value--;

                if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                {
                    if (i.Width == 300) ImageMouseEnter(i);
                    ImageClick(i);
                    lastInteract = DateTime.Now;
                }
                else if (state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.B))
                {
                    ImageRightClick(i);
                    lastInteract = DateTime.Now;

                }


                short thumbLeftX = state.Gamepad.LeftThumbX;
                bool right = false;
                bool left = false;
                if (thumbLeftX > 25000)
                {
                    right = true;
                }
                if (thumbLeftX < -25000)
                {
                    left = true;
                }
                if (right || left)
                {
                    lastInteract = DateTime.Now;
                    if (right && selectedIndex < games.Count - 1)
                        selectedIndex++;
                    if (left && selectedIndex > 0)
                        selectedIndex--;

                    p = (DockPanel)gamesImageContainer.Children[selectedIndex];
                    i = (Image)p.Children[0];
                    ImageMouseEnter(i);
                    if (right && selectedIndex > 0)
                    {
                        p = (DockPanel)gamesImageContainer.Children[selectedIndex - 1];
                        i = (Image)p.Children[0];
                        ImageMouseLeave(i);
                    }
                    if (left && selectedIndex >= 0)
                    {
                        p = (DockPanel)gamesImageContainer.Children[selectedIndex + 1];
                        i = (Image)p.Children[0];
                        ImageMouseLeave(i);
                    }
                    scrollPanel.ScrollToHorizontalOffset(((selectedIndex * 300) - this.Width / 2) + 300);
                }
            }
            catch (Exception)
            {
            }

        }

        public void refreshGames()
        {
            games = games.OrderByDescending(g => !string.IsNullOrEmpty(g.InstalledPath))
                       .ThenByDescending(g => g.LastRunDateTime)
                       .ThenByDescending(g => g.RunCount)
                       .ToList();
            gamesImageContainer.Children.Clear();
            foreach (var game in games)
            {
                DockPanel dockPanel = new();
                BitmapImage coverImage = new(new Uri(System.IO.Path.Combine(gamePath + game.Name, "cover.jpg")));
                Image imageControl = new()
                {
                    Source = coverImage,
                    Tag = game,
                    Width = 300,
                    Height = 400,
                    Stretch = Stretch.UniformToFill,
                    StretchDirection = StretchDirection.DownOnly,
                };
                imageControl.MouseEnter += ImageControl_MouseEnter;
                imageControl.MouseLeave += ImageControl_MouseLeave;
                imageControl.MouseLeftButtonDown += ImageControl_MouseLeftButtonUp;
                imageControl.MouseRightButtonUp += ImageControl_MouseRightButtonUp;
                dockPanel.Children.Add(imageControl);
                gamesImageContainer.Children.Add(dockPanel);
            }
        }


        private void ImageControl_MouseRightButtonUp(object sender, MouseButtonEventArgs e) => ImageRightClick((Image)sender);
        private void ImageRightClick(Image image)
        {
            Game game = (Game)image.Tag;
            var res = game.InstalledPath = Microsoft.VisualBasic.Interaction.InputBox($"Enter Executable {game.Name} Path:",
                                           "Executable Game Path",
                                           game.InstalledPath,
                                           -1, -1).Replace("\"", "");
            File.WriteAllText(System.IO.Path.Combine(gamePath + game.Name, "info.json"), JsonConvert.SerializeObject(game));
            games[games.IndexOf(games.First(x => x.InstalledPath == game.InstalledPath))] = game;
            refreshGames();
        }
        private void ImageControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
                ImageClick((Image)sender);
        }
        private void ImageClick(Image image)
        {
            Game game = (Game)image.Tag;
            game.RunCount += 1;
            if (!string.IsNullOrEmpty(game.InstalledPath))
            {
                Process process = new();
                process.StartInfo.FileName = game.InstalledPath;
                process.StartInfo.WorkingDirectory = new FileInfo(game.InstalledPath).Directory.FullName;
                process.Start();

            }
            game.LastRunDateTime = DateTime.Now;
            File.WriteAllText(System.IO.Path.Combine(gamePath + game.Name, "info.json"), JsonConvert.SerializeObject(game));
        }

        private void ImageControl_MouseLeave(object sender, MouseEventArgs e) => ImageMouseLeave((Image)sender);

        private void ImageControl_MouseEnter(object sender, MouseEventArgs e) => ImageMouseEnter((Image)sender);
        private void ImageMouseLeave(Image image)
        {
            DoubleAnimation widthAnimation = new()
            {
                To = 300,
                Duration = TimeSpan.FromSeconds(0.25)
            };
            DoubleAnimation heightAnimation = new()
            {
                To = 400,
                Duration = TimeSpan.FromSeconds(0.25)
            };
            image.BeginAnimation(Image.WidthProperty, widthAnimation);
            image.BeginAnimation(Image.HeightProperty, heightAnimation);
        }
        private void ImageMouseEnter(Image image)
        {

            foreach (DockPanel p in gamesImageContainer.Children)
            {
                Image i = (Image)p.Children[0];
                if (i.Width > 300)
                    ImageMouseLeave(i);
            }
            Game game = (Game)image.Tag;
            DoubleAnimation widthAnimation = new()
            {
                To = 400,
                Duration = TimeSpan.FromSeconds(0.25)
            };
            DoubleAnimation heightAnimation = new()
            {
                To = 533,
                Duration = TimeSpan.FromSeconds(0.25)
            };
            image.BeginAnimation(Image.WidthProperty, widthAnimation);
            image.BeginAnimation(Image.HeightProperty, heightAnimation);
            blurredImage.Source = image.Source;
            selectedIndex = games.IndexOf(games.First(x => x.InstalledPath == game.InstalledPath));
        }
        private void ScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            startPoint = e.GetPosition(null);
        }
        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                ScrollViewer scrollViewer = sender as ScrollViewer;

                if (scrollViewer != null)
                {
                    Point newPoint = e.GetPosition(null);
                    Vector delta = startPoint - newPoint;

                    startPoint = newPoint;

                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + delta.X);
                }
            }
        }
        private void ScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => isDragging = false;
        private void btnAddGame_Click(object sender, RoutedEventArgs e)
        {
            AddGame addGame = new AddGame();
            addGame.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            addGame.Closed += AddGame_Closed;
            addGame.ShowDialog();
        }
        private void AddGame_Closed(object? sender, EventArgs e) => initGames();
        private void lblNoGame_MouseDown(object sender, MouseButtonEventArgs e) => btnAddGame_Click(sender, e);
    }
}
