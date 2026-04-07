using Avalonia;
using Avalonia.Controls;
using enBot.Models;
using System.Collections.Generic;

namespace enBot.Views;

public partial class InlineTextControl : UserControl
{
    public static readonly StyledProperty<List<InlineSegment>> SegmentsProperty =
        AvaloniaProperty.Register<InlineTextControl, List<InlineSegment>>(nameof(Segments), []);

    public List<InlineSegment> Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public InlineTextControl()
    {
        InitializeComponent();
    }
}
