# Architecture

Assets/Plugins/MeteorLauncherCore validates and mounts externally supplied content. Assets/PortPreparation/Launcher provides import UI, controls and gyro. Assets/Meteor and Assets/Code retain upstream game logic and parsers. Assets/Editor/MeteorResourceBuild creates an empty resource-free entry scene and exports iOS. Third-party dependencies and all game content live outside this repository.
