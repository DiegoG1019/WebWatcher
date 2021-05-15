# WebWatcher

WebWatcher is an app to periodically issue tasks related to watching a specific element, whether it's a site, or a local resource, and notify through a Telegram bot.

## Installation

Download this Repo, as well as [DGUtilities](https://github.com/DiegoG1019/DGUtilities) and place them adjacent to each other. Then, load the WebWatcher.sln in Visual Studio, it should be able to compile then. The project is oriented to being run as a Linux service, but can run on windows without any issues

## Usage

### First Run
In orden for a succesful project run, two things are needed:
- A `.config/settings.cfg.json` file. It will automatically be generated along with an exception the first time you run the program, fill out the fields, and run again.

- A `.data/allow.json` file. The program is designed in a way that, any user that is not in this file, will be entirely ignored by the bot. There is a permission hierarchy to choose from. The file itself should look like this
```json
[
  {
    "User": TelegramUserId,
    "Rights": 1-4
  }
]
```
Replace `TelegramUserId` with your user id, you can find it using the @raw_data_bot in telegram.
"Rights" should be a number between 1 and 4: 1:User, 2: Moderator, 3: Admin, 4: Creator. What these permissions mean is up to you. Currently, Only Admins and Creators can request the bot service to exit, and only the Creator can modify access using the /allow command.

Once the bot is up and running, type /help

### Extensions
Following an update to DGUtilities, the app should now support extension loading, though it's still largely untested, and no security measures are in place.
By default, all new extensions are found and added to `ExtensionEnable` in settings, and are, by default, set to `false`, meaning they won't be loaded. You can fix this by diving into your settings.

In order to compile a new extension, simply create a new project that references WebWatcher, build it (preferably in Release mode), dive into the `bin` folder and grab the .dll

Extensions are loaded at startup before commands and watchers are loaded, but after Serilog

### Output channels
- The log channel id is in the settings file and is defaulted to null. Meaning it will be disabled unless otherwise configured.

### Watch Routines
In order to make more Watch routines, simply create a class that implements IWebWatcher and decorate it with the `Watcher` attribute

### Bot commands
In order to make more Bot commands, simply make a new class that implements `IBotCommand` and decorate it with the `BotCommand` attribute.

You can even replace /help and /start, as all commands are loaded first and then, if they're not found, they're defaulted. If they're found, yours will be used instead.

### Run as a service
In order to run this app as a service, take a look at the `WebWatcher.service` systemd service file template
Commands:
1. `nano WebWatcher/WebWatcher.service` use `sudo` if doing it through another user, to edit the service file
2. `sudo chmod 7xx WebWatcher/DiegoG.WebWatcher` to make sure the file is executable
3. `sudo chown USERID -R WebWatcher` to make sure the entire directory is owned by the user owning the service
4. `sudo cp WebWatcher/WebWatcher.service /etc/systemd/system/` to throw the file into the service files directory
5. `sudo systemctl daemon-reload` to make sure your file is visible
6. `sudo systemctl start WebWatcher` to start the service
8. OPTIONAL: After a few seconds, `sudo systemctl status WebWatcher` to verify the status of the service.

Alternatively, you can also:
4. `sudo ln -s WebWatcher/WebWatcher.service /etc/systemd/system/WebWatcher.service` to create a symlink to the file. This way, if you make any changes, you won't have to `cp` again

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)

## My Extensions
These are extensions I made for my personal use of the WebWatcher. Check them out [here](https://github.com/DiegoG1019/WatcherExtensions)
