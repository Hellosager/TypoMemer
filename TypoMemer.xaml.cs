using System;
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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using TypoMemer.Models;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace TypoMemer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void SetActiveWindow(IntPtr windowHandle) => SetForegroundWindow(windowHandle);

        private const int HOTKEY_ID = 1337;

        private const uint MOD_CONTROL = 0x0002; // CTRL
        private const uint VK_SPACE = 0x20;

        private static IMongoCollection<Word> wordCollection;

        IntPtr handle;

        private IntPtr _windowHandle;
        private HwndSource _source;

        private DispatcherTimer typingTimer;

        private ObservableCollection<string> words = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _windowHandle = new WindowInteropHelper(this).EnsureHandle();
            Debug.Write("Handle: " + _windowHandle + Environment.NewLine);
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);
            Debug.Write("Initializing Hotkey" + Environment.NewLine);
            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_SPACE); //CTRL + Space
            autoCompleteDropdown.ItemsSource = words;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_SPACE)
                            {
                                handle = GetForegroundWindow();
                                this.WindowState = WindowState.Normal;
                                
                                // both is needed else it won't focus the window
                                this.Show();
                                this.Activate();
                                
                                // this basically waits for rendering to focus the field
                                this.Dispatcher.BeginInvoke((Action)delegate
                                {
                                    Keyboard.Focus(autoCompleteDropdown);
                                }, DispatcherPriority.Render);

                                Debug.Write("Space was pressed" + Environment.NewLine);
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        /*protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }*/

        private void autoCompleteDropdown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (SetForegroundWindow(handle))
                    System.Windows.Forms.SendKeys.SendWait(autoCompleteDropdown.Text);

                Debug.WriteLine("Enter was pressed");
            }
        }

        private void autoCompleteDropdown_TextChanged(object sender, TextChangedEventArgs e)
        {
            autoCompleteDropdown.IsDropDownOpen = false;
            if (autoCompleteDropdown.Text.Length > 5 && !autoCompleteDropdown.Items.Contains(autoCompleteDropdown.Text))
            {
                if(typingTimer == null)
                {
                    typingTimer = new DispatcherTimer();
                    typingTimer.Interval = TimeSpan.FromMilliseconds(500);
                    typingTimer.Tick += new EventHandler(this.handleTypingTimerTimeout);
                }
                typingTimer.Stop();
                typingTimer.Tag = (sender as ComboBox).Text;
                typingTimer.Start();



            }
        }

        private int caretPosition = 0;
        private void autoCompleteDropdown_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // see https://stackoverflow.com/questions/1441645/wpf-dropdown-of-a-combobox-highlightes-the-text
            TextBox textBox = (TextBox)((ComboBox)sender).Template.FindName("PART_EditableTextBox", (ComboBox)sender);
/*            if(((ComboBox)sender).Text.Length > 0)
            {
                textBox.SelectionStart = ((ComboBox)sender).Text.Length;
                textBox.SelectionLength = 0;
            }*/
/*
            TextBox txt = (TextBox)sender;

            if (autoCompleteDropdown.IsDropDownOpen && txt.SelectionLength > 0)
            {
                txt.CaretIndex = caretPosition;
            }
            if (txt.SelectionLength == 0 && txt.CaretIndex != 0)
            {
                caretPosition = txt.CaretIndex;
            }*/
        }

        private void handleTypingTimerTimeout(object sender, EventArgs e)
        {
            var timer = sender as DispatcherTimer;
            if(timer == null)
            {
                return;
            }

            var text = timer.Tag.ToString();
            Debug.WriteLine("Showing suggestions for " + text);

            // get help here: https://www.thecodebuzz.com/mongodb-csharp-driver-like-query-examples/
            var filter = Builders<Word>.Filter.Regex("word", "^" + text + ".*");

            // https://docs.microsoft.com/de-de/dotnet/csharp/programming-guide/concepts/async/
            new Task(() => { queryDatabaseAsync(filter); }).Start();

            timer.Stop();
        }

        private void queryDatabaseAsync(FilterDefinition<Word> filter)
        {
            var cursor = App.wordCollection.Find(filter).Limit(10);
            var result = cursor.ToList();
            /*List<string> words = new List<string>();*/

            this.Dispatcher.Invoke(() =>
            {
                words.Clear();

                foreach (Word word in result)
                {
                    Debug.WriteLine(word.word);
                    // TODO: And now show the words in frontend
                    words.Add(word.word);
                }
                autoCompleteDropdown.IsDropDownOpen = true;
            });
            /*autoCompleteDropdown.ItemsSource = words;*/

        }

    }
}
