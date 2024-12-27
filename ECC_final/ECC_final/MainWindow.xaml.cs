using ECC_final.ECC;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Xceed.Words.NET;
using System.Net.Mail;
using System.Net;

namespace ECC_final
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            TextBox[] textBoxes = { xA, xB, xD, xC1, xC2 , xP};

            foreach (TextBox textBox in textBoxes)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text)) // Kiểm tra TextBox rỗng
                {
                    MessageBox.Show($"TextBox '{textBox.Name}' is empty!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            try
            {
                response.Content = "Decoding...";
                response.Foreground = new SolidColorBrush(Colors.Orange);
                //curve
                int a = int.Parse(xA.Text.Trim());
                int b = int.Parse(xB.Text.Trim());
                int p = int.Parse(xP.Text.Trim());

                EllipticCurve curve = new EllipticCurve(a, b, p);
                //C1
                string[] partsC1 = xC1.Text.Trim('(', ')').Split(',');
                var C1 = (int.Parse(partsC1[0].Trim()), int.Parse(partsC1[1].Trim()));

                //C2
                Regex regex = new Regex(@"\((\d+),\s*(\d+),\s*(\d+)\)");
                MatchCollection matches = regex.Matches(xC2.Text);
                List<(int x, int y, int z)> C2 = new List<(int x, int y, int z)>();

                foreach (Match match in matches)
                {
                    int x = int.Parse(match.Groups[1].Value);
                    int y = int.Parse(match.Groups[2].Value);
                    int z = int.Parse(match.Groups[3].Value);
                    C2.Add((x, y, z));
                }
                //d
                int d = int.Parse(xD.Text.Trim());

                //Decode
                var text = await Task.Run(() => EllipticCurveAlgorithm.Decode(d, C1, C2, curve));
                decodeTextBox.Text = text;

                response.Content = "Decoding successful!";
                response.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (FormatException fe)
            {
                response.Content = "Decoding failed!";
                response.Foreground = new SolidColorBrush(Colors.Red);
                MessageBox.Show($"Input format error: {fe.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                response.Content = "Decoding failed!";
                response.Foreground = new SolidColorBrush(Colors.Red);
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Encode btn
        private async void btnEncode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(originalTextBox.Text))
            {
                MessageBox.Show("Please enter text!");
                return;
            }
            string text = originalTextBox.Text;

            // Hiển thị trạng thái đang mã hóa
            Dispatcher.Invoke(() =>
            {
                response.Content = "Is encoding...";
                response.Foreground = new SolidColorBrush(Colors.Orange);
                
            });

            try
            {
                // Chạy Encode trên luồng khác
                var encodedObject = await Task.Run(() => EllipticCurveAlgorithm.Encode(text));

                Dispatcher.Invoke(() =>
                {
                    xA.Text = encodedObject.curve.a.ToString();
                    xB.Text = encodedObject.curve.b.ToString();
                    xD.Text = encodedObject.d.ToString();
                    xQ.Text = $"({encodedObject.Q.x}, {encodedObject.Q.y})";
                    xC1.Text = $"({encodedObject.C1.x}, {encodedObject.C1.y})";
                    xP.Text = encodedObject.curve.p.ToString();

                    // Hiển thị các điểm trong C2
                    xC2.Text = string.Join(", ", encodedObject.C2.Select(c2 => $"({c2.x}, {c2.y}, {c2.offset})"));

                    // Thông báo thành công
                    response.Content = "Successfully encrypted!";
                    response.Foreground = new SolidColorBrush(Colors.Green);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    response.Content = "Encryption failed!";
                    response.Foreground = new SolidColorBrush(Colors.Red);
                });
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }


        private void btnOpen1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Word documents (*.docx)|*.docx|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    string content = "";
                    if (filePath.EndsWith(".docx"))
                    {
                        using (DocX document = DocX.Load(filePath))
                        {
                            foreach (var p in document.Paragraphs)
                            {
                                content += p.Text + "\n";
                            }
                        }
                    }
                    else if (filePath.EndsWith(".txt"))
                    {
                        content = System.IO.File.ReadAllText(filePath);
                    }

                    originalTextBox.Text = content;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}");
                }
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                TextBox[] textBoxes = { xK, xP, xPointX, xPointY };

                foreach (TextBox textBox in textBoxes)
                {
                    if (string.IsNullOrWhiteSpace(textBox.Text)) // Kiểm tra TextBox rỗng
                    {
                        MessageBox.Show($"TextBox '{textBox.Name}' is empty!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                int k = int.Parse(xK.Text.Trim());
                int p = int.Parse(xP.Text.Trim());
                int x = int.Parse(xPointX.Text.Trim());
                int y = int.Parse(xPointY.Text.Trim());
                var Q = (x, y);
                var result = EllipticCurve.Multiply(Q, k, p);
                xPointResult.Text = result.ToString();
            }catch(Exception err)
            {

            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            TextBox[] textBoxes = { xA, xB, xD, xQ, xC1, xC2, xP , xK, xPointX, xPointY, xPointResult, originalTextBox, xC1, xC2, decodeTextBox};

            foreach (TextBox textBox in textBoxes)
            {
                textBox.Text = "";
            }
        }

        private void btnSave2_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Word documents (*.docx)|*.docx|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                try
                {
                    if (filePath.EndsWith(".docx"))
                    {
                        using (DocX document = DocX.Create(filePath))
                        {
                            document.InsertParagraph(decodeTextBox.Text);
                            document.Save();
                        }
                    }
                    else if (filePath.EndsWith(".txt"))
                    {
                        System.IO.File.WriteAllText(filePath, decodeTextBox.Text);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}");
                }
            }
        }

        private void btnSave1_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Word documents (*.docx)|*.docx|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                try
                {
                    if (filePath.EndsWith(".docx"))
                    {
                        using (DocX document = DocX.Create(filePath))
                        {
                            document.InsertParagraph(xC1.Text);
                            document.InsertParagraph(xC2.Text);
                            document.Save();
                        }
                    }
                    else if (filePath.EndsWith(".txt"))
                    {
                        string content = xC1.Text + Environment.NewLine + xC2.Text;
                        System.IO.File.WriteAllText(filePath, content);
                    }
                }
                catch (Exception ex)
                {
                    response.Content = $"Error: {ex.Message}";
                }
            }
        }

        private void btnOpen2_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Word documents (*.docx)|*.docx|Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    string content = "";
                    if (filePath.EndsWith(".docx"))
                    {
                        using (DocX document = DocX.Load(filePath))
                        {
                            StringBuilder stringBuilder = new StringBuilder();
                            for (int i = 0; i < document.Paragraphs.Count; i++)
                            {
                                if (i == 0)
                                {
                                    xC1.Text = document.Paragraphs[i].Text;
                                }
                                else
                                {
                                    stringBuilder.AppendLine(document.Paragraphs[i].Text);
                                }
                            }
                            xC2.Text = stringBuilder.ToString();
                        }
                    }
                    else if (filePath.EndsWith(".txt"))
                    {
                        string[] lines = System.IO.File.ReadAllLines(filePath);
                        StringBuilder stringBuilder = new StringBuilder();
                        for(int i = 0; i < lines.Length; i++)
                        {
                            if( i == 0)
                            {
                                xC1.Text = lines[i];
                            }
                            else
                            {
                                stringBuilder.AppendLine(lines[i]);
                            }
                        }
                        xC2.Text += stringBuilder.ToString();

                    }

                    originalTextBox.Text = content;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}");
                }
            }
        }
    }
}
