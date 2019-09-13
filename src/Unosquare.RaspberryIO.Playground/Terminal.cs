using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosquare.RaspberryIO.Playground
{
    public static class Terminal
    {
        static Terminal()
        {
            OutputEncoding = Console.OutputEncoding;
            BorderColor = ConsoleColor.Green;
        }
        public static Encoding OutputEncoding
        {
            get;
            set;
        }

        public static ConsoleColor BorderColor
        {
            get;
            set;
        }

        internal static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Creates a table prompt where the user can enter an option based on the options dictionary provided.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="options">The options.</param>
        /// <param name="anyKeyOption">Any key option.</param>
        /// <returns>A value that identifies the console key that was pressed.</returns>
        public static ConsoleKeyInfo ReadPrompt(
            string title,
            IDictionary<ConsoleKey, string> options,
            string anyKeyOption)
        {
            const ConsoleColor textColor = ConsoleColor.White;
            var lineLength = Console.LargestWindowWidth;
            var lineAlign = -(lineLength - 2);
            var textFormat = "{0," + lineAlign + "}";

            // lock the output as an atomic operation
            {
                {
                    // Top border
                    Table.TopLeft();
                    Table.Horizontal(-lineAlign);
                    Table.TopRight();
                }

                {
                    // Title
                    Table.Vertical();
                    var titleText = string.Format(CultureInfo.CurrentCulture, 
                        textFormat,
                        string.IsNullOrWhiteSpace(title) ? " Select an option from the list below." : $" {title}");
                    Write(titleText, textColor);
                    Table.Vertical();
                }

                {
                    // Title Bottom
                    Table.LeftTee();
                    Table.Horizontal(-lineAlign);
                    Table.RightTee();
                }

                // Options
                foreach (var kvp in options)
                {
                    Table.Vertical();
                    Write(string.Format(CultureInfo.CurrentCulture, textFormat,
                        $"    {"[ " + kvp.Key + " ]",-10}  {kvp.Value}"), textColor);
                    Table.Vertical();
                }

                // Any Key Options
                if (string.IsNullOrWhiteSpace(anyKeyOption) == false)
                {
                    Table.Vertical();
                    Write(string.Format(CultureInfo.CurrentCulture, textFormat, " "), ConsoleColor.Gray);
                    Table.Vertical();

                    Table.Vertical();
                    Write(string.Format(CultureInfo.CurrentCulture, textFormat,
                        $"    {" ",-10}  {anyKeyOption}"), ConsoleColor.Gray);
                    Table.Vertical();
                }

                {
                    // Input section
                    Table.LeftTee();
                    Table.Horizontal(-lineAlign);
                    Table.RightTee();

                    Table.Vertical();
                    Write(string.Format(CultureInfo.CurrentCulture, textFormat, " Option:" ), ConsoleColor.Green);
                    Table.Vertical();

                    Table.BottomLeft();
                    Table.Horizontal(-lineAlign);
                    Table.BottomRight();
                }
            }

            var userInput = Console.ReadKey(true);
            Write(userInput.Key.ToString(), ConsoleColor.Gray);

            return userInput;
        }

        /// <summary>
        /// Writes a character a number of times, optionally adding a new line at the end.
        /// </summary>
        /// <param name="charCode">The character code.</param>
        /// <param name="color">The color.</param>
        /// <param name="count">The count.</param>
        /// <param name="newLine">if set to <c>true</c> [new line].</param>
        public static void Write(byte charCode, ConsoleColor? color = null, int count = 1, bool newLine = false)
        {
            {
                var bytes = new byte[count];
                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = charCode;
                }

                if (newLine)
                {
                    var newLineBytes = OutputEncoding.GetBytes(Environment.NewLine);
                    bytes = bytes.Union(newLineBytes).ToArray();
                }

                var buffer = Encoding.ASCII.GetChars(bytes);
                Console.Write(buffer);
            }
        }

        public static void Write(char code, ConsoleColor? color = null, int count = 1, bool newLine = false)
        {
            Console.Write(new String(code, count));
        }

        /// <summary>
        /// Writes a character a number of times, optionally adding a new line at the end.
        /// </summary>
        /// <param name="text">The characters.</param>
        /// <param name="color">The color.</param>
        public static void Write(string text, ConsoleColor? color = null)
        {
            Console.Write(text);       
        }

        /// <summary>
        /// Represents a Table to print in console.
        /// </summary>
        private static class Table
        {
            public static void Vertical() => Write('\u2502', BorderColor);

            public static void RightTee() => Write('\u2524', BorderColor);

            public static void TopRight() => Write('\u2510', BorderColor);

            public static void BottomLeft() => Write('\u2514', BorderColor);

            public static void BottomTee() => Write('\u2534', BorderColor);

            public static void TopTee() => Write('\u252c', BorderColor);

            public static void LeftTee() => Write('\u251c', BorderColor);

            public static void Horizontal(int length) => Write('\u2500', BorderColor, length);

            public static void Tee() => Write((byte)197, BorderColor);

            public static void BottomRight() => Write('\u2518', BorderColor);

            public static void TopLeft() => Write('\u250C', BorderColor);
        }
    }
}
