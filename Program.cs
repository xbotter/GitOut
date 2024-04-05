using System.Diagnostics;
using System.CommandLine;

var mainBranchOption = new Option<string>(
    ["--main-branch","-m"],
    getDefaultValue: () => "main",
    description: "The main branch name"
);

var newBranchArgument = new Argument<string>(
    "new branch name",
    description: "The new branch name"
);

var cmd = new RootCommand
{
    mainBranchOption,
    newBranchArgument
};


cmd.SetHandler((mainBranch,newBranch) =>
{
    ExecuteGitCommand("--version");
    ExecuteGitCommand("fetch origin");
    ExecuteGitCommand($"checkout {mainBranch}");
    ExecuteGitCommand($"pull origin {mainBranch}");
    ExecuteGitCommand($"checkout -b {newBranch}");
},mainBranchOption, newBranchArgument);


await cmd.InvokeAsync(args);

static void ExecuteGitCommand(string command)
{
    using (Process process = new Process())
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = command;

        Console.WriteLine($"Executing: git {command}");

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine(error);
            Environment.Exit(process.ExitCode);
        }
        else
        {
            Console.WriteLine(output);
        }
    }
}