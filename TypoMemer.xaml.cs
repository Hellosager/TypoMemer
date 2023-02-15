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
using TypoMemer.Components;

namespace TypoMemer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TypoMemerWindow : Window
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

        public TypoMemerWindow()
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
                {
                    System.Windows.Forms.SendKeys.SendWait(autoCompleteDropdown.Text);
                    currentString = ""; // reset the string , because we accepted a suggestion
                    autoCompleteDropdown.wordList.Clear(); // also clear suggestion list
                    autoCompleteDropdown.Text = "";
                    autoCompleteDropdown.IsDropDownOpen = false;
                    this.Hide();
                }

                Debug.WriteLine("Enter was pressed");
            }
            else if (e.Key == Key.Down)
            {
                Debug.WriteLine("Down");
            }
        }

        private void autoCompleteDropdown_TextChanged(object sender, TextChangedEventArgs e)
        {
            /*            autoCompleteDropdown.IsDropDownOpen = false;
            */
            Debug.WriteLine("text changed");

            string textBoxText = autoCompleteDropdown.Text;

            if ((!textBoxText.Equals(currentString)) // in case we switch back to source word
                 && textBoxText.Length > 5
                 /*&& !autoCompleteDropdown.Items.Contains(currentString)*/
                 && !autoCompleteDropdown.Items.Contains(textBoxText)) // don't do anything while changing through suggestions
            {
                if(typingTimer == null)
                {
                    typingTimer = new DispatcherTimer();
                    typingTimer.Interval = TimeSpan.FromMilliseconds(500);
                    typingTimer.Tick += new EventHandler(this.handleTypingTimerTimeout);
                }
                typingTimer.Stop();
                typingTimer.Tag = (sender as CustomComboBox).Text;
                typingTimer.Start();



            }
        }

        private string currentString = "";

        private void handleTypingTimerTimeout(object sender, EventArgs e)
        {
            var timer = sender as DispatcherTimer;
            if(timer == null)
            {
                return;
            }

            var searchTerm = timer.Tag.ToString();
            currentString = searchTerm;
            //this.caretPosition = searchTerm != null ? searchTerm.Length : 0;

            //Debug.WriteLine("Search Term is '" + searchTerm + "', setting caret position to " + caretPosition);
            Debug.WriteLine("Showing suggestions for " + searchTerm);

            // https://docs.microsoft.com/de-de/dotnet/csharp/programming-guide/concepts/async/
            new Task(() => { queryDatabaseAsync(searchTerm); }).Start();

            timer.Stop();

            // get help here: https://www.thecodebuzz.com/mongodb-csharp-driver-like-query-examples/
            // var filter = Builders<Word>.Filter.Regex("word", "^" + text + ".*");
        }

        private void queryDatabaseAsync(string searchTerm)
        {

            var result = App.wordCollection
                .Aggregate(buildAutocompletePipeline(searchTerm))
                .ToList();

            this.Dispatcher.Invoke(() =>
            {
                autoCompleteDropdown.IsDropDownOpen = false;
                autoCompleteDropdown.wordList.Clear();

                foreach (Word word in result)
                {
                    Debug.WriteLine(word.word);
                    autoCompleteDropdown.wordList.Add(word.word);
                }
                
                // do we really have to do this?
                autoCompleteDropdown.IsDropDownOpen = true;
                //autoCompleteDropdown.textBox.SelectionStart = searchTerm.Length;
                //autoCompleteDropdown.textBox.SelectionLength = 0;
            });
        }

        private PipelineDefinition<Word, Word> buildAutocompletePipeline(string searchTerm)
        {
            return new BsonDocument[]
            {
                new BsonDocument("$search",
                new BsonDocument("autocomplete",
                new BsonDocument
                        {
                            { "query", searchTerm },
                            { "path", "word" }
                        })),
                new BsonDocument("$limit", 10), // limit to 10 results
                new BsonDocument("$project",
                new BsonDocument
                    {
                        { "_id", 0 },
                        { "word", 1 }
                    })
            };
        }

    }
}
