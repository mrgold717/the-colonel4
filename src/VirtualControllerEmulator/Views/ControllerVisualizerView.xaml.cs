using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VirtualControllerEmulator.Services;

namespace VirtualControllerEmulator.Views;

public partial class ControllerVisualizerView : UserControl
{
    private InputMappingService? _mappingService;

    private static readonly SolidColorBrush ActiveBrush   = new(Color.FromRgb(0xE9, 0x45, 0x60));
    private static readonly SolidColorBrush InactiveBrush = new(Color.FromRgb(0x0F, 0x34, 0x60));

    // Special per-button inactive colors
    private static readonly Dictionary<string, SolidColorBrush> ButtonInactiveColors = new()
    {
        { "BtnA", new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0x33)) },
        { "BtnB", new SolidColorBrush(Color.FromRgb(0x66, 0x1A, 0x00)) },
        { "BtnX", new SolidColorBrush(Color.FromRgb(0x00, 0x33, 0x66)) },
        { "BtnY", new SolidColorBrush(Color.FromRgb(0x33, 0x66, 0x00)) },
    };

    public ControllerVisualizerView()
    {
        InitializeComponent();
    }

    public void Attach(InputMappingService mappingService)
    {
        if (_mappingService != null)
            _mappingService.ControllerStateChanged -= OnStateChanged;

        _mappingService = mappingService;
        if (_mappingService != null)
            _mappingService.ControllerStateChanged += OnStateChanged;
    }

    public void Detach()
    {
        if (_mappingService != null)
        {
            _mappingService.ControllerStateChanged -= OnStateChanged;
            _mappingService = null;
        }
    }

    private void OnStateChanged(object? sender, ControllerStateChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var s = e.State;
            UpdateButton(BtnA,     s.Buttons.GetValueOrDefault("A"),             ButtonInactiveColors["BtnA"]);
            UpdateButton(BtnB,     s.Buttons.GetValueOrDefault("B"),             ButtonInactiveColors["BtnB"]);
            UpdateButton(BtnX,     s.Buttons.GetValueOrDefault("X"),             ButtonInactiveColors["BtnX"]);
            UpdateButton(BtnY,     s.Buttons.GetValueOrDefault("Y"),             ButtonInactiveColors["BtnY"]);
            UpdateButton(BtnLB,    s.Buttons.GetValueOrDefault("LeftShoulder"),  InactiveBrush);
            UpdateButton(BtnRB,    s.Buttons.GetValueOrDefault("RightShoulder"), InactiveBrush);
            UpdateButton(BtnBack,  s.Buttons.GetValueOrDefault("Back"),          InactiveBrush);
            UpdateButton(BtnStart, s.Buttons.GetValueOrDefault("Start"),         InactiveBrush);
            UpdateButton(BtnGuide, s.Buttons.GetValueOrDefault("Guide"),         InactiveBrush);
            UpdateButton(BtnDUp,   s.Buttons.GetValueOrDefault("Up"),            InactiveBrush);
            UpdateButton(BtnDDown, s.Buttons.GetValueOrDefault("Down"),          InactiveBrush);
            UpdateButton(BtnDLeft, s.Buttons.GetValueOrDefault("Left"),          InactiveBrush);
            UpdateButton(BtnDRight,s.Buttons.GetValueOrDefault("Right"),         InactiveBrush);

            UpdateStickDot(LeftStickDot,  s.LeftStickX,  s.LeftStickY);
            UpdateStickDot(RightStickDot, s.RightStickX, s.RightStickY);

            LeftTriggerBar.Value  = s.LeftTrigger;
            RightTriggerBar.Value = s.RightTrigger;
        });
    }

    private static void UpdateButton(Border btn, bool active, SolidColorBrush inactiveBrush)
    {
        btn.Background = active ? ActiveBrush : inactiveBrush;
    }

    private static void UpdateStickDot(Ellipse dot, short axisX, short axisY)
    {
        const double canvasSize = 80;
        const double dotSize    = 16;
        const double center     = (canvasSize - dotSize) / 2;
        const double range      = (canvasSize - dotSize) / 2 - 2;

        double nx = axisX  / 32767.0;
        double ny = -axisY / 32767.0;

        Canvas.SetLeft(dot, center + nx * range);
        Canvas.SetTop(dot,  center + ny * range);
    }
}
