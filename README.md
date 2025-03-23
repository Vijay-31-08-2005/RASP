# Rasp - Remote Access Storage Pool

Rasp is a lightweight version control system designed for efficient local and remote file tracking. It allows developers to manage changes, commit updates, and integrate with Azure for cloud storage.

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

Now you can use `rasp` globally

## Usage

```
rasp <command> [options]
```

### Available Commands

#### Repository Management

- `init`               - Initialize a new Rasp repository
- `drop`               - Delete the current Rasp repository

#### File Operations

- `add`                - Add a file to the staging area
- `commit`             - Commit staged changes with a message
- `revert`             - Revert a specific file in the staging area
- `mv`                 - Move a file
- `cp`                 - Copy a file
- `delete, -d`         - Delete a file, description, or branch

#### Branching

- `branch`             - Create a new branch
- `checkout`           - Switch to a specified branch
- `merge`              - Merge a specified branch into the current branch

#### Remote Operations

- `clone`              - Clone a repository
- `push`               - Upload files to the Azure server
- `pull`               - Download files from the Azure server
- `set`                - Configure the Azure connection string for remote storage

#### Information & Logs

- `logs`               - Show the history of repository actions
- `status`             - Show the current repository status
- `history`            - Show current branch's commit history

### Options & Flags

- `-h, --help`         - Show help for a command
- `-v, --version`      - Display the current version of Rasp
- `-m`                 - Specify a commit message
- `-p`                 - Set user profile details
- `-i`                 - Activate shell mode
- `-o`                 - Deactivate shell mode
- `-l`                 - List existing branches
- `-rb, rollback`      - Roll back the latest commit

## Examples

Initialize a new repository:

```
rasp init
```

Add and commit files:

```
rasp add myfile.txt
rasp commit -m "Initial commit"
```

Push changes to Azure:

```
rasp push
```

Rollback the latest commit:

```
rasp rollback
```

For more details, run:

```
rasp <command> --help
```

## License

Rasp is an open-source project. You are free to modify and distribute it under the MIT License.

## Author

Created by **Kisetsu**.

## Contributing

Feel free to contribute by submitting pull requests, reporting issues, or suggesting improvements.

---



