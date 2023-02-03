using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
         * - Support deleting of character
         * - fill current selection case sensitive
         * 
         * */

        public static readonly DependencyProperty wordListProperty;


        public CustomComboBox()
        {
            wordList = new ObservableCollection<string>();
            ItemsSource = wordList;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if(e.Key == Key.Down && !IsDropDownOpen && wordList.Count > 0)
            {
                IsDropDownOpen= true;
            } else
            {
                base.OnPreviewKeyDown(e);
            }
        }

        public ObservableCollection<string> wordList { get; set; }

    }
}
