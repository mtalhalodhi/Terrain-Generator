using Helix = HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace TerrainGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Zone
        {
            private double depth;

            public string Name { get; set; }
            public double Depth { get => depth; set => depth = value; }
            public System.Drawing.Color Color { get; set; }

            public string DepthString
            {
                get
                {
                    return Depth.ToString();
                }
                set
                {
                    if (!double.TryParse(value, out depth)) depth = 0;
                }
            }

            public string ColorString
            {
                get
                {
                    return Color.Name;
                }
                set
                {
                    try
                    {
                        Color = System.Drawing.Color.FromName(value);
                    }
                    catch
                    {

                        Color = System.Drawing.Color.Black;
                    }
                }
            }

            public System.Windows.Media.Color MediaColor
            {
                get
                {
                    return System.Windows.Media.Color.FromRgb(Color.R, Color.G, Color.B);
                }
                set
                {
                    Color = System.Drawing.Color.FromArgb(value.R, value.G, value.B);
                }
            }
        }

        private List<Zone> Zones = new List<Zone>();
        
        Bitmap map;
        Bitmap noise;

        public MainWindow()
        {
            InitializeComponent();
        }

        double mWidth = 512;
        double mHeight = 512;

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int width = int.Parse(MapWidth.Text);
                int height = int.Parse(MapHeight.Text);
                mWidth = width;
                mHeight = height;
                double scale = double.Parse(NoiseScale.Text);
                int octaves = int.Parse(NoiseOctaves.Text);
                double persistance = double.Parse(NoisePersistance.Text);
                double lacunarity = double.Parse(NoiseLacunarity.Text);
                int seed = int.Parse(NoiseSeed.Text);
                double multiplier = double.Parse(MapScale.Text);

                map = new Bitmap(width, height);
                noise = new Bitmap(width, height);

                var pnoise = PerlinNoise.GenerateNoiseMap(width, height, scale, octaves, persistance, lacunarity, seed);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        System.Drawing.Color c = System.Drawing.Color.Black;
                        System.Drawing.Color m = System.Drawing.Color.Black;

                        foreach (var z in Zones)
                        {
                            if (z.Depth >= pnoise[x, y])
                            {
                                c = z.Color;
                                break;
                            }
                        }

                        int v = (int)(pnoise[x, y] * 255);
                        v = v > 255 ? 255 : v < 0 ? 0 : v;
                        m = System.Drawing.Color.FromArgb(v, v, v);

                        map.SetPixel(x, y, c);
                        noise.SetPixel(x, y, m);

                    }
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pnoise[x, y] *= -multiplier;
                    }
                }

                MapImage.Source = BitmapToSource(map);
                NoiseImage.Source = BitmapToSource(noise);

                var mesh = ModelGenerator.GenerateTerrainMesh(pnoise).CreateMesh();

                DiffuseMaterial diffuse = new DiffuseMaterial(new ImageBrush(BitmapToSource(map)));

                diffuse.AmbientColor = Colors.Black;
                diffuse.Color = Colors.White;

                MeshVisual.Content = new GeometryModel3D(mesh, diffuse);
                MeshVisual.Transform = new ScaleTransform3D(1, 1, -1);
            }
            catch (Exception E)
            {
                ErrorDialogText.Text = E.Message;
                ErrorDialog.IsOpen = true;
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var d = new SaveFileDialog();
                d.Filter = "Bitmap|*.bmp";
                if (d.ShowDialog().Value)
                {
                    map.Save(d.FileName.Replace(".bmp", "_map.bmp"));
                    noise.Save(d.FileName.Replace(".bmp", "_noise.bmp"));
                }
            }
            catch (Exception E)
            {
                ErrorDialogText.Text = E.Message;
                ErrorDialog.IsOpen = true;
            }
        }

        private BitmapSource BitmapToSource(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private void AddZone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Zones.Add(new Zone() { Name = "", Depth = 0, Color = System.Drawing.Color.Black });
            }
            catch (Exception E)
            {
                ErrorDialogText.Text = E.Message;
                ErrorDialog.IsOpen = true;
            }
            ZonesDisplay.ItemsSource = null;
            ZonesDisplay.ItemsSource = Zones;
        }

        private void RemoveZone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Zones.RemoveAt(ZonesDisplay.SelectedIndex);
            }
            catch (Exception E)
            {
                ErrorDialogText.Text = E.Message;
                ErrorDialog.IsOpen = true;
            }

            ZonesDisplay.ItemsSource = null;
            ZonesDisplay.ItemsSource = Zones;
        }

        private void MapZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MapImage.Width = mWidth / 100 * MapZoomSlider.Value;
            MapImage.Height = mHeight / 100 * MapZoomSlider.Value;
            NoiseImage.Width = mWidth / 100 * MapZoomSlider.Value;
            NoiseImage.Height = mHeight / 100 * MapZoomSlider.Value;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var item = (string)((Label)((ListBox)sender).SelectedItem).Content;

                if (item == "Noise")
                {
                    NoiseGrid.Visibility = Visibility.Visible;
                    MapGrid.Visibility = Visibility.Collapsed;
                    MeshGrid.Visibility = Visibility.Collapsed;
                }
                if (item == "Texture")
                {
                    NoiseGrid.Visibility = Visibility.Collapsed;
                    MapGrid.Visibility = Visibility.Visible;
                    MeshGrid.Visibility = Visibility.Collapsed;
                }
                if (item == "3D Mesh")
                {

                    NoiseGrid.Visibility = Visibility.Collapsed;
                    MapGrid.Visibility = Visibility.Collapsed;
                    MeshGrid.Visibility = Visibility.Visible;
                }
            }
            catch (Exception E)
            {
                ErrorDialogText.Text = E.Message;
                ErrorDialog.IsOpen = true;
            }
        }

        private void CloseErrorDialog_Click(object sender, RoutedEventArgs e)
        {
            ErrorDialog.IsOpen = false;
        }
    }
}
