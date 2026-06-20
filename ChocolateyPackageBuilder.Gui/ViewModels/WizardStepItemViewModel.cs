using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public sealed partial class WizardStepItemViewModel(string title, string subtitle, int index) : ViewModelBase
{
    public string Title { get; } = title;
    public string Subtitle { get; } = subtitle;
    public int Index { get; } = index;

    [ObservableProperty] private bool isComplete;
    [ObservableProperty] private bool isCurrent;
}
