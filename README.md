# QstSelect

This application automatically selects the appropriate version of Quest for you.
There's a new (5.8 or later) and and old (5.7 or older) version.
They're partially incompatible, so it's kind of a hassle to work with them.

## Dependencies

You need 7-zip, either as installed version or as portable version.
[It can be obtained here for free](https://7-zip.org)

## Installation

1. Download the latest version from [Gitload](https://gitload.net/AyrA/QstSelect)
2. Copy the executable into an empty directory where it won't bother you.
3. Double click the executable to generate the configuration

## Manual Configuration

The executable assist you in creating the configuration, but you can also do so manually.

Open `config.txt` in a text editor to set the configuration.
You need to supply 3 executable paths:

1. **7z**: This is the path to 7z.exe (see Dependencies above)
2. **Quest.Old**: This is the old version of quest (5.7 or older)
3. **Quest.New**: This is the new version of quest (5.8 or later)

Please use full application path and do not use quotes around them.
The quickest way to obtain the full path of an executable is to browse to it using Windows Explorer,
then right click on the executable while holding shift,
then select "Copy as Path" from the menu.
Do not forget to remove the quotes when pasting into the config file.

## Registering File Type

1. Right click on a `.quest` game file and open the properties
2. Click the "Change" button
3. Select "More Apps", scroll to the end of the list, and click "Look for another app on this PC".
4. Browse to and select the `QstSelect` executable.
5. Click "OK"
6. Repeat for `.quest-save` files too.

## Using

*Note: Windows sometimes wants you to confirm your newly registered file types*

If you've registered the types as instructed above, just double click the file to open it.
