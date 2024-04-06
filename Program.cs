using System.CommandLine;
using System.Text.RegularExpressions;
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


cmd.SetHandler(MainHandle, newBranchArgument, mainBranchOption, forceCheckout);

await cmd.InvokeAsync(args);

static void MainHandle(
            string newBranch,
            string mainBranch,
            bool forceCheckout)
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger("git-out");
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

    var options = new FetchOptions
    {
        TagFetchMode = TagFetchMode.None
    };

    if (mainBranch == defaultBranch)
    {
        var mBranch = repo.Branches.Where(_ => _.IsRemote)
                            .FirstOrDefault(_ => _.CanonicalName.EndsWith("main") || _.CanonicalName.EndsWith("master"));
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

    Commands.Fetch(repo, "origin", [$"+refs/heads/{mainBranch}:refs/remotes/origin/{mainBranch}"], options, $"Fetching origin {mainBranch} branch");

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
