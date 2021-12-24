using ChipLauncher.Model;
using System;
using System.Collections.Generic;
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

namespace ChipLauncher.UI
{
    public partial class OTPInputDialog : Window, ICloseable
    {
        public string UserInputOTPString => (DataContext as OTPInputDialogMV).UserInputOTPString;

        public OTPInputDialog() => InitializeComponent();

        private void HandleNumbersOnly(object obj, TextCompositionEventArgs e)
            => e.Handled = (obj as TextBox).Text.Length >= 6 || new Regex("[^0-9]+").IsMatch(e.Text ?? "");
    }
}
