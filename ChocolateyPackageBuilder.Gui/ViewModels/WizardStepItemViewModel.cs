using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public sealed partial class WizardStepItemViewModel(string title, string subtitle, int index) : ViewModelBase
{
    [ObservableProperty] public partial bool IsComplete { get; set; }

    [ObservableProperty] public partial bool IsCurrent { get; set; }

    public string Title { get; } = title;
    public string Subtitle { get; } = subtitle;
    public int Index { get; } = index;
}