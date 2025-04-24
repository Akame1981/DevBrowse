using System.Windows;

namespace FileManager.ViewModels
{
    /// <summary>
    /// A dialog window for getting user input.
    /// </summary>
    public partial class InputDialog : Window
    {
        /// <summary>
        /// Initializes a new instance of the InputDialog class.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="prompt">The prompt text to display.</param>
        /// <param name="defaultValue">The default value for the input field.</param>
        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            PromptText = prompt;
            ResponseText = defaultValue;
            DataContext = this;
        }

        /// <summary>
        /// Gets or sets the prompt text.
        /// </summary>
        public string PromptText { get; set; }

        /// <summary>
        /// Gets or sets the response text.
        /// </summary>
        public string ResponseText { get; set; }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
} 