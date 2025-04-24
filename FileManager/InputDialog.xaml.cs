using System.Windows;

namespace FileManager
{
    public partial class InputDialog : Window
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ResponseText { get; set; }

        public InputDialog(string title, string message, string defaultResponse = "")
        {
            InitializeComponent();
            Title = title;
            Message = message;
            ResponseText = defaultResponse;
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
} 