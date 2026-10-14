using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DigitalMatrixClock;

public partial class MainPage : ContentPage
{
    private readonly ClockDrawable _clockDrawable = new();
    private IDispatcherTimer _timer;

    public MainPage()
    {
        InitializeComponent();

        ClockCanvas.Drawable = _clockDrawable;

        // Таймер для обновления экрана каждую секунду
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            _clockDrawable.CurrentTime = DateTime.Now;
            ClockCanvas.Invalidate(); // Принудительная перерисовка холста
        };
        _timer.Start();
    }
}

public class ClockDrawable : IDrawable
{
    public DateTime CurrentTime { get; set; } = DateTime.Now;

    // Матричные шрифты 5x7 для цифр 0-9 (1 = пиксель включен, 0 = выключен)
    private static readonly Dictionary<char, byte[,]> FontPatterns = new()
    {
        ['0'] = new byte[,] {
            {1,1,1,1,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,1,1,1,1}
        },
        ['1'] = new byte[,] {
            {0,0,1,0,0},
            {0,1,1,0,0},
            {0,0,1,0,0},
            {0,0,1,0,0},
            {0,0,1,0,0},
            {0,0,1,0,0},
            {0,1,1,1,0}
        },
        ['2'] = new byte[,] {
            {1,1,1,1,1},
            {0,0,0,0,1},
            {0,0,0,0,1},
            {1,1,1,1,1},
            {1,0,0,0,0},
            {1,0,0,0,0},
            {1,1,1,1,1}
        },
        ['3'] = new byte[,] {
            {1,1,1,1,1},
            {0,0,0,0,1},
            {0,0,0,0,1},
            {1,1,1,1,1},
            {0,0,0,0,1},
            {0,0,0,0,1},
            {1,1,1,1,1}
        },
        ['4'] = new byte[,] {
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,1,1,1,1},
            {0,0,0,0,1},
            {0,0,0,0,1},
            {0,0,0,0,1}
        },
        ['5'] = new byte[,] {
            {1,1,1,1,1},
            {1,0,0,0,0},
            {1,0,0,0,0},
            {1,1,1,1,1},
            {0,0,0,0,1},
            {0,0,0,0,1},
            {1,1,1,1,1}
        },
        ['6'] = new byte[,] {
            {1,1,1,1,1},
            {1,0,0,0,0},
            {1,0,0,0,0},
            {1,1,1,1,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,1,1,1,1}
        },
        ['7'] = new byte[,] {
            {1,1,1,1,1},
            {0,0,0,0,1},
            {0,0,0,1,0},
            {0,0,1,0,0},
            {0,1,0,0,0},
            {0,1,0,0,0},
            {0,1,0,0,0}
        },
        ['8'] = new byte[,] {
            {1,1,1,1,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,1,1,1,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,1,1,1,1}
        },
        ['9'] = new byte[,] {
            {1,1,1,1,1},
            {1,0,0,0,1},
            {1,0,0,0,1},
            {1,1,1,1,1},
            {0,0,0,0,1},
            {0,0,0,0,1},
            {1,1,1,1,1}
        },
        [':'] = new byte[,] {
            {0,0},
            {1,1},
            {1,1},
            {0,0},
            {1,1},
            {1,1},
            {0,0}
        }
    };

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.Black;
        canvas.FillRectangle(dirtyRect);

        string timeString = CurrentTime.ToString("HH:mm:ss");

        // Расчет общей ширины матричной строки
        int totalCols = 0;
        foreach (char c in timeString)
        {
            if (FontPatterns.TryGetValue(c, out var pattern))
            {
                totalCols += pattern.GetLength(1) + 1; // ширина символа + отступ
            }
        }

        // Автомасштабирование под экран (портретный или альбомный)
        float padding = 24f;
        float availableWidth = dirtyRect.Width - (padding * 2);
        float availableHeight = dirtyRect.Height - (padding * 2);

        float dotSize = Math.Min(availableWidth / (totalCols * 1.3f), availableHeight / (7 * 1.3f));
        float dotSpacing = dotSize * 0.28f;
        float step = dotSize + dotSpacing;

        float clockPixelWidth = (totalCols * step);
        float clockPixelHeight = (7 * step);

        // Центрирование часов на холсте
        float startX = (dirtyRect.Width - clockPixelWidth) / 2f;
        float startY = (dirtyRect.Height - clockPixelHeight) / 2f;

        float cursorX = startX;
        var activeDotColor = Microsoft.Maui.Graphics.Color.FromRgb(0x00, 0xE5, 0xFF);   // Неоновый циан
        var inactiveDotColor = Microsoft.Maui.Graphics.Color.FromRgb(0x09, 0x22, 0x2B); // Приглушенный фон

        foreach (char c in timeString)
        {
            if (!FontPatterns.TryGetValue(c, out var pattern))
                continue;

            int rows = pattern.GetLength(0);
            int cols = pattern.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int col = 0; col < cols; col++)
                {
                    bool isActive = pattern[r, col] == 1;

                    canvas.FillColor = isActive ? activeDotColor : inactiveDotColor;

                    float x = cursorX + (col * step);
                    float y = startY + (r * step);

                    canvas.FillRoundedRectangle(x, y, dotSize, dotSize, dotSize * 0.2f);
                }
            }

            cursorX += (cols + 1) * step;
        }
    }
}