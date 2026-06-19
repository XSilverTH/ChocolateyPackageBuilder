using System;
using ChocolateyPackageBuilder.Gui.Features.PackageBuilder;
using Xunit;

namespace ChocolateyPackageBuilder.Tests;

public class PackageBuilderViewModelTests
{
    [Fact]
    public void PackageNameChange_WhenDescriptionIsEmpty_SetsDefaultDescription()
    {
        // Arrange
        var viewModel = new PackageBuilderViewModel(null!);
        viewModel.Description = string.Empty;

        // Act
        viewModel.PackageName = "my-package";

        // Assert
        Assert.Equal("Chocolatey package for my-package.", viewModel.Description);
    }

    [Fact]
    public void PackageNameChange_WhenDescriptionIsDefaultDescription_UpdatesToNewDefaultDescription()
    {
        // Arrange
        var viewModel = new PackageBuilderViewModel(null!);
        viewModel.PackageName = "first-package";
        
        // Ensure it is set to the default description
        Assert.Equal("Chocolatey package for first-package.", viewModel.Description);

        // Act
        viewModel.PackageName = "second-package";

        // Assert
        Assert.Equal("Chocolatey package for second-package.", viewModel.Description);
    }

    [Fact]
    public void PackageNameChange_WhenDescriptionIsCustom_DoesNotOverwriteCustomDescription()
    {
        // Arrange
        var viewModel = new PackageBuilderViewModel(null!);
        viewModel.PackageName = "some-package";
        viewModel.Description = "This is a very special custom description.";

        // Act
        viewModel.PackageName = "another-package";

        // Assert
        Assert.Equal("This is a very special custom description.", viewModel.Description);
    }

    [Fact]
    public void PackageNameChange_WhenNewValueIsEmpty_ResetsDefaultDescriptionToEmpty()
    {
        // Arrange
        var viewModel = new PackageBuilderViewModel(null!);
        viewModel.PackageName = "some-package";
        Assert.Equal("Chocolatey package for some-package.", viewModel.Description);

        // Act
        viewModel.PackageName = string.Empty;

        // Assert
        Assert.Equal(string.Empty, viewModel.Description);
    }
}
