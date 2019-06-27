﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var img = new BitmapImage(new Uri(@"pack://application:,,,/Images/map.png"));
            imgMap.Source = img;
        }

        private void CreateCameraMarker(Point pos, Cam cam)
        {
            var img = new Image
            {
                Source = new BitmapImage(new Uri($@"pack://application:,,,/Images/cam_{(cam.Status ? "green" : "red")}.png")),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(cam.Angle),
                Width = 32,
                Height = 32,
                Tag = cam,
                ToolTip = cam.Label
            };
            img.MouseDown += Cam_MouseDown;
            img.MouseEnter += Img_MouseEnter;
            cnvMap.Children.Add(img);
            Canvas.SetLeft(img, pos.X - img.Width / 2);
            Canvas.SetTop(img, pos.Y - img.Height / 2);
        }

        private void Img_MouseEnter(object sender, MouseEventArgs e)
        {
            var cam = (Cam)((Image)sender).Tag;
            var newSelection = from ListBoxItem item in lstDevices.Items
                               where cam.Label.Equals(item.Content)
                               select item;
            if (newSelection.Any())
                lstDevices.SelectedItem = newSelection.First();
        }

        private void Cam_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var cam = (Cam)((Image)sender).Tag;
            if (e.LeftButton == MouseButtonState.Pressed)
                new StreamWindow(cam, false).ShowDialog();
            else if (e.RightButton == MouseButtonState.Pressed)
                new StreamWindow(cam, true).ShowDialog();
        }

        private void CreateClientMarker(Point pos, Client client)
        {
            var ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = client.Status ? Brushes.Green : Brushes.Red,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                ToolTip = client.Label,
                Tag = client
            };
            ellipse.MouseEnter += Ellipse_MouseEnter;
            cnvMap.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, pos.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, pos.Y - ellipse.Height / 2);
        }
        
        private void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            var client = (Client)((Ellipse)sender).Tag;
            var newSelection = from ListBoxItem item in lstDevices.Items
                               where client.Label.Equals(item.Content)
                               select item;
            if (newSelection.Any())
                lstDevices.SelectedItem = newSelection.First();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadInformation();
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            var selectedItem = (ListBoxItem)lstDevices.SelectedItem;
            LoadInformation();
            if (selectedItem != null)
            {
                var newSelection = from ListBoxItem item in lstDevices.Items
                                   where selectedItem.Content.Equals(item.Content)
                                   select item;
                if (newSelection.Any())
                    lstDevices.SelectedItem = newSelection.First();
            }
        }

        private void LoadInformation()
        {
            List<Cam> cams = Task.Run(async () => { return await Communicator.GetCamsAsync(); }).Result;
            List<Client> clients = Task.Run(async () => { return await Communicator.GetClientsAsync(); }).Result;

            cnvMap.Children.Clear();
            cnvMap.Width = imgMap.ActualWidth;
            cnvMap.Height = imgMap.ActualHeight;
            lstDevices.Items.Clear();
            txtInfo.Clear();

            foreach (var client in clients)
            {
                CreateClientMarker(new Point(cnvMap.Width * client.X, cnvMap.Height * client.Y), client);
            }

            foreach (var cam in cams)
            {
                CreateCameraMarker(new Point(cnvMap.Width * cam.X, cnvMap.Height * cam.Y), cam);

                ListBoxItem item = new ListBoxItem
                {
                    Tag = cam,
                    Content = cam.Label,
                    FontWeight = FontWeights.Bold
                };
                lstDevices.Items.Add(item);

                foreach (var client in clients.Where(c => c.Id == cam.Client_Id))
                {
                    ListBoxItem subitem = new ListBoxItem
                    {
                        Tag = client,
                        Content = client.Label
                    };
                    lstDevices.Items.Add(subitem);
                }
            }
        }

        private void LstDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lstDevices.SelectedItem as ListBoxItem;
            if (selectedItem != null)
            {
                if (selectedItem.Tag is Client client)
                {
                    txtInfo.Text = client.ToString();
                }
                else if (selectedItem.Tag is Cam cam)
                {
                    txtInfo.Text = cam.ToString();
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInformation();
        }
    }
}
