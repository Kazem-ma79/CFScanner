using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Net;
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

namespace Windows
{
    /// <summary>
    /// Interaction logic for IPAddressTextBox.xaml
    /// </summary>
    public partial class IPAddressTextBox : UserControl
    {
        #region class variables and properties

        #region private variables and properties
        private TextBox FirstBox { get { return firstBox; } }
        private TextBox SecondBox { get { return secondBox; } }
        private TextBox ThirdBox { get { return thirdBox; } }
        private TextBox FourthBox { get { return fourthBox; } }
        #endregion

        #region public variables and properties
        public string Text
        {
            get
            {
                return $"{firstBox.Text}.{secondBox.Text}.{thirdBox.Text}.{fourthBox.Text}";
            }
            set
            {
                string[] _value = value.Split('.');
                firstBox.Text = _value[0];
                secondBox.Text = _value[1];
                thirdBox.Text = _value[2];
                fourthBox.Text = _value[3];
            }
        }

        public IPAddress IP
        {
            get
            {
                return IPAddress.Parse(Text);
            }
            set
            {
                string[] _value = value.ToString().Split('.');
                firstBox.Text = _value[0];
                secondBox.Text = _value[1];
                thirdBox.Text = _value[2];
                fourthBox.Text = _value[3];
            }
        }
        #endregion

        #endregion


        #region constructors
        public IPAddressTextBox()
        {
            InitializeComponent();
        }

        public IPAddressTextBox(byte[] bytesToFill)
        {
            InitializeComponent();

            firstBox.Text = Convert.ToString(bytesToFill[0]);
            secondBox.Text = Convert.ToString(bytesToFill[1]);
            thirdBox.Text = Convert.ToString(bytesToFill[2]);
            fourthBox.Text = Convert.ToString(bytesToFill[3]);
        }
        #endregion


        #region methods

        #region public methods
        public byte[] GetByteArray()
        {
            byte[] userInput = new byte[4];

            userInput[0] = Convert.ToByte(firstBox.Text);
            userInput[1] = Convert.ToByte(secondBox.Text);
            userInput[2] = Convert.ToByte(thirdBox.Text);
            userInput[3] = Convert.ToByte(fourthBox.Text);

            return userInput;
        }
        #endregion

        #region private methods
        private void jumpRight(TextBox rightNeighborBox, KeyEventArgs e)
        {
            rightNeighborBox.Focus();
            rightNeighborBox.CaretIndex = 0;
            e.Handled = true;
        }

        private void jumpLeft(TextBox leftNeighborBox, KeyEventArgs e)
        {
            leftNeighborBox.Focus();
            if (leftNeighborBox.Text != "")
            {
                leftNeighborBox.CaretIndex = leftNeighborBox.Text.Length;
            }
            e.Handled = true;
        }

        //checks for backspace, arrow and decimal key presses and jumps boxes if needed.
        //returns true when key was matched, false if not.
        private bool checkJumpRight(TextBox currentBox, TextBox rightNeighborBox, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    if (currentBox.CaretIndex == currentBox.Text.Length || currentBox.Text == "")
                    {
                        jumpRight(rightNeighborBox, e);
                    }
                    return true;
                case Key.OemPeriod:
                case Key.Decimal:
                case Key.Space:
                    jumpRight(rightNeighborBox, e);
                    rightNeighborBox.SelectAll();
                    return true;
                default:
                    return false;
            }
        }

        private bool checkJumpLeft(TextBox currentBox, TextBox leftNeighborBox, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    if (currentBox.CaretIndex == 0 || currentBox.Text == "")
                    {
                        jumpLeft(leftNeighborBox, e);
                    }
                    return true;
                case Key.Back:
                    if ((currentBox.CaretIndex == 0 || currentBox.Text == "") && currentBox.SelectionLength == 0)
                    {
                        jumpLeft(leftNeighborBox, e);
                    }
                    return true;
                default:
                    return false;
            }
        }

        //discards non digits, prepares IPMaskedBox for textchange.
        private void handleTextInput(TextBox currentBox, TextBox rightNeighborBox, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(Convert.ToChar(e.Text)))
            {
                e.Handled = true;
                SystemSounds.Beep.Play();
                return;
            }

            if (currentBox.Text.Length == 3 && currentBox.SelectionLength == 0)
            {
                e.Handled = true;
                SystemSounds.Beep.Play();
                if (currentBox != fourthBox)
                {
                    rightNeighborBox.Focus();
                    rightNeighborBox.SelectAll();
                }
            }
        }

        //checks whether textbox content > 255 when 3 characters have been entered.
        //clears if > 255, switches to next textbox otherwise 
        private void handleTextChange(TextBox currentBox, TextBox rightNeighborBox)
        {
            if (currentBox.Text.Length == 3)
            {
                try
                {
                    Convert.ToByte(currentBox.Text);

                }
                catch (Exception exception) when (exception is FormatException || exception is OverflowException)
                {
                    currentBox.Clear();
                    currentBox.Focus();
                    SystemSounds.Beep.Play();
                    return;
                }
                if (currentBox.CaretIndex != 2 && currentBox != fourthBox)
                {
                    rightNeighborBox.CaretIndex = rightNeighborBox.Text.Length;
                    rightNeighborBox.SelectAll();
                    rightNeighborBox.Focus();
                }
            }
        }
        #endregion      

        #endregion


        #region Events
        //jump right, left or stay. 
        private void firstByte_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            checkJumpRight(firstBox, secondBox, e);
        }

        private void secondByte_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (checkJumpRight(secondBox, thirdBox, e))
                return;

            checkJumpLeft(secondBox, firstBox, e);
        }

        private void thirdByte_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (checkJumpRight(thirdBox, fourthBox, e))
                return;

            checkJumpLeft(thirdBox, secondBox, e);
        }

        private void fourthByte_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            checkJumpLeft(fourthBox, thirdBox, e);

            if (e.Key == Key.Space)
            {
                SystemSounds.Beep.Play();
                e.Handled = true;
            }
        }


        //discards non digits, prepares IPMaskedBox for textchange.
        private void firstByte_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            handleTextInput(firstBox, secondBox, e);
        }

        private void secondByte_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            handleTextInput(secondBox, thirdBox, e);
        }

        private void thirdByte_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            handleTextInput(thirdBox, fourthBox, e);
        }

        private void fourthByte_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            handleTextInput(fourthBox, fourthBox, e); //pass fourthbyte twice because no right neighboring box.
        }


        //checks whether textbox content > 255 when 3 characters have been entered.
        //clears if > 255, switches to next textbox otherwise 
        private void firstByte_TextChanged(object sender, TextChangedEventArgs e)
        {
            handleTextChange(firstBox, secondBox);
        }

        private void secondByte_TextChanged(object sender, TextChangedEventArgs e)
        {
            handleTextChange(secondBox, thirdBox);
        }

        private void thirdByte_TextChanged(object sender, TextChangedEventArgs e)
        {
            handleTextChange(thirdBox, fourthBox);
        }

        private void fourthByte_TextChanged(object sender, TextChangedEventArgs e)
        {
            handleTextChange(fourthBox, fourthBox);
        }

        #endregion
    }
}
