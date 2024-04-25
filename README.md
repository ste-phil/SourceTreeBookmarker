# SourceTree bookmarking utility

Have you ever been anoyed that SourceTree (https://www.sourcetreeapp.com/) didn't register repositories cloned via the terminal. 
I was. This tool lets you register it in sourcetree via a simple command after you have finished cloning.

Usage:
* Download app from Github release
* Register stcli.exe in path (see https://stackoverflow.com/questions/4822400/register-an-exe-so-you-can-run-it-from-any-command-line-in-windows)
* Restart terminal
* Run for a git repository folder ``sticli -f <folder-to-your-git-repo>``
* Run for a folder that contains multiple git repositories  ``sticli -r -f <folder-that-contains-many-repos>``