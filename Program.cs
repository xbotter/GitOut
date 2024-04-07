using System.CommandLine;
using System.Diagnostics;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

const string defaultBranch = "`default`";

var mainBranchOption = new Option<string>(
    ["--main-branch", "-m"],
    getDefaultValue: () => defaultBranch,
    description: "The main branch name"
);

var forceCheckout = new Option<bool>(
    ["--force", "-f"],
    getDefaultValue: () => false,
    description: "Force checkout"
);

var newBranchArgument = new Argument<string>(
    "new branch name",
    description: "The new branch name"
);

var cmd = new RootCommand
{
    mainBranchOption,
    forceCheckout,
    newBranchArgument
};

var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger("git-out");

cmd.SetHandler(MainHandle, newBranchArgument, mainBranchOption, forceCheckout);

await cmd.InvokeAsync(args);

async void MainHandle(
            string newBranch,
            string mainBranch,
            bool forceCheckout)
{

    var currentDir = Environment.CurrentDirectory;

    if (!Repository.IsValid(currentDir))
    {
        logger.LogError("Not a git repository");
        return;
    }

    var repo = new Repository(currentDir);

    if (repo.RetrieveStatus().IsDirty && !forceCheckout)
    {
        logger.LogError("Repository has uncommitted changes");
        return;
    }

    var remote = repo.Network.Remotes["origin"];

    if (remote == null)
    {
        logger.LogError("No remote named 'origin'");
        return;
    }


    if (mainBranch == defaultBranch)
    {
        var mBranch = repo.Branches.Where(_ => _.IsRemote)
                            .FirstOrDefault(_ => _.CanonicalName.EndsWith("/main") || _.CanonicalName.EndsWith("/master"));
        if (mBranch == null)
        {
            logger.LogError("No main/master branch specified");
            mainBranch = "master";
        }
        else
        {
            mainBranch = mBranch.CanonicalName.Split('/')[^1];
        }
    }

    logger.LogInformation($"Fetching origin {mainBranch} branch");

    await ExecuteGitCommandAsync("fetch", $"origin +refs/heads/{mainBranch}:refs/remotes/origin/{mainBranch}");

    var mainBranchRef = repo.Branches[$"remotes/origin/{mainBranch}"];

    var commit = mainBranchRef.Tip;

    logger.LogInformation($"Creating new branch {newBranch}");
    var branch = repo.CreateBranch(newBranch, commit);

    var checkoutOptions = new CheckoutOptions
    {
        CheckoutModifiers = CheckoutModifiers.Force
    };

    logger.LogInformation($"Checking out {newBranch}");
    Commands.Checkout(repo, branch, checkoutOptions);
}

async Task ExecuteGitCommandAsync(string command, string args)
{
    using (Process process = new Process())
    {
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = $"{command} {args}";

        logger.LogInformation($"Executing: git {command} {args}");

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            logger.LogError(error);
            Environment.Exit(process.ExitCode);
        }
        else
        {
            logger.LogInformation(output);
        }
    }
}