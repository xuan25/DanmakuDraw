using BiliLogin;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DanmakuDraw
{
    public partial class MainWindow : Window
    {
        #region Init

        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < 16; i++)
            {
                RowDefinition rowD = new RowDefinition();
                ColumnDefinition colD = new ColumnDefinition();
                CanvasGrid.RowDefinitions.Add(rowD);
                CanvasGrid.ColumnDefinitions.Add(colD);
            }
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    Border gridB = new Border();
                    gridB.BorderThickness = new Thickness(0.1);
                    gridB.BorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00));
                    gridB.MouseLeftButtonDown += GridB_MouseLeftButtonDown;
                    gridB.MouseRightButtonDown += GridB_MouseRightButtonDown;
                    gridB.MouseEnter += GridB_MouseEnter;
                    gridB.Background = new SolidColorBrush(EraseColor);
                    CanvasGrid.Children.Add(gridB);
                    Grid.SetRow(gridB, i);
                    Grid.SetColumn(gridB, j);
                }
            }
            //CanvasGrid.ShowGridLines = true;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists("cookies.dat"))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                using (FileStream fileStream = new FileStream("cookies.dat", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    object obj = binaryFormatter.Deserialize(fileStream);
                    CookieCollection cookieCollection = (CookieCollection)obj;
                    UserCookieCollection = cookieCollection;
                }
            }
        }

        #endregion

        #region Drawing

        #region Toolbar

        private enum PaintModes { Draw, Erase };
        private PaintModes PaintMode = PaintModes.Draw;

        private void DrawBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawBtn.IsChecked = true;
            EraseBtn.IsChecked = false;
            PaintMode = PaintModes.Draw;
        }

        private void EraseBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawBtn.IsChecked = false;
            EraseBtn.IsChecked = true;
            PaintMode = PaintModes.Erase;
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (Border gridB in CanvasGrid.Children)
            {
                gridB.Background = new SolidColorBrush(EraseColor);
            }
        }

        #endregion
        
        #region Color

        private Color EraseColor = Color.FromArgb(0x01, 0xff, 0xff, 0xff);

        private void UpdateColor(Border gridB)
        {
            switch (PaintMode)
            {
                case PaintModes.Draw:
                    Color color = ((SolidColorBrush)ColorB.Background).Color;
                    gridB.Background = new SolidColorBrush(color);
                    break;
                case PaintModes.Erase:
                    gridB.Background = new SolidColorBrush(EraseColor);
                    break;
            }
        }

        private void ColorB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Color oriColor = ((SolidColorBrush)ColorB.Background).Color;
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.AllowFullOpen = true;
            colorDialog.FullOpen = true;
            colorDialog.ShowHelp = true;
            colorDialog.Color = System.Drawing.Color.FromArgb(0xff, oriColor.R, oriColor.G, oriColor.B);
            System.Windows.Forms.DialogResult dialogResult = colorDialog.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                ColorB.Background = new SolidColorBrush(Color.FromArgb(0xff, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
        }

        #endregion

        #region Mouse events

        private void GridB_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Border gridB = (Border)sender;
                UpdateColor(gridB);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                Border gridB = (Border)sender;
                gridB.Background = new SolidColorBrush(EraseColor);
            }
        }

        private void GridB_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border gridB = (Border)sender;
            UpdateColor(gridB);
        }

        private void GridB_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border gridB = (Border)sender;
            gridB.Background = new SolidColorBrush(EraseColor);
        }

        #endregion

        #endregion

        #region Damaku sending

        private const string ProcessingString = "🏃";
        private const string LoginString = "⎆";
        private const string ApplyString = "✔";

        #region Login

        private CookieCollection UserCookieCollection = null;

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UserCookieCollection == null)
            {
                MoblieLoginWindow moblieLoginWindow = new MoblieLoginWindow(this);
                moblieLoginWindow.LoggedIn += MoblieLoginWindow_LoggedIn;
                moblieLoginWindow.Canceled += MoblieLoginWindow_Canceled;
                moblieLoginWindow.Show();
                ApplyBtn.Content = LoginString;
                ApplyBtn.IsEnabled = false;
            }
            else
            {
                ApplyDrawing();
            }
        }        

        private void MoblieLoginWindow_Canceled(MoblieLoginWindow sender)
        {
            ApplyBtn.Content = ApplyString;
            ApplyBtn.IsEnabled = true;
        }

        private void MoblieLoginWindow_LoggedIn(MoblieLoginWindow sender, CookieCollection cookies, uint uid)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                sender.Topmost = false;
                sender.Hide();

                UserCookieCollection = cookies;

                ApplyBtn.Content = ApplyString;
                ApplyBtn.IsEnabled = true;

                sender.Close();

                BinaryFormatter binaryFormatter = new BinaryFormatter();
                using(FileStream fileStream = new FileStream("cookies.dat", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    binaryFormatter.Serialize(fileStream, cookies);
                }

                ApplyDrawing();
            }));
        }

        #endregion

        #region Sending request

        private CancellationTokenSource ApplyThreadCancellationTokenSource = null;
        SolidColorBrush ActiveBorderBrush = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0xff));
        Thickness ActiveBorderThickness = new Thickness(2);

        struct PixelData
        {
            public int X;
            public int Y;
            public Color Color;
            public Border GridB;

            public PixelData(int x, int y, Color color, Border gridB)
            {
                X = x;
                Y = y;
                Color = color;
                GridB = gridB;
            }
        }

        private void ApplyDrawing()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            ApplyThreadCancellationTokenSource = cancellationTokenSource;

            ApplyBtn.Content = ProcessingString;
            ApplyBtn.IsEnabled = false;

            List<PixelData> pixelDataList = new List<PixelData>();
            foreach (Border gridB in CanvasGrid.Children)
            {
                Color color = ((SolidColorBrush)gridB.Background).Color;
                if (color.A == 0xff)
                {
                    int row = Grid.GetRow(gridB);
                    int col = Grid.GetColumn(gridB);
                    pixelDataList.Add(new PixelData(col, row, color, gridB));
                    
                }
            }

            Thread thread = new Thread(() =>
            {
                Brush brushTemp = null;
                Thickness thicknessTemp = new Thickness();
                foreach (PixelData pixelData in pixelDataList)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        break;
                    
                    Dispatcher.Invoke(() =>
                    {
                        brushTemp = pixelData.GridB.BorderBrush;
                        thicknessTemp = pixelData.GridB.BorderThickness;
                        pixelData.GridB.BorderBrush = ActiveBorderBrush;
                        pixelData.GridB.BorderThickness = ActiveBorderThickness;
                    });
                    SetPixel(pixelData.X, pixelData.Y, pixelData.Color.R, pixelData.Color.G, pixelData.Color.B);
                    Thread.Sleep(1000);
                    Dispatcher.Invoke(() =>
                    {
                        pixelData.GridB.BorderBrush = brushTemp;
                        pixelData.GridB.BorderThickness = thicknessTemp;
                    });
                }
                Dispatcher.Invoke(() =>
                {
                    ApplyBtn.Content = ApplyString;
                    ApplyBtn.IsEnabled = true;
                    CancelBtn.IsEnabled = false;
                });
            });
            thread.Start();
            CancelBtn.IsEnabled = true;
        }

        private void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            string msg = HttpUtility.UrlEncode($"#x{x}y{y}r{r}g{g}b{b}");
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.live.bilibili.com/msg/send");
            httpWebRequest.Method = "POST";
            CookieContainer cookieContainer = new CookieContainer();
            cookieContainer.Add(UserCookieCollection);
            httpWebRequest.CookieContainer = cookieContainer;
            httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            httpWebRequest.Accept = "application/json, text/javascript, */*; q=0.01";
            httpWebRequest.Referer = "https://live.bilibili.com/866146";

            string csrf = string.Empty;
            foreach (Cookie cookie in UserCookieCollection) {
                if (cookie.Name == "bili_jct")
                {
                    csrf = cookie.Value;
                    break;
                }
            }
            Dictionary<string, string> dataDict = new Dictionary<string, string>{
                { "color", "16777215"},
                { "fontsize", "25"},
                { "mode", "1"},
                { "msg", msg},
                { "rnd", "0"},
                { "roomid", "866146"},
                { "bubble", "0"},
                { "csrf_token", csrf},
                { "csrf", csrf}
            };
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, string> keyValuePair in dataDict)
            {
                stringBuilder.Append('&');
                stringBuilder.Append(keyValuePair.Key);
                stringBuilder.Append('=');
                stringBuilder.Append(keyValuePair.Value);
            }
            string dataStr = stringBuilder.ToString(1, stringBuilder.Length - 1);
            byte[] data = Encoding.UTF8.GetBytes(dataStr);

            httpWebRequest.ContentLength = data.Length;

            using (Stream stream = httpWebRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            if (httpWebResponse.StatusCode == HttpStatusCode.OK)
            {
                using (Stream stream = httpWebResponse.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        string responseContent = streamReader.ReadToEnd();
                        Console.WriteLine($"{msg} - {responseContent}");
                    }
                }
            }
            else
            {
                throw new Exception(httpWebResponse.StatusDescription);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ApplyThreadCancellationTokenSource != null)
                ApplyThreadCancellationTokenSource.Cancel();
        }

        #endregion

        #endregion

        #region Save Load

        struct SavingPixel
        {
            public byte Index;
            public byte R;
            public byte G;
            public byte B;

            public SavingPixel(byte index, byte r, byte g, byte b)
            {
                Index = index;
                R = r;
                G = g;
                B = b;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            saveFileDialog.DefaultExt = ".dmd";
            saveFileDialog.Filter = "Danmaku Draw File (.dmd)|*.dmd";
            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                List<SavingPixel> savingPixels = new List<SavingPixel>();
                for(ushort i = 0; i < CanvasGrid.Children.Count; i++)
                {
                    Border gridB = (Border)CanvasGrid.Children[i];
                    Color color = ((SolidColorBrush)gridB.Background).Color;
                    if (color.A == 0xff)
                    {
                        SavingPixel savingPixel = new SavingPixel((byte)i, color.R, color.G, color.B);
                        savingPixels.Add(savingPixel);
                    }
                }
                using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    BinaryWriter binaryWriter = new BinaryWriter(fileStream);
                    binaryWriter.Write("DMD ".ToCharArray());           // header
                    binaryWriter.Write((byte)01);                       // major version
                    binaryWriter.Write((byte)01);                       // minor version
                    binaryWriter.Write((ushort)savingPixels.Count);     // pixel count
                    foreach (SavingPixel savingPixel in savingPixels)
                    {
                        binaryWriter.Write(savingPixel.Index);
                        binaryWriter.Write(savingPixel.R);
                        binaryWriter.Write(savingPixel.G);
                        binaryWriter.Write(savingPixel.B);
                    }
                }
            }
        }

        private void OpenFile(string filename)
        {
            using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                char[] chars = binaryReader.ReadChars(4);
                if (new string(chars) == "DMD ")
                {
                    byte major = binaryReader.ReadByte();
                    byte minor = binaryReader.ReadByte();
                    if (major == 01 && minor == 01)
                    {
                        ushort count = binaryReader.ReadUInt16();
                        for (ushort i = 0; i < count; i++)
                        {
                            byte index = binaryReader.ReadByte();
                            byte r = binaryReader.ReadByte();
                            byte g = binaryReader.ReadByte();
                            byte b = binaryReader.ReadByte();
                            ((Border)CanvasGrid.Children[index]).Background = new SolidColorBrush(Color.FromArgb(0xff, r, g, b));
                        }
                    }
                    else
                    {
                        MessageBox.Show("Unsupported file version");
                    }
                }
                else
                {
                    MessageBox.Show("Unknown format");
                }
            }
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".dmd";
            openFileDialog.Filter = "Danmaku Draw File (.dmd)|*.dmd";
            openFileDialog.Multiselect = true;
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                for (int i = 0; i < openFileDialog.FileNames.Length; i++)
                {
                    OpenFile(openFileDialog.FileNames[i]);
                }
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
            for(int i = 0; i < filenames.Length; i++)
            {
                OpenFile(filenames[i]);
            }
        }

        #endregion
    }
}
