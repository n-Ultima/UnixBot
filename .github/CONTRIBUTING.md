# Contributing Guidelines
## Description
This file serves as a base mark to those who wish to contribute to the UnixBot project. Maintainers, and guidelines, are listed here, along with steps that should be taken before attempting to contribute.
## Maintainers
Ultima#2000
## Contributors
Voxel#8113
## Before Contributing
*  Before you contribute(via a pull request), it is asked that you run `dotnet build -c Release /warnaserror` on your local branch. We strive to not have any warnings, and this command will flag all warnings as errors. It is also asked that when naming variables, properties, etc, that they not be nonsense, and actually make sense to the context of what you're writing
* Also, treat others how you would treat yourself. Disrespect is not tolerated. Treat other humans how you would like to be treated.
## Questions
If you have any questions regarding Unix, you can join the [Unix Discord Server](http://www.ultima.one/unix)
## Requirements
* Download the [.NET SDK](https://dotnet.microsoft.com/download)

## How to Contribute:
* Create and clone your fork(git clone <your fork)
* Create a branch:
```
git checkout -b feature/FeatureName
```
* Make sure there are no warnings:
```
dotnet build -c Release /warnaserror
```
* Commit your changes and push your changes.
```
git add *
git commit -m "Commit message"
git push origin feature/FeatureName
```
Open a pull request

Once your PR has been submitted, your changes will be reviewed. It is expected that feedback provided be taken as constructive, and not an attack.

