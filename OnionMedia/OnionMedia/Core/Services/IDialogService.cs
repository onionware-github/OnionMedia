/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using System.Threading.Tasks;

namespace OnionMedia.Core.Services;

public interface IDialogService
{
	/// <summary>
	/// Opens a dialog and lets the user select a folder.
	/// </summary>
	/// <returns>The path of the selected folder, null if the dialog get cancelled.</returns>
	Task<string> ShowFolderPickerDialogAsync();

	/// <summary>
	/// Opens a dialog and lets the user select a single file.
	/// </summary>
	/// <returns>The path of the selected file, null if the dialog get cancelled.</returns>
	Task<string> ShowSingleFilePickerDialogAsync();

	/// <summary>
	/// Opens a dialog and lets the user select multiple files.
	/// </summary>
	/// <returns>The paths of the selected files, null if the dialog get cancelled.</returns>
	Task<string[]> ShowMultipleFilePickerDialogAsync();

	/// <summary>
	/// Opens an information dialog with a title, a text and a close button.
	/// </summary>
	/// <param name="title">The title of the dialog.</param>
	/// <param name="content">The content of the dialog.</param>
	/// <param name="closeButtonText">The text for the close button.</param>
	Task ShowInfoDialogAsync(string title, string content, string closeButtonText);

	/// <summary>
	/// Shows a dialog with the given options.
	/// </summary>
	/// <param name="title">The title of the dialog.</param>
	/// <param name="content">The content of the dialog.</param>
	/// <param name="yesButtonText">The text for the "Yes" button.</param>
	/// <param name="noButtonText">The text for the "No" button.</param>
	/// <param name="cancelButtonText">The text for the "Cancel" button.</param>
	/// <returns>true if "yes" is selected, false if "no" is selected. Otherwhise null.</returns>
	Task<bool?> ShowInteractionDialogAsync(string title, string content, string yesButtonText, string noButtonText, string cancelButtonText);

	/// <summary>
	/// Shows a dialog with the given options.
	/// </summary>
	/// <param name="dialogTextOptions">Contains the information to display the dialog.</param>
	/// <returns>true if "yes" is selected, false if "no" is selected. Otherwhise null.</returns>
	Task<bool?> ShowDialogAsync(DialogTextOptions dialogTextOptions);
}

public class DialogTextOptions
{
	public string Title { get; set; }

	public string Content { get; set; }
	public TextWrapMode ContentTextWrapping { get; set; }


	/// <summary>The text for the "Close" button. null makes the button not available.</summary>
	public string CloseButtonText { get; set; }

	/// <summary>The text for the "Yes" button. null makes the button not available.</summary>
	public string YesButtonText { get; set; }

	/// <summary>The text for the "No" button. null makes the button not available.</summary>
	public string NoButtonText { get; set; }
}

public enum TextWrapMode
{
	NoWrap,
	Wrap,
	WrapWholeWords
}