# Contributing

[![Slack](https://img.shields.io/badge/slack-@cncf/otel--dotnet--auto--instr-brightgreen.svg?logo=slack)](https://cloud-native.slack.com/archives/C01NR1YLSE7)

We'd love your help!

[The project board](https://github.com/orgs/open-telemetry/projects/39)
shows the current work in progress.

Please join our weekly [SIG meeting](https://github.com/open-telemetry/community#special-interest-groups).
Meeting notes are available as a public [Google
doc](https://docs.google.com/document/d/1dYdwRQVE3zu0vlp_lqGctNm0dCQUkDo2LfScUJzpuT8/edit?usp=sharing).

Get in touch on [Slack](https://cloud-native.slack.com/archives/C01NR1YLSE7).
If you are new, you can create a [CNCF Slack account](http://slack.cncf.io/).

## Give feedback

We are always looking for your feedback.

You can do this by [submitting a GitHub issue](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/new).

You may also prefer writing on [Slack](https://cloud-native.slack.com/archives/C01NR1YLSE7).

## Report a bug

Reporting bugs is an important contribution. Please make sure to include:

* Expected and actual behavior
* OpenTelemetry, OS, and .NET versions you are using
* If possible, steps to reproduce

## Request a feature

If you would like to work on something that is not listed as an issue
(e.g. a new feature or enhancement) please first read our [design.md](design.md)
to make sure your proposal aligns with the goals of the
project, then create an issue and describe your proposal.

## How to contribute

Please read the [OpenTelemetry New Contributor Guide](https://github.com/open-telemetry/community/tree/main/guides/contributor)
and the [code of conduct](https://github.com/open-telemetry/community/blob/main/code-of-conduct.md).
for general practices of the OpenTelemetry project.

Select a good issue from the links below (ordered by difficulty/complexity):

* [Good First Issue](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)
* [Help Wanted](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22)

Comment on the issue that you want to work on so we can assign it to you and
clarify anything related to it.

If you would like to work on something that is not listed as an issue,
please [request a feature](#request-a-feature) first.
It is best to do this in advance so that maintainers can decide if the proposal
is a good fit for this repository.
This will help avoid situations when you spend significant time
on something that maintainers may decide this repo is not the right place for.

See [developing.md](developing.md) to learn more about
the development environment setup and usage.

## Create Your First Pull Request

### How to Send Pull Requests

Everyone is welcome to contribute code to `opentelemetry-dotnet-instrumentation`
via GitHub pull requests (PRs).

To create a new PR, fork the project in GitHub and clone the upstream repo:

> [!NOTE]
> Please fork to a personal GitHub account rather than a corporate/enterprise
> one so maintainers can push commits to your branch.
> **Pull requests from protected forks will not be accepted.**

```sh
git clone https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation.git
```

Navigate to the repo root:

```sh
cd opentelemetry-dotnet-instrumentation
```

Add your fork as an origin:

```sh
git remote add fork https://github.com/YOUR_GITHUB_USERNAME/opentelemetry-dotnet-instrumentation.git
```

Check out a new branch, make modifications and push the branch to your fork:

```sh
$ git checkout -b feature
# change files
# Test your changes locally.
$ nuke Workflow
$ git add my/changed/files
$ git commit -m "short description of the change"
$ git push fork feature
```

Open a pull request against the main `opentelemetry-demo` repo.

### How to Receive Comments

* If the PR is not ready for review, please mark it as
  [`draft`](https://github.blog/2019-02-14-introducing-draft-pull-requests/).
* Make sure CLA is signed and all required CI checks are clear.
* Submit small, focused PRs addressing a single
  concern/issue.
* Make sure the PR title reflects the contribution.
* Write a summary that helps understand the change.
* Include usage examples in the summary, where applicable.
* Include benchmarks (before/after) in the summary, for contributions that are
  performance enhancements.

### How to Get PRs Merged

A PR is considered to be **ready to merge** when:

* It has received approval from
  [Approvers](https://github.com/open-telemetry/community/blob/main/guides/contributor/membership.md#approver)
  /
  [Maintainers](https://github.com/open-telemetry/community/blob/main/guides/contributor/membership.md#maintainer).
* Major feedbacks are resolved.
* The documentation and [Changelog](../CHANGELOG.md) have been updated
  to reflect the new changes.

Any Maintainer can merge the PR once it is **ready to merge**. Note, that some
PRs may not be merged immediately if the repo is in the process of a release and
the maintainers decided to defer the PR to the next release train.

If a PR has been stuck (e.g. there are lots of debates and people couldn't agree
on each other), the owner should try to get people aligned by:

* Consolidating the perspectives and putting a summary in the PR. It is
  recommended to add a link into the PR description, which points to a comment
  with a summary in the PR conversation.
* Tagging subdomain experts (by looking at the change history) in the PR asking
  for suggestion.
* Reaching out to more people on the [CNCF OpenTelemetry Automatic
  instrumentation for .NET Slack channel](https://cloud-native.slack.com/archives/C01NR1YLSE7).
* Stepping back to see if it makes sense to narrow down the scope of the PR or
  split it up.
* If none of the above worked and the PR has been stuck for more than 2 weeks,
  the owner should bring it to the OpenTelemetry Automatic Instrumentation SIG
  [meeting](README.md#contributing).
