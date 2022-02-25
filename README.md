# NeoPixel Ticker Tape

A .NET library to show and scroll text on a 8x32 NeoPixel (WS2812b LED) panel controlled by a Raspberry Pi.

## Hardware

To use this, you will need the following hardware:

* A Raspberry Pi (any Pi that supports .NET will work, including the Pi Zero W 2)
* A 8x32 NeoPixel panel, such as [this panel on Amazon](https://amzn.to/3sVjF7M) - this is an affiliate link
* A 5V power supply for the panel (it draws too much power to be powered from the Pi)
* Jumper wires

## Pi setup

To set up your Pi and panel:

* Connect the 5V supply to the 5V and GND connections of the NeoPixel panel. These power connections are in the middle of the panel, and are not the input or output connections.

    ![A 5v power supply connected to the power wires on the middle of the panel](https://github.com/jimbobbennett/NeoPixelTickerTape/raw/main/img/power-connection.png)

* Connect the GND connection on the input side of the NeoPixel panel to a GPIO GND pin, for example the 3rd pin from the SD card end on the outside of the Pi.

* Connect the DIN connection on the input side of the NeoPixel panel to the GPIO10 pin (SPI COPI). This is the 10th pin from the SD card end on the inside of the Pi.

    ![A Pi Zero 2 W connected to the input pins](https://github.com/jimbobbennett/NeoPixelTickerTape/raw/main/img/control-connection.png)

* Ensure SPI is enabled, either by:

  * Turning SPI on in `raspi-config`
  * Turning SPI on in `/boot/config.txt`:

    ```ini
    dtparam=spi=on
    ```

* Fix the core clock to ensure SPI doesn't change speed by adding this to `/boot/config.txt`:

    ```ini
    core_freq=250
    core_freq_min=250
    ```

## Using the code

* Install the nuget package:

    ```sh
    dotnet add package JimBobBennett.NeoPixelTickerTape
    ```

* From your code, create a new `WS2812b` instance:

    ```csharp
    // Create connection settings to connect to the panel using SPI
    SpiConnectionSettings settings = new(0, 0)
    {
        ClockFrequency = 2_400_000,
        Mode = SpiMode.Mode0,
        DataBitLength = 8
    };

    // Create an SPI device
    SpiDevice spi = SpiDevice.Create(settings);

    // Use the SPI device to connect to the panel.
    // The 8x32 panel has 256 pixels in total
    var neoPixels = new Ws2812b(spi, 256);
    ```

* Use the static `JimBobBennett.NeoPixelTickerTape.TickerTapeWriter` class to write to the NeoPixel panel. This class has methods to write text to the panel. Note that only ASCII characters are supported, not unicode.

  * The `WriteLetter` method writes a single letter to the panel at the given column. The panel is 32 columns wide, with column numbers from 0-31. Each character is 6 columns wide (including a single blank line after to separate characters), and characters can be part way on the panel. You can write to any column, but you will only see letters written to columns -5 to 36 (with partial letters if you write from before 0 or past 26).

    `WriteLetter` clears all pixels to the left of the letter (this is due to how the pixels are written to in the .NET library), so if you need to write multiple letters, write from right to left.

    The `color` parameter sets the color to write. The `Color` class has ARGB values, but only the RGB are used - to reduce brightness, reduce the R, G, and B values accordingly.

  * The `WriteText` method writes a string to the panel starting at the given column. All text before column 0 and after column 31 is not shown.

  * The `ScrollText` method scrolls text like a ticker tape. The text scrolls in from the right hand side, and scrolls all the way off the left hand side.

* Sing *Ticker Tape Writer* to the tune of the Beatles *Paperback Writer*
