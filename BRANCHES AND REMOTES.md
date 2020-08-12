# Branch name guidance

**Master branch:**  
`master`

**Branches containing work by specific people:**  
`priv/github-user-name.descriptive-name-of-branch`  
`priv/macrogreg.vendor-remotes-instructions`  
`priv/pjanotti.setup-codeowners`  
`. . .`  

**Long-lived branches containing large features that have many commits from multiple authors:**  
`feature/descriptive-name-of-branch`  
`feature/refactor-assembly-structure`  
`feature/instrument-call-targets`  
`. . .`  

**Branches representing the contents of downstream vendor repos:**  
`vendors/vendor-name`    
`vendors/datadog`  
`vendors/splunk`  
`vendors/microsoft`  
`. . .`  

**Branches tracking other repos (you will create them locally, see below):**  
`vendor-remotes/vendor-name`  
`vendor-remotes/datadog`  
`vendor-remotes/splunk`  
`vendor-remotes/microsoft`  
`. . .`   


# Merging from a vendor-repo

## You are merging from a tracking vendor repo into the OTel repo for the first time:

**Create a container folder:**  
```
c:\Code> mkdir DotNET-Instrumentation
```
```
c:\Code> cd DotNET-Instrumentation
```

**Clone this OTel repo:**  
```
c:\Code\DotNET-Instrumentation>git clone git@github.com:open-telemetry/opentelemetry-dotnet-instrumentation.git
```
```
c:\Code\DotNET-Instrumentation>cd opentelemetry-dotnet-instrumentation
```

**Add a remote for your specific vendor repo:**  
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git remote add _VendorName_ _VendorRemoteRepoAddress_
```
for example: 
```
c:\00\Code\GitHubOTel\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git remote add DataDog git@github.com:DataDog/dd-trace-dotnet.git
```

**Validate:**  
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git remote
```
you should see:
```
DataDog
origin
```

**Fetch branches from the new remote:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git fetch DataDog
```

**Create a branch tracking your remote:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout -b vendor-remotes/_vendor-name_ --track _VendorName_/master
```
for example
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout -b vendor-remotes/datadog --track DataDog/master
```

You should see:
```
Switched to a new branch 'vendor-remotes/datadog'
Branch 'vendor-remotes/datadog' set up to track remote branch 'master' from 'DataDog'.
```

**Pull your new branch** (to be sure)**:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git pull
```

Validate that this pulls from your vendor repo. I.e., this the output should contain a line such as
```
From github.com:VendorName/_VendorRemoteRepoAddress_
```
e.g.:
```
From github.com:DataDog/dd-trace-dotnet
```

**Create a new branch that will be mirroring your vendor-repo into a remote branch in the OTel repo (please mind the naming convention). That branch should initially sub-branch from master:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout master
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout -b vendors/_vendor-name_
```
e.g.:
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout master
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git checkout -b vendors/datadog
```

**Merge from the branch tracking your vendor repo into the branch mirroring your vendor repo into OTel:**
- **Make sure to squash.** It is important because every person who made a commit to the code you are merging will need to sign the OTel contributor license agreement (CLA). If you squash, only you need to sign.
- **Make sure to create a common baseline/history.**
``` 
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git merge vendor-remotes/_vendor-name_ --squash --allow-unrelated-histories
```
e.g.:
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git merge vendor-remotes/datadog --squash --allow-unrelated-histories
```

**Commit your merge:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git commit -m "Descriptive commit message such as 'Merging feature XYZ from VendorName'."
```

**Push to OTel repo:**
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git push --set-upstream origin vendors/_vendor-name_
```
e.g.:
```
c:\Code\DotNET-Instrumentation\opentelemetry-dotnet-instrumentation>git push --set-upstream origin vendors/datadog
```

Now you can **go to the GitHub site** and **create a pull request from "vendors/_vendor-name_" into "master"**.

## You have previously merged from a tracking vendor repo into the OTel repo

* You already have a remote named "_VendorName_" and a local branch named "vendor-remotes/_vendor-name_" which tracks thar remove.  
If not, see above.

* Pull your "vendor-remotes/_vendor-name_" branch.

* Merge your local branch "vendor-remotes/_vendor-name_" which tracks the vendor remote into your local branch "vendors/_vendor-name_" which tracks the OTel remote (origin).  
You may need to either have everybody who made commits that are part of this merge sign the OTel contributor license agreement (CLA), or you need to make sure that this is a squash-merge (then only you will need to sign the CLA). The CLA only needs to be signed once.

* Push your "vendors/_vendor-name_" branch to the OTel remote (origin).

* Create a PR from "vendors/_vendor-name_" into "master".