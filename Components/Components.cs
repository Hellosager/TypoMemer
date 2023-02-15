using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TypoMemer.Components
{

    // inspiration: https://social.msdn.microsoft.com/Forums/vstudio/en-US/22a1d405-c0fe-40ea-b86d-ca0a02ae4e99/handling-combobox-keydown-and-keyup-directional-keys?forum=wpf
    // and ComboBox base class
    public class CustomComboBox : ComboBox
    {

        /*
         * TODOS:
         * 
         * - fill current selection case sensitive
         * 
         * */

        public static readonly DependencyProperty wordListProperty;
        public static readonly DependencyProperty typoMemerWindowProperty;


        public CustomComboBox()
        {
            wordList = new ObservableCollection<string>();
            ItemsSource = wordList;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            typoMemerWindow = (TypoMemerWindow)Window.GetWindow(this);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if((e.Key == Key.Down || e.Key == Key.Up) && !IsDropDownOpen && wordList.Count > 0)
            {
                IsDropDownOpen= true;
            } else
            {
                base.OnPreviewKeyDown(e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                IsDropDownOpen= false;
                this.wordList.Clear(); // also clear suggestion list
                this.Text = "";
                typoMemerWindow.Hide();
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            Debug.WriteLine("selection in CustomComboBox changed to " + e.AddedItems[0]);
            // textBox.text
            TextBox textBox = ((TextBox)(this.Template.FindName("PART_EditableTextBox", this)));
            string currentValue = textBox.Text; // from TextBoxBase
            string valueBefore = this.Text;
            if (e.AddedItems.Count > 0 && wordList.Contains(e.AddedItems[0]))
            {
                // we selected a suggestion, this is fine, do nothing
                this.Text = (string)e.AddedItems[0];
                textBox.SelectionStart = this.Text.Length;
                textBox.SelectionLength = 0;

                e.Handled = true;
            }
            else if(valueBefore.StartsWith(currentValue, StringComparison.CurrentCultureIgnoreCase))
            {

                // TODO do i still need this after setting textSearchEnabled false?
                // we just deleted a character, this is fine, do nothing
                this.Text = currentValue;
                textBox.CaretIndex = currentValue.Length;
                e.Handled = true;
            }
            else
            {

                base.OnSelectionChanged(e);
            }

        }

        public ObservableCollection<string> wordList { get; set; }

        public TypoMemerWindow typoMemerWindow { get; set; }
    }
}
