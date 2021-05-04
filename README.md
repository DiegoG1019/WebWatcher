# WebWatcher

WebWatcher is an app to periodically issue tasks related to watching a specific element, whether it's a site, or a local resource, and notify through a Telegram bot.

## Installation

Download this Repo, as well as [DGUtilities](https://github.com/DiegoG1019/DGUtilities) and place them adjacent to each other. Then, load the WebWatcher.sln in Visual Studio, it should be able to compile then. The project is designed to run as a Linux service, but if you want to run it on Windows in release mode, simply comment out the `WriteTo.Syslog` line in `Program.cs`

## Usage

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

In order to make more Watch routines, simply create a class that implements IWebWatcher and register it along the others in `Program.cs`
(I'm in the works of making it attribute reflection based, but that's on hold for now)

In order to make more Bot commands, simply make a new class that implements `IBotCommand` and decorate it with the `BotCommand` attribute. Nothing else needs to be done. You can even replace /help and /start, as all commands are loaded first and then, if they're not found, they're defaulted. If they're found, yours will be used instead.

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)