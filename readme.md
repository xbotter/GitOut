# GitOut 

This is a simple command line tool that can switch to the master/main branch and pull the latest code in one step, and switch to a new branch in one step.

## Installation

Requires .NET SDK 8.0 to be installed. 
Then you can install the tool using the following command:

```bash
dotnet tool install -g gitout
```

## Usage

```bash
gitout my-new-branch
```

This will switch to the main branch, pull the latest code, and then switch to the new branch `my-new-branch`.

If you want to switch to the other branch and pull the latest code, you can run:

```bash
gitout -m master my-new-branch
```

## License
MIT 

