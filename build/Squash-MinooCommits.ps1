<#
.SYNOPSIS
Squashes the current branch from the first Kiarash Minoo or Ahmad Minoo
commit through HEAD.

.DESCRIPTION
Searches both author and committer names, selects the earliest matching
commit in the current branch, and stages the inclusive squash while preserving
the current file tree. The command requires a clean worktree. By default it
leaves the staged result for manual review, commit, and push. Specify Commit to
create the squash commit automatically, reusing the current HEAD commit message
unless MessageFile is supplied. This command never pushes automatically.

.EXAMPLE
.\build\Squash-MinooCommits.ps1

.EXAMPLE
.\build\Squash-MinooCommits.ps1 -Commit -MessageFile .\commit-message.txt -Force
#>
[CmdletBinding()]
param(
    [string[]] $Names = @('Kiarash Minoo', 'Ahmad Minoo'),
    [string] $MessageFile,
    [switch] $Commit,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'

function Invoke-Git {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    $output = & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }

    return $output
}

$repositoryRoot = Invoke-Git -Arguments @('rev-parse', '--show-toplevel')
Push-Location $repositoryRoot

try {
    if ($MessageFile -and -not $Commit) {
        throw 'MessageFile can only be used together with the Commit switch.'
    }

    $status = Invoke-Git -Arguments @('status', '--porcelain', '--untracked-files=normal')
    if ($status) {
        throw 'The worktree is not clean. Commit or stash all changes before squashing history.'
    }

    if ($MessageFile) {
        $MessageFile = (Resolve-Path -LiteralPath $MessageFile).Path
    }

    $firstMatch = Invoke-Git -Arguments @(
        'log',
        '--reverse',
        '--format=%H%x1f%an%x1f%cn',
        'HEAD'
    ) | ForEach-Object {
        $fields = $_ -split ([char] 0x1f)
        if ($fields.Count -eq 3 -and
            ($Names -contains $fields[1] -or $Names -contains $fields[2])) {
            [pscustomobject]@{
                Hash      = $fields[0]
                Author    = $fields[1]
                Committer = $fields[2]
            }
        }
    } | Select-Object -First 1

    if (-not $firstMatch) {
        throw "No commit authored or committed by $($Names -join ' or ') exists in the current branch."
    }

    $parentRecord = Invoke-Git -Arguments @('rev-list', '--parents', '-n', '1', $firstMatch.Hash)
    $parentFields = $parentRecord -split '\s+'
    if ($parentFields.Count -lt 2) {
        throw "The first matching commit $($firstMatch.Hash) is the root commit; this command does not rewrite repository roots."
    }

    $parent = $parentFields[1]
    $oldHead = Invoke-Git -Arguments @('rev-parse', 'HEAD')
    $range = "$parent..$oldHead"
    $commitCount = [int] (Invoke-Git -Arguments @('rev-list', '--count', $range))

    Write-Host "First matching commit: $($firstMatch.Hash)"
    Write-Host "Author: $($firstMatch.Author)"
    Write-Host "Committer: $($firstMatch.Committer)"
    Write-Host "Commits to squash: $commitCount"
    Invoke-Git -Arguments @('log', '--oneline', '--reverse', $range) | Write-Host

    if ($commitCount -le 1) {
        Write-Host 'The matching range is already represented by a single commit. No rewrite is needed.'
        return
    }

    if (-not $Force) {
        $confirmation = Read-Host 'Rewrite this local branch history? Type YES to continue'
        if ($confirmation -cne 'YES') {
            Write-Host 'Squash cancelled.'
            return
        }
    }

    Invoke-Git -Arguments @('reset', '--soft', $parent) | Out-Null

    if (-not $Commit) {
        Write-Host 'Squash prepared. The combined changes are staged, but no commit was created.'
        Write-Host 'Review with: git status && git diff --cached'
        Write-Host 'Commit when ready with: git commit'
        Write-Host 'Push when ready with: git push --force-with-lease'
        Write-Host "To undo the preparation before committing: git reset --soft $oldHead"
        return
    }

    try {
        if ($MessageFile) {
            Invoke-Git -Arguments @('commit', '--file', $MessageFile) | Write-Host
        }
        else {
            Invoke-Git -Arguments @('commit', '--reuse-message', $oldHead) | Write-Host
        }
    }
    catch {
        & git reset --soft $oldHead
        throw
    }

    $newHead = Invoke-Git -Arguments @('rev-parse', 'HEAD')
    Write-Host "Squash complete: $newHead"
    Write-Host 'No push was performed. Push when ready with: git push --force-with-lease'
}
finally {
    Pop-Location
}
