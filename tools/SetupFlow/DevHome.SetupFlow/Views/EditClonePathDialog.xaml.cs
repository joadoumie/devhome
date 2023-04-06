// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

/// <summary>
/// Dialog to handle changing the clone path in the repo review screen.
/// </summary>
public sealed partial class EditClonePathDialog
{
    private readonly ISetupFlowStringResource _stringResource;

    /// <summary>
    /// Gets or sets the view model to handle clone paths.
    /// </summary>
    public EditClonePathViewModel EditClonePathViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the view model that handles the dev drive.
    /// </summary>
    public EditDevDriveViewModel EditDevDriveViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the view model to handle the folder picker.
    /// </summary>
    public FolderPickerViewModel FolderPickerViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets a value indicating whether the Dev Drive checkmark was previously checked or unchecked.
    /// </summary>
    public bool PrevCheckBoxSelection
    {
        get; private set;
    }

    public EditClonePathDialog(IDevDriveManager devDriveManager, CloningInformation cloningInfo)
    {
        this.InitializeComponent();
        EditClonePathViewModel = new EditClonePathViewModel();
        EditDevDriveViewModel = new EditDevDriveViewModel(devDriveManager);
        FolderPickerViewModel = new FolderPickerViewModel();
        EditDevDriveViewModel.DevDriveClonePathUpdated += (_, updatedDevDriveRootPath) =>
        {
            FolderPickerViewModel.CloneLocationAlias = EditDevDriveViewModel.GetDriveDisplayName(DevDriveDisplayNameKind.FormattedDriveLabelKind);
            FolderPickerViewModel.CloneLocation = updatedDevDriveRootPath;
        };
        IsPrimaryButtonEnabled = FolderPickerViewModel.ValidateCloneLocation();
        if (cloningInfo.CloneToDevDrive)
        {
            AddDevDriveInfo();
        }

        FolderPickerViewModel.CloneLocation = cloningInfo.CloningLocation.FullName;
        _stringResource = Application.Current.GetService<ISetupFlowStringResource>();
        PrevCheckBoxSelection = DevDriveCheckBox.IsChecked.GetValueOrDefault(false);
        UpdateDialogState();
    }

    /// <summary>
    /// Open up folder picker.
    /// </summary>
    private async void ChooseCloneLocationButton_Click(object sender, RoutedEventArgs e)
    {
        await FolderPickerViewModel.ChooseCloneLocation();
        IsPrimaryButtonEnabled = FolderPickerViewModel.ValidateCloneLocation();
    }

    /// <summary>
    /// Adds or removes the default dev drive.  This dev drive will be made at the loading screen.
    /// </summary>
    private void MakeNewDevDriveComboBox_Click(object sender, RoutedEventArgs e)
    {
        // Getting here means
        // 1. The user does not have any existing dev drives
        // 2. The user wants to clone to a new dev drive.
        // 3. The user un-checked this and does not want a new dev drive.
        var isChecked = (sender as CheckBox).IsChecked;
        UpdateDialogState();
        if (EditClonePathViewModel.ShouldShowAreYouSureMessage)
        {
            return;
        }

        if (isChecked.Value)
        {
            AddDevDriveInfo();
        }
        else
        {
            RemoveDevDriveInfo();
        }
    }

    /// <summary>
    /// User wants to customize the default dev drive.
    /// </summary>
    private void CustomizeDevDriveHyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        EditDevDriveViewModel.PopDevDriveCustomizationAsync();
    }

    /// <summary>
    /// User left the clone location.  Validate the text.
    /// </summary>
    private void CloneLocationTextBox_TextChanged(object sender, RoutedEventArgs e)
    {
        IsPrimaryButtonEnabled = FolderPickerViewModel.ValidateCloneLocation();
    }

    /// <summary>
    /// Update dialog to show Dev Drive information.
    /// </summary>
    private void AddDevDriveInfo()
    {
        if (EditDevDriveViewModel.MakeDefaultDevDrive())
        {
            DevDriveCheckBox.IsChecked = true;
            FolderPickerViewModel.InDevDriveScenario = true;
            FolderPickerViewModel.CloneLocation = EditDevDriveViewModel.GetDriveDisplayName();
            FolderPickerViewModel.CloneLocationAlias = EditDevDriveViewModel.GetDriveDisplayName(DevDriveDisplayNameKind.FormattedDriveLabelKind);
            FolderPickerViewModel.DisableBrowseButton();
            PrevCheckBoxSelection = true;
        }
        else
        {
            // TODO: Add simple error Text in UI, e.g MakeDefaultDevDrive could return
            // the actual result and we could display the error text related to it from the .resw file.
        }
    }

    /// <summary>
    /// Used so we don't close the dialog when we're currently showing the user the warning message, and the user clicks the primary button.
    /// <remarks>
    /// Cancelling the button click args keeps the dialog alive without closing it. When the primary button is clicked we remove the Dev Drive info
    /// from the dialog and show the default dialog content with the textbox and browse button.
    /// </remarks>
    /// </summary>
    private void EditClonePathDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Checks if the previous checkmark value is true. If it is true and the current value is false, this means the user
        // unselected the checkmark, we are currently showing the warning message and the user has selected the primary button
        // to confirm they do not want to create a new Dev Drive.
        if (PrevCheckBoxSelection && !DevDriveCheckBox.IsChecked.GetValueOrDefault(false))
        {
            args.Cancel = true;
            PrevCheckBoxSelection = false;
            UpdateDialogState();
            RemoveDevDriveInfo();
            IsPrimaryButtonEnabled = false;
        }
    }

    /// <summary>
    /// Used so we don't close the dialog when we're showing the user the warning message, and the user clicks cancel.
    /// <remarks>
    /// Cancelling the button click args keeps the dialog alive without closing it. When cancelled we repopulate the Dev drive
    /// info back into the dialog, and recheck the checkmark.
    /// </remarks>
    /// </summary>
    private void EditClonePathDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Checks if the previous checkmark value is true. If it is true and the current value is false, this means the user
        // unselected the checkmark, we are currently showing the warning message and the user has selected the close button
        // to cancel out of the warning and back to the main edit clone path dialog.
        // This response means the user still wants to create the Dev Drive, so repopulate the data.
        if (PrevCheckBoxSelection && !DevDriveCheckBox.IsChecked.GetValueOrDefault(false))
        {
            args.Cancel = true;
            PrevCheckBoxSelection = true;
            DevDriveCheckBox.IsChecked = true;
            UpdateDialogState();
            AddDevDriveInfo();
        }
    }

    /// <summary>
    /// Used to update the title, primary button and closed button text of the dialog depending on the state of the Dev Drive checkmark.
    /// <remarks>
    /// State is based on the current and previous status of the checkbox. When previous state is true (checkmark checked) and the current
    /// state is false, that means the user unselected the checkmark. We show the user the a warning message in the dialog that the Dev Drive
    /// will not be created. All other states show the edit clone path dialog with the textbox and browse button.
    /// </remarks>
    /// </summary>
    public void UpdateDialogState()
    {
        CloseButtonText = _stringResource.GetLocalized(StringResourceKey.EditClonePathDialog + $"/CloseButtonText");

        if (PrevCheckBoxSelection && PrevCheckBoxSelection != DevDriveCheckBox.IsChecked.GetValueOrDefault(false))
        {
            Title = _stringResource.GetLocalized(StringResourceKey.EditClonePathDialogUncheckCheckMark + $"/Title");
            PrimaryButtonText = _stringResource.GetLocalized(StringResourceKey.EditClonePathDialogUncheckCheckMark + $"/PrimaryButtonText");
            EditClonePathViewModel.ShouldShowAreYouSureMessage = true;
            IsPrimaryButtonEnabled = true;
        }
        else
        {
            Title = _stringResource.GetLocalized(StringResourceKey.EditClonePathDialog + $"/Title");
            PrimaryButtonText = _stringResource.GetLocalized(StringResourceKey.EditClonePathDialog + $"/PrimaryButtonText");
            EditClonePathViewModel.ShouldShowAreYouSureMessage = false;
        }
    }

    /// <summary>
    /// Used to remove Dev Drive information from inside the dialog when user unchecks the Dev Drive checkmark
    /// </summary>
    public void RemoveDevDriveInfo()
    {
        EditDevDriveViewModel.RemoveNewDevDrive();
        FolderPickerViewModel.InDevDriveScenario = false;
        FolderPickerViewModel.CloneLocationAlias = string.Empty;
        FolderPickerViewModel.CloneLocation = string.Empty;
        FolderPickerViewModel.EnableBrowseButton();
    }
}