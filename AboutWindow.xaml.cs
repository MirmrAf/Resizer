using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace Resizer
{
    /// <summary>
    /// Window what represent credits and authors
    /// </summary>
    public partial class AboutWindow : Window
    {
        public struct CreditAuthors
        {
            public string Author;
            public string Github;
            public string Content;

            public CreditAuthors(string author, string github, string content)
            {
                
                Author = author;
                Github = github;
                Content = content;
            }
        }
        private void AddCredit(CreditAuthors credit)
        {
            StackPanel mainLayoutPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(5),

            };
            Border mainBorder = new Border
            {
                Background = Brushes.WhiteSmoke,
                Child = mainLayoutPanel,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(2),
                Margin = new Thickness(5),
            };
            TextBlock textAuth = new TextBlock();
            textAuth.HorizontalAlignment = HorizontalAlignment.Center;
            textAuth.Text = credit.Author;
            textAuth.Margin = new Thickness(0, 5, 0, 5);
            textAuth.FontSize = 14;
            textAuth.Foreground = Brushes.Black;
            TextBlock textGit = new TextBlock();
            textGit.HorizontalAlignment = HorizontalAlignment.Left;
            textGit.Text = credit.Github;
            textGit.Margin = new Thickness(0, 5, 0, 5);
            textGit.FontSize = 12;
            textGit.Foreground = Brushes.Black;
            TextBlock textBlock = new TextBlock();
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
            textBlock.Text = credit.Content;
            textBlock.Margin = new Thickness(0, 5, 0, 5);
            textBlock.FontSize = 10;
            textBlock.Foreground = Brushes.Black;
            mainLayoutPanel.Children.Add(textAuth);
            mainLayoutPanel.Children.Add(textGit);
            mainLayoutPanel.Children.Add(textBlock);

            CreditsPanel.Children.Add(mainBorder);
        }
        private void LoadCredits()
        {
            string jsonFilePath = "Abouts/AuthorsAndUsingRes.json";
            string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
            List<CreditAuthors> credits = JsonSerializer.Deserialize<List<CreditAuthors>>(jsonContent, new JsonSerializerOptions
            {
                IncludeFields = true
            });
            foreach (var credit in credits)
            {
                AddCredit(credit);
            }
        }
        public AboutWindow()
        {
            InitializeComponent();
            LoadCredits();
        }


    }
}
