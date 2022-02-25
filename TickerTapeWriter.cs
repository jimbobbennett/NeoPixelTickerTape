using System.Drawing;
using Iot.Device.Graphics;
using Iot.Device.Ws28xx;

namespace JimBobBennett.NeoPixelTickerTape
{
    /// <summary>
    /// Writes text to a 8x32 NeoPixel panel, such as this one:
    /// https://amzn.to/3sVjF7M (affiliate link).
    /// Only standard ASCII characters are supported, not unicode.
    /// </summary>
    public static class TickerTapeWriter
    {
        /// <summary>
        /// Writes a single letter to the panel starting at the given column, from 0 to 31 (this is for a 32 pixel wide panel).
        /// If the character is wider than the available space for the column, it gets cut off after 32 columns.
        /// For example, writing a character at column 28 will only show 4 columns of the 6 for the character.
        /// 
        /// You can start the character before column 0 to show later parts of the character, such as when scrolling
        /// a message
        /// </summary>
        /// <param name="neo">The NeoPixel panel</param>
        /// <param name="letter">The character to write</param>
        /// <param name="column">The start column</param>
        /// <param name="color">The color to use to write the letter</param>
        public static void WriteLetter(Ws2812b neo, char letter, int column, Color color)
        {
            // Get a bitmap image from the NeoPixel wrapper. This is a bitmap 256*1
            // as the panel is 256 pixels wrapped into a panel.
            var img = neo.Image;

            // Set all the pixels in front of the column to blank
            // This is required, you have to set all pixels for each image, otherwise
            // the first pixel set is the first pixel in the strip regardless of what position in the 
            // bitmap image you set.
            //
            // For example, if you don't do this and set pixel 128 to a color, then the first pixel in the
            // strip is lit up
            // Go figure 🤷
            for (var i = 0; i < column; ++i)
            {
                // Each column is 8 pixels tall
                for (var j = 0; j < 8; ++j)
                {
                    // Black is (0,0,0), so is the way to turn pixels off
                    img.SetPixel((i*8)+j, 0, Color.Black);
                }
            }

            // Write the letter
            WriteIndividualLetter(img, letter, column, color);

            // Send the update to light up the panel - the panel doesn't change until this is called.
            neo.Update();
        }

        /// <summary>
        /// Writes a letter to the panel at the given column
        /// </summary>
        /// <param name="neo">The NeoPixel panel</param>
        /// <param name="letter">The character to write</param>
        /// <param name="column">The start column</param>
        /// <param name="color">The color to use to write the letter</param>
        private static void WriteIndividualLetter(BitmapImage img, char letter, int column, Color color)
        {
            // Check the letter is supported by the font - if not don't write it
            if (!Fonts.Default.ContainsKey(letter))
            {
                return;
            }

            // Get the binary values for the pixel from the font
            var letterToPrint = Fonts.Default[letter];

            // The font is an array of binary values, each bit corresponds to a pixel on the panel. A 1 means light the pixel,
            // 0means don't light it
            foreach(var c in letterToPrint)
            {
                // Only write to columns 0-31, ignore all others such as because text is being scrolled
                // or is wider than the 32 columns
                if (column >= 32)
                {
                    break;
                }
                else if (column < 0)
                {
                    column++;
                    continue;
                }

                // Take a copy of the column data so we can bit shift it to get the pixel values
                var columnData = c;

                // The pixels in the panel are a continuous strip that loops up and down
                // For example, the first few columns are:
                //
                //  0   15   16   31
                //  1   14   17   30
                //  2   13   18   29
                //  3   12   19   28
                //  4   11   20   27
                //  5   10   21   26
                //  6    9   22   25
                //  7    8   23   24
                //
                // So the pixels start at the top of column 0 as 0, go down to 7, then back up the next column with 8 at the bottom, 15 at the top.
                // Then back down from 16 to 23 on the next column, back upwards for the next and so on
                // This means we need to know if a column is an up or down column - if it is up then we need to invert the letter bits otherwise
                // alternate columns will be upside down
                // Even numbered columns are down, odd are up
                var isDown = column % 2 == 0;

                // Down columns
                if (isDown)
                {
                    // For down columns we start at the bottom as we read the bits from 
                    // the least significant bit
                    // The bottom is the column * 8 (the size of the column), + 7 to get to the bottom.
                    // So for column 0 we start at pixel 7, column 2 we start at 23 etc.
                    var startPixel = (column * 8) + 7;

                    // Loop through all 8 bits in the column bits
                    for (var i = 0; i < 8; i++)
                    {
                        // CHeck the least significant bit - 1 means set the pixel, 0 means turn it off
                        if ((columnData & 1) > 0)
                        {
                            // The pixel to set is the start - the position as we are writing from the bottom
                            // Set that pixel to the provide color.
                            // SetPixel wants an x and y position, but NeoPixel strips are modeled as
                            // a 1 dimensional bitmap, so the y value is always 0
                            img.SetPixel(startPixel - i, 0, color);
                        }
                        else
                        {
                            // The pixel to clear is the start - the position as we are writing from the bottom
                            // Set that pixel to black - (0,0,0), turning the pixel off.
                            // SetPixel wants an x and y position, but NeoPixel strips are modeled as
                            // a 1 dimensional bitmap, so the y value is always 0
                            img.SetPixel(startPixel - i, 0, Color.Black);
                        }

                        // Bitshift, moving the next bit to the least significant bit
                        columnData = (byte)(columnData >> 1);
                    }
                }
                // up columns
                else
                {
                    // For up columns we start at the bottom as we read the bits from 
                    // the least significant bit
                    // The bottom is the column * 8 (the size of the column) as these go up.
                    // So for column 1 we start at pixel 8, column 3 we start at 24 etc.
                    var startPixel = column * 8;

                    // Loop through all 8 bits in the column bits
                    for (var i = 0; i < 8; i++)
                    {
                        // CHeck the least significant bit - 1 means set the pixel, 0 means turn it off
                        if ((columnData & 1) > 0)
                        {
                            // The pixel to set is the start + the position as we are writing from the bottom
                            // Set that pixel to the provide color.
                            // SetPixel wants an x and y position, but NeoPixel strips are modeled as
                            // a 1 dimensional bitmap, so the y value is always 0
                            img.SetPixel(startPixel + i, 0, color);
                        }
                        else
                        {
                            // The pixel to clear is the start + the position as we are writing from the bottom
                            // Set that pixel to black - (0,0,0), turning the pixel off.
                            // SetPixel wants an x and y position, but NeoPixel strips are modeled as
                            // a 1 dimensional bitmap, so the y value is always 0
                            img.SetPixel(startPixel + i, 0, Color.Black);
                        }

                        // Bitshift, moving the next bit to the least significant bit
                        columnData = (byte)(columnData >> 1);
                    }
                }

                // Move to the next column
                column++;
            }
        }

        /// <summary>
        /// Writes a string to the NeoPixel panel.
        /// If the text is too long, it gets cut off after 32 columns.
        /// 
        /// You can start the text before column 0 to show later parts of the text, such as when scrolling
        /// a message
        /// </summary>
        /// <param name="neo">The NeoPixel panel</param>
        /// <param name="text">The text to write to the panel</param>
        /// <param name="column">The start column</param>
        /// <param name="color">The color to use to write the text</param>
        public static void WriteText(Ws2812b neo, string text, int column, Color color)
        {
            // Get a bitmap image from the NeoPixel strip. This is a bitmap that is the total length of the strip by 1
            // So for a 8x32 panel, the bitmap is a strip 256 pixels long and 1 wide.
            var img = neo.Image;

            // Set all the pixels in front of the column to blank
            // This is required, you have to set all pixels for each image, otherwise
            // the first pixel set is the first pixel in the strip regardless of what position in the 
            // bitmap image you set.
            //
            // For example, if you don't do this and set pixel 128 to a color, then the first pixel in the
            // strip is lit up
            // Go figure 🤷
            for (var i = 0; i < column; ++i)
            {
                // Each column is 8 pixels tall
                for (var j = 0; j < 8; ++j)
                {
                    // Black is (0,0,0), so is the way to turn pixels off
                    img.SetPixel((i*8)+j, 0, Color.Black);
                }
            }

            // Loop through each character in the text to write them individually
            foreach (var c in text)
            {
                // Write the character at teh current column
                WriteIndividualLetter(img, c, column, color);

                // Shift the column by the width of each character - 6 columns
                column += 6;
            }

            // Send the update to light up the panel - the panel doesn't change until this is called.
            neo.Update();
        }

        /// <summary>
        /// An async method to scroll the given text across the NeoPixel panel.
        /// After the text has been fully scrolled, the panel is cleared.
        /// </summary>
        /// <param name="neo">The NeoPixel panel</param>
        /// <param name="text">The text to scroll across to the panel</param>
        /// <param name="color">The color to use to write the text</param>
        /// <param name="updateDelayMs">The delay in milliseconds between each frame of the scroll. Defaults to 30ms</param>
        /// <returns>A Task so this can be awaited</returns>
        public static async Task ScrollText(Ws2812b neo, string text, Color color, int updateDelayMs = 20)
        {
            // Get the total column length of the text - each character is 6 columns wide
            var textLength = text.Length * 6;

            // Start writing at column 31, the right-most column
            // Loop backwards from column 31 till the length past column 0, so the whole
            // text is scrolled from right to left.
            for (var column = 31; column >= 0 - textLength; --column)
            {
                // Write the text at the required column
                WriteText(neo, text, column, color);
                // Wait between each frame
                await Task.Delay(updateDelayMs);
            }

            // Once the text scrolling has finished, clear the panel
            // Get the bitmap image for the panel
            BitmapImage img = neo.Image;
            // Clear the image
            img.Clear();
            // Write the cleared image back to the panel
            neo.Update();
        }
    }
}