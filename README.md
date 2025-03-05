# Rasp - Remote Access Storage Pool

## Overview

**Rasp** is a lightweight command-line tool designed for managing files and GitHub repositories efficiently. It allows users to move, copy, delete files, display messages, and clone repositories with simple commands.

## Installation

### Windows

1. **Download and Extract**

   - Download the compiled `rasp.exe` file and move it to `C:\Program Files\RASP\`.

2. **Add to System Path**

   - Open `System Properties` (`Win + R`, type `sysdm.cpl`, press `Enter`).
   - Go to the **Advanced** tab and click **Environment Variables**.
   - Under **System Variables**, find `Path`, click **Edit**, and add:
     ```
     C:\Program Files\Rasp\
     ```
   - Click **OK** and restart the command prompt.

### Linux/Mac

1. Move the executable to `/usr/local/bin/`:
   ```sh
   sudo mv rasp /usr/local/bin/
   ```
2. Make it executable:
   ```sh
   chmod +x /usr/local/bin/rasp
   ```

Now you can use `rasp` globally.

## Usage

Run the following command to see available options:

```sh
rasp --help
```

### Commands

| Command                                                                      | Description                               |
| ---------------------------------------------------------------------------- | ----------------------------------------- |
| `rasp display <message>`                                                     | Displays a custom message                 |
| `rasp --help`                                                                 | Shows help information                    |
| `rasp readme`                                                                | Displays the README                       |
| `rasp --version`                                                             | Displays the version                      |
| `rasp move <source> <destination>`                                           | Moves a file                              |
| `rasp copy <source> <destination>`                                           | Copies a file                             |
| `rasp delete <file>`                                                         | Deletes a file                            |
| `rasp clone <username> <repo> <directory>`                                   | Clones a GitHub repository                |
| `rasp upload <containerName> <connectionString> <filePath>`                  | Uploads a file to Azure Blob Storage      |
| `rasp download <blobName> <containerName> <connectionString> <directory>`    |  Downloads a blob from Azure Blob Storage |

## Example Usage

### Move a File

```sh
rasp move C:\Users\User\Desktop\file.txt D:\Backup\
```

### Copy a File

```sh
rasp copy C:\Users\User\file.txt C:\Users\User\Documents\
```

### Delete a File

```sh
rasp delete C:\Users\User\oldfile.txt
```

### Clone a Repository

```sh
rasp clone octocat Hello-World C:\Projects\
```

### Uploads a File

```sh
rasp upload container conn%asdfawe C:\Projects\file.txt
```

### Downloads a File

```sh
rasp download blob container conn%asdfawe C:\Projects\folder
```

## License

This project is licensed under the MIT License.

## Author

Created by **Kisetsu**.

