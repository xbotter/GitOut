using System.Diagnostics;
using System.CommandLine;
using LibGit2Sharp;

var mainBranchOption = new Option<string>(
    ["--main-branch", "-m"],
    getDefaultValue: () => "main",
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


cmd.SetHandler(MainHandle,newBranchArgument,mainBranchOption, forceCheckout);

await cmd.InvokeAsync(args);

/*
cmd.SetHandler((mainBranch,newBranch) =>
{
    git fetch origin");
    git checkout {mainBranch}");
    git pull origin {mainBranch}");
    git checkout -b {newBranch}");
},mainBranchOption, newBranchArgument);
*/
static void MainHandle(
            string newBranch,
            string mainBranch,
            bool forceCheckout  )
{
    var currentDir = Environment.CurrentDirectory;

    if (!Repository.IsValid(currentDir))
    {
        Console.Error.WriteLine("Not a git repository");
        return;
    }

    var repo = new Repository(currentDir);

    if(repo.RetrieveStatus().IsDirty && !forceCheckout)
    {
        Console.Error.WriteLine("Repository has uncommitted changes");
        return;
    }

    var remote = repo.Network.Remotes["origin"];

    if (remote == null)
    {
        Console.Error.WriteLine("No remote named 'origin'");
        return;
    }

    var options = new FetchOptions
    {
        TagFetchMode = TagFetchMode.None
    };

    Commands.Fetch(repo, "origin", [$"+refs/heads/{mainBranch}:refs/remotes/origin/{mainBranch}"], options, $"Fetching origin {mainBranch} branch");
    
    var mainBranchRef = repo.Branches[$"remotes/origin/{mainBranch}"];

    var commit = mainBranchRef.Tip;

    var branch = repo.CreateBranch(newBranch, commit);

    var checkoutOptions = new CheckoutOptions
    {
        CheckoutModifiers = CheckoutModifiers.Force
    };

    Commands.Checkout(repo, branch, checkoutOptions);
}
