using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChocolateyPackageBuilder.Gui.ViewModels;

public partial class AppStatusViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Detail { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IsBusy { get; set; }

    [ObservableProperty] public partial bool IsError { get; set; }

    [ObservableProperty] public partial DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.Now;

    [ObservableProperty] public partial string Message { get; set; } = "Ready";

    public void SetReady(string detail = "")
    {
        Set("Ready", detail, false, false);
    }

    public void SetBusy(string message, string detail = "")
    {
        Set(message, detail, true, false);
    }

    public void SetSuccess(string message, string detail = "")
    {
        Set(message, detail, false, false);
    }

    public void SetError(string message, string detail = "")
    {
        Set(message, detail, false, true);
    }

    private void Set(string message, string detail, bool isBusy, bool isError)
    {
        Message = message;
        Detail = detail;
        IsBusy = isBusy;
        IsError = isError;
        LastUpdated = DateTimeOffset.Now;
    }
}