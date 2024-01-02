Run below command to check what you can do with scripts:
    
    dotnet fsi build.fsx -- -h

## Dev

### Requires

- dotnet 8 sdk
- nodejs 20 or above if you want to rebuild the style

Open terminal and run
    
    dotnet fsi build.fsx -- -p dev

> You can change or copy **appsettings.json** file in **src\Memory** folder and name it **appsettings.Development.json** and override related fields according to your needs.

## Publish

    dotnet fsi build.fsx -- -p publish --platform win64

Then go to the **publish-xxx** folder, you can copy and paste to related platform to run.

> You can change or copy **appsettings.json** file in **publish-xxx** folder and name it **appsettings.Production.json** and override related fields according to your needs.


## Tag memory by face

- Configure the **FamiliarFacesFolder** in the settings
- Put images with only one face to the folder, the file name will be used as the tag. You can name it like **some name.png**

> Currently only support win.x64, linux.arm64, ubuntu.20.04.x64


## appsettings.xxx.json

You can create this file to override some settings.

- Version

    Sometimes, you want to reset the cache for the browser after you delete the local cache or db and want to rebuild all the memories, you can upgrade this property.


- Theme

    Set your own theme, all the theme can be found in this [list](https://daisyui.com/docs/themes).

- Users

    This fields can help you to set initial users with some password which can be reset by the specific user in the login page. Service will only pick new users from this list when start, it will never change or delete any users.

- FFmpegBinFolder

    Use ffmpeg to process videos, so we need do download related files (-shared) from [here](https://github.com/BtbN/FFmpeg-Builds/releases).

    Download related platform's files and locate to the bin folder.
